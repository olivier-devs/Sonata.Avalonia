using System.ComponentModel;

namespace Sonata.Avalonia.Internal;

/// <summary>
/// Abstraction over a Window used to make <see cref="WindowConductor"/> testable
/// without instantiating a real <see cref="Avalonia.Controls.Window"/>.
/// </summary>
internal interface IWindowAdapter : IDisposable
{
    event EventHandler<CancelEventArgs>? Closing;
    event EventHandler? Closed;
    event EventHandler? StateChanged;
    WindowState WindowState { get; }
    void Close(object? dialogResult = null);
}

/// <summary>
/// Adapter wrapping a real <see cref="Window"/>. Forwards events and Close.
/// </summary>
internal sealed class WindowAdapter : IWindowAdapter
{
    private readonly Window _window;
    private readonly IDisposable _stateSubscription;

    public WindowAdapter(Window window)
    {
        _window = window;
        _stateSubscription = window.GetPropertyChangedObservable(Window.WindowStateProperty)
            .Subscribe(_ => StateChanged?.Invoke(this, EventArgs.Empty));
        _window.Closing += OnWindowClosing;
        _window.Closed += OnWindowClosed;
    }

    public event EventHandler<CancelEventArgs>? Closing;
    public event EventHandler? Closed;
    public event EventHandler? StateChanged;

    public WindowState WindowState => _window.WindowState;

    public void Close(object? dialogResult = null) => _window.Close(dialogResult);

    public void Dispose()
    {
        _window.Closing -= OnWindowClosing;
        _window.Closed -= OnWindowClosed;
        _stateSubscription.Dispose();
    }

    private void OnWindowClosing(object? sender, WindowClosingEventArgs e) => Closing?.Invoke(sender, e);
    private void OnWindowClosed(object? sender, EventArgs e) => Closed?.Invoke(sender, e);
}

/// <summary>
/// Orchestrates a Window and its ViewModel lifecycle (activation, state changes,
/// close guard). Works against <see cref="IWindowAdapter"/> for testability.
/// </summary>
internal sealed class WindowConductor : IChildDelegate
{
    private readonly IWindowAdapter _window;
    private readonly object _viewModel;
    private readonly ILogger _logger;

    public WindowConductor(IWindowAdapter window, object viewModel, ILogger logger)
    {
        _window = window;
        _viewModel = viewModel;
        _logger = logger;

        // They won't be able to request a close unless they implement IChild anyway...
        if (_viewModel is IChild viewModelAsChild)
            viewModelAsChild.Parent = this;

        FireAndForget.Run(ScreenExtensions.TryActivateAsync(_viewModel), _logger);

        _window.Closed += WindowClosed;

        if (_viewModel is IScreenState)
            _window.StateChanged += WindowStateChanged;

        if (_viewModel is IGuardClose)
            _window.Closing += WindowClosing;
    }

    private void WindowStateChanged(object? sender, EventArgs e)
    {
        switch (_window.WindowState)
        {
            case WindowState.Maximized:
            case WindowState.Normal:
                _logger.LogInformation("Window {0} maximized/restored: activating", _window);
                FireAndForget.Run(ScreenExtensions.TryActivateAsync(_viewModel), _logger);
                break;

            case WindowState.Minimized:
                _logger.LogInformation("Window {0} minimized: deactivating", _window);
                FireAndForget.Run(ScreenExtensions.TryDeactivateAsync(_viewModel), _logger);
                break;
        }
    }

    private void UnsubscribeFromWindowEvents()
    {
        _window.StateChanged -= WindowStateChanged;
        _window.Closed -= WindowClosed;
        _window.Closing -= WindowClosing;
    }

    private void WindowClosed(object? sender, EventArgs e)
    {
        UnsubscribeFromWindowEvents();

        try
        {
            _window.Dispose();
        }
        catch (Exception ex)
        {
            // Guarded: a misbehaving adapter (or logger) must not prevent the ViewModel from closing.
            try { _logger.LogError(ex, "Window adapter Dispose threw for ViewModel {0}; closing the ViewModel anyway", _viewModel); }
            catch { }
        }
        finally
        {
            FireAndForget.Run(ScreenExtensions.TryCloseAsync(_viewModel), _logger);
        }
    }

    private async void WindowClosing(object? sender, CancelEventArgs e)
    {
        if (e.Cancel)
            return;

        // Cancel by default: the close only proceeds if CanCloseAsync succeeds below.
        e.Cancel = true;

        try
        {
            _logger.LogInformation("ViewModel {0} close requested because its View was closed", _viewModel);

            if (await ((IGuardClose)_viewModel).CanCloseAsync())
            {
                _window.Closing -= WindowClosing;
                _window.Close();
                // The Closed event handler handles unregistering the events, and closing the ViewModel
            }
            else
            {
                _logger.LogInformation("Close of ViewModel {0} cancelled because CanCloseAsync returned false", _viewModel);
            }
        }
        catch (Exception ex)
        {
            // Guarded: CanCloseAsync or a misbehaving logger must not crash the async void handler.
            // Cancel the close so the window stays usable.
            try { _logger.LogError(ex, "CanCloseAsync threw for ViewModel {0}; close cancelled, window stays open", _viewModel); }
            catch { }
        }
    }

    /// <summary>
    /// Close was requested by the child
    /// </summary>
    async Task IChildDelegate.CloseItemAsync(object item, bool? dialogResult, CancellationToken ct)
    {
        if (item != _viewModel)
        {
            _logger.LogWarning("IChildDelegate.CloseItemAsync called with item {0} which is _not_ our ViewModel {1}", item, _viewModel);
            return;
        }

        if (_viewModel is IGuardClose guardClose && !await guardClose.CanCloseAsync(ct))
        {
            _logger.LogInformation("Close of ViewModel {0} cancelled because CanCloseAsync returned false", _viewModel);
            return;
        }

        _logger.LogInformation("ViewModel {0} close requested with DialogResult {1} because it called RequestClose", _viewModel, dialogResult);

        UnsubscribeFromWindowEvents();

        try
        {
            _window.Dispose();
            await ScreenExtensions.TryCloseAsync(_viewModel, ct);
        }
        finally
        {
            _window.Close(dialogResult);
        }
    }
}
