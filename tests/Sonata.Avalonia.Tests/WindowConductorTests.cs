using Avalonia.Controls;
using System.ComponentModel;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Sonata.Avalonia;
using Sonata.Avalonia.Internal;
using Xunit;

namespace Sonata.Avalonia.Tests;

[Collection("Ambient")]
public class WindowConductorTests
{
    private readonly FakeWindowAdapter _window;
    private readonly TestScreen _vm;
    private readonly WindowConductor _conductor;

    public WindowConductorTests()
    {
        UiThreadDispatch.Dispatcher = SynchronousDispatcher.Instance;
        _window = new FakeWindowAdapter();
        _vm = new TestScreen();
        _conductor = new WindowConductor(_window, _vm, NullLogger.Instance);
    }

    [Fact]
    public void Construction_ActivatesViewModel()
    {
        Assert.Equal(ScreenState.Active, _vm.ScreenState);
        Assert.Contains("OnInitialActivateAsync", _vm.Calls);
    }

    [Fact]
    public void StateChanged_Minimized_Deactivates()
    {
        _window.WindowState = WindowState.Minimized;
        _window.RaiseStateChanged();

        Assert.Equal(ScreenState.Deactivated, _vm.ScreenState);
    }

    [Fact]
    public void StateChanged_Restored_Reactivates()
    {
        _window.WindowState = WindowState.Minimized;
        _window.RaiseStateChanged();
        _window.WindowState = WindowState.Maximized;
        _window.RaiseStateChanged();

        Assert.Equal(ScreenState.Active, _vm.ScreenState);
    }

    [Fact]
    public void WindowClosing_CanCloseFalse_CancelsClose()
    {
        _vm.CanCloseResult = false;

        var e = new CancelEventArgs();
        _window.RaiseClosing(e);

        Assert.True(e.Cancel);
        Assert.Equal(0, _window.CloseCalls);
    }

    [Fact]
    public void WindowClosing_CanCloseTrue_ClosesWindow()
    {
        _vm.CanCloseResult = true;

        _window.RaiseClosing(new CancelEventArgs());

        Assert.Equal(1, _window.CloseCalls);
    }

    [Fact]
    public void WindowClosing_CanCloseThrows_CancelsCloseWithoutCrash()
    {
        _vm.ThrowOnCanClose = true;

        var e = new CancelEventArgs();
        _window.RaiseClosing(e);

        Assert.True(e.Cancel);
        Assert.Equal(0, _window.CloseCalls);
    }

    [Fact]
    public void WindowClosed_ClosesViewModelAndDisposes()
    {
        _window.RaiseClosed();

        Assert.Equal(ScreenState.Closed, _vm.ScreenState);
        Assert.True(_window.Disposed);
    }

    [Fact]
    public async Task CloseItemAsync_ClosesWindowWithDialogResult()
    {
        await ((IChildDelegate)_conductor).CloseItemAsync(_vm, true, CancellationToken.None);

        Assert.Equal(1, _window.CloseCalls);
        Assert.Equal(true, _window.LastDialogResult);
        Assert.Equal(ScreenState.Closed, _vm.ScreenState);
        Assert.True(_window.Disposed);
    }

    [Fact]
    public async Task CloseItemAsync_GuardFalse_DoesNothing()
    {
        _vm.CanCloseResult = false;

        await ((IChildDelegate)_conductor).CloseItemAsync(_vm, null, CancellationToken.None);

        Assert.Equal(0, _window.CloseCalls);
    }

    [Fact]
    public async Task CloseItemAsync_WrongItem_NoAction()
    {
        await ((IChildDelegate)_conductor).CloseItemAsync(new TestScreen(), null, CancellationToken.None);

        Assert.Equal(0, _window.CloseCalls);
    }

    [Fact]
    public async Task CloseItemAsync_TryCloseThrows_StillClosesAndDisposes()
    {
        _vm.ThrowOnClose = true;

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => ((IChildDelegate)_conductor).CloseItemAsync(_vm, null, CancellationToken.None));

        Assert.Equal(1, _window.CloseCalls);
        Assert.True(_window.Disposed);
    }

    [Fact]
    public void WindowClosing_AlreadyCancelled_ShortCircuits()
    {
        _vm.CanCloseResult = false;

        var e = new CancelEventArgs { Cancel = true };
        _window.RaiseClosing(e);

        Assert.Equal(0, _window.CloseCalls);   // handler returned before touching the guard
    }

    [Fact]
    public async Task CloseItemAsync_DisposeThrows_StillCloses()
    {
        var throwing = new ThrowingDisposeWindowAdapter();
        var vm = new TestScreen();
        var conductor = new WindowConductor(throwing, vm, NullLogger.Instance);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => ((IChildDelegate)conductor).CloseItemAsync(vm, null, CancellationToken.None));

        Assert.Equal(1, throwing.CloseCalls);
        Assert.Equal(ScreenState.Closed, vm.ScreenState);
    }

    [Fact]
    public void WindowClosed_DisposeThrows_StillClosesViewModel()
    {
        var provider = new TestLoggerProvider();
        var logger = new LoggerFactory(new[] { provider }).CreateLogger("WindowConductor");
        var throwing = new ThrowingDisposeWindowAdapter();
        var vm = new TestScreen();
        var conductor = new WindowConductor(throwing, vm, logger);

        throwing.RaiseClosed();

        Assert.Equal(ScreenState.Closed, vm.ScreenState);
        Assert.Contains(provider.Entries, e => e.Level == LogLevel.Error && e.Message.Contains("cleanup threw"));
    }

    [Fact]
    public void WindowClosed_DisposeAndLoggerThrow_StillClosesViewModel()
    {
        var throwing = new ThrowingDisposeWindowAdapter();
        var vm = new TestScreen();
        var conductor = new WindowConductor(throwing, vm, new ThrowingLogger());

        throwing.RaiseClosed();

        Assert.Equal(ScreenState.Closed, vm.ScreenState);
    }

    [Fact]
    public void WindowClosing_ThrowingLogger_DoesNotCrash()
    {
        var window = new FakeWindowAdapter();
        var vm = new TestScreen();
        var conductor = new WindowConductor(window, vm, new ThrowingLogger());

        var e = new CancelEventArgs();
        window.RaiseClosing(e);

        Assert.True(e.Cancel);
        Assert.Equal(0, window.CloseCalls);
    }
}
