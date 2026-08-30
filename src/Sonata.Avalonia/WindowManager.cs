namespace Sonata.Avalonia;

/// <summary>
/// Manager capable of taking a ViewModel instance, instantiating its View and showing it as a dialog or window
/// </summary>
public interface IWindowManager
{
    /// <summary>
    /// Given a ViewModel, show its corresponding View as a window
    /// </summary>
    /// <param name="viewModel">ViewModel to show the View for</param>
    void ShowWindow(object viewModel);

    /// <summary>
    /// Given a ViewModel, show its corresponding View as a window, and set its owner
    /// </summary>
    /// <param name="viewModel">ViewModel to show the View for</param>
    /// <param name="ownerViewModel">The ViewModel for the View which should own this window</param>
    void ShowWindow(object viewModel, IViewAware? ownerViewModel);

    /// <summary>
    /// Given a ViewModel, show its corresponding View as a Dialog
    /// </summary>
    /// <param name="viewModel">ViewModel to show the View for</param>
    /// <returns>DialogResult of the View</returns>
    Task<T> ShowDialog<T>(object viewModel);

    /// <summary>
    /// Given a ViewModel, show its corresponding View as a Dialog, and set its owner
    /// </summary>
    /// <param name="viewModel">ViewModel to show the View for</param>
    /// <param name="ownerViewModel">The ViewModel for the View which should own this dialog</param>
    /// <returns>DialogResult of the View</returns>
    Task<T> ShowDialog<T>(object viewModel, IViewAware? ownerViewModel);

    /// <summary>
    /// Display a MessageBox
    /// </summary>
    /// <param name="messageBoxText">A <see cref="string"/> that specifies the text to display.</param>
    /// <param name="caption">A <see cref="string"/> that specifies the title bar caption to display.</param>
    /// <param name="buttons">A <see cref="MessageBoxButton"/> value that specifies which button or buttons to display.</param>
    /// <param name="icon">A <see cref="MessageBoxImage"/> value that specifies the icon to display.</param>
    /// <param name="defaultResult">A <see cref="MessageBoxResult"/> value that specifies the default result of the message box.</param>
    /// <param name="cancelResult">A <see cref="MessageBoxResult"/> value that specifies the cancel result of the message box</param>
    /// <param name="flowDirection">The <see cref="FlowDirection"/> to use, overrides the <see cref="MessageBoxViewModel.DefaultFlowDirection"/></param>
    /// <param name="textAlignment">The <see cref="TextAlignment"/> to use, overrides the <see cref="MessageBoxViewModel.DefaultTextAlignment"/></param>
    /// <returns>The result chosen by the user</returns>
    Task<T> ShowMessageBox<T>(
        string messageBoxText,
        string? caption = null,
        MessageBoxButton buttons = MessageBoxButton.OK,
        MessageBoxImage icon = MessageBoxImage.None,
        MessageBoxResult defaultResult = MessageBoxResult.OK,
        MessageBoxResult cancelResult = MessageBoxResult.None,
        FlowDirection flowDirection = FlowDirection.LeftToRight,
        TextAlignment textAlignment = TextAlignment.Left);
}

/// <summary>
/// Configuration passed to WindowManager (normally implemented by SonataApplicationBase)
/// </summary>
public interface IWindowManagerConfig
{
    /// <summary>
    /// Returns the currently-displayed window, or null if there is none (or it can't be determined)
    /// </summary>
    /// <returns>The currently-displayed window, or null</returns>
    TopLevel? GetActiveWindow();
}

/// <summary>
/// Default implementation of IWindowManager, is capable of showing a ViewModel's View as a dialog or a window
/// </summary>
public class WindowManager : IWindowManager
{
    private readonly ILogger _logger;
    private readonly IViewManager _viewManager;
    private readonly Func<TopLevel?> _getActiveWindow;
    private readonly Func<IMessageBoxViewModel> _messageBoxViewModelFactory;

    /// <summary>
    /// Initialises a new instance of the <see cref="WindowManager"/> class, using the given <see cref="IViewManager"/>
    /// </summary>
    public WindowManager(IViewManager viewManager, IWindowManagerConfig config,
        Func<IMessageBoxViewModel> messageBoxViewModelFactory, ILogger<WindowManager> logger)
    {
        _viewManager = viewManager;
        _getActiveWindow = config.GetActiveWindow;
        _messageBoxViewModelFactory = messageBoxViewModelFactory;
        _logger = logger;
    }

    /// <summary>
    /// Given a ViewModel, show its corresponding View as a window
    /// </summary>
    /// <param name="viewModel">ViewModel to show the View for</param>
    public void ShowWindow(object viewModel)
    {
        ShowWindow(viewModel, null);
    }

    /// <summary>
    /// Given a ViewModel, show its corresponding View as a window, and set its owner
    /// </summary>
    /// <param name="viewModel">ViewModel to show the View for</param>
    /// <param name="ownerViewModel">The ViewModel for the View which should own this window</param>
    public void ShowWindow(object viewModel, IViewAware? ownerViewModel)
    {
        var window = CreateWindow(viewModel, false, ownerViewModel);
        if (ownerViewModel?.View is Window owner)
            window.Show(owner);
        else
            window.Show();
    }

    /// <summary>
    /// Given a ViewModel, show its corresponding View as a Dialog
    /// </summary>
    /// <param name="viewModel">ViewModel to show the View for</param>
    /// <returns>DialogResult of the View</returns>
    public Task<T> ShowDialog<T>(object viewModel)
    {
        return ShowDialog<T>(viewModel, null);
    }

    /// <summary>
    /// Given a ViewModel, show its corresponding View as a Dialog, and set its owner
    /// </summary>
    /// <param name="viewModel">ViewModel to show the View for</param>
    /// <param name="ownerViewModel">The ViewModel for the View which should own this dialog</param>
    /// <returns>DialogResult of the View</returns>
    public Task<T> ShowDialog<T>(object viewModel, IViewAware? ownerViewModel)
    {
        var window = CreateWindow(viewModel, true, ownerViewModel);

        var owner = ownerViewModel?.View as Window ?? InferOwnerOf(window);
        if (owner is null)
            throw new InvalidOperationException(
                "ShowDialog requires an owner window: no ownerViewModel was provided and no active window could be inferred. " +
                "Provide a ViewModel whose View is a shown Window, or call ShowDialog while a window is active.");

        return window.ShowDialog<T>(owner);
    }

    /// <summary>
    /// Display a MessageBox
    /// </summary>
    /// <param name="text">A <see cref="string"/> that specifies the text to display.</param>
    /// <param name="caption">A <see cref="string"/> that specifies the title bar caption to display.</param>
    /// <param name="buttons">A <see cref="MessageBoxButton"/> value that specifies which button or buttons to display.</param>
    /// <param name="icon">A <see cref="MessageBoxImage"/> value that specifies the icon to display.</param>
    /// <param name="defaultResult">A <see cref="MessageBoxResult"/> value that specifies the default result of the message box.</param>
    /// <param name="cancelResult">A <see cref="MessageBoxResult"/> value that specifies the cancel result of the message box</param>
    /// <param name="flowDirection">The <see cref="FlowDirection"/> to use, overrides the <see cref="MessageBoxViewModel.DefaultFlowDirection"/></param>
    /// <param name="textAlignment">The <see cref="TextAlignment"/> to use, overrides the <see cref="MessageBoxViewModel.DefaultTextAlignment"/></param>
    /// <returns>The result chosen by the user</returns>
    public Task<T> ShowMessageBox<T>(
        string text,
        string? caption = null,
        MessageBoxButton buttons = MessageBoxButton.OK,
        MessageBoxImage icon = MessageBoxImage.None,
        MessageBoxResult defaultResult = MessageBoxResult.OK,
        MessageBoxResult cancelResult = MessageBoxResult.None,
        FlowDirection flowDirection = FlowDirection.LeftToRight,
        TextAlignment textAlignment = TextAlignment.Left)
    {
        var vm = _messageBoxViewModelFactory();
        vm.Setup(text, caption, buttons, icon, defaultResult, cancelResult, flowDirection, textAlignment);
        return ShowDialog<T>(vm);
    }

    /// <summary>
    /// Given a ViewModel, create its View, ensure that it's a Window, and set it up
    /// </summary>
    /// <param name="viewModel">ViewModel to create the window for</param>
    /// <param name="isDialog">True if the window will be used as a dialog</param>
    /// <param name="ownerViewModel">Optionally the ViewModel which owns the view which should own this window</param>
    /// <returns>Window which was created and set up</returns>
    protected virtual Window CreateWindow(object viewModel, bool isDialog, IViewAware? ownerViewModel)
    {
        var view = _viewManager.CreateAndBindViewForModelIfNecessary(viewModel);
        var window = view as Window;
        if (window == null)
        {
            var e = new SonataInvalidViewTypeException(string.Format("WindowManager.ShowWindow or .ShowDialog tried to show a View of type '{0}', but that View doesn't derive from the Window class. " +
                "Make sure any Views you display using WindowManager.ShowWindow or .ShowDialog derive from Window (not UserControl, etc)",
                view == null ? "(null)" : view.GetType().Name));
            _logger.LogError(e, "Located view is not a valid Window");
            throw e;
        }

        // Only set this it hasn't been set / bound to anything
        if (viewModel is IHaveDisplayName haveDisplayName && (string.IsNullOrEmpty(window.Title) || window.Title == view.GetType().Name) /*&& BindingOperations.GetBindingBase(window, Window.TitleProperty) == null*/)
        {
            var binding = new Binding(nameof(IHaveDisplayName.DisplayName))
            {
                Source = haveDisplayName,
                Mode = BindingMode.TwoWay
            };
            window.Bind(Window.TitleProperty, binding);
        }

        if (isDialog)
        {
            _logger.LogInformation("Displaying ViewModel {0} with View {1} as a Dialog", viewModel, window);
        }
        else
        {
            _logger.LogInformation("Displaying ViewModel {0} with View {1} as a Window", viewModel, window);
        }

        // If and only if they haven't tried to position the window themselves...
        // Has to be done after we've attempted to set the owner
        if (window.WindowStartupLocation == WindowStartupLocation.Manual && window.Position == default)
        {
            window.WindowStartupLocation = isDialog ? WindowStartupLocation.CenterOwner : WindowStartupLocation.CenterScreen;
        }

        // This gets itself retained by the window, by registering events
        new WindowConductor(new WindowAdapter(window), viewModel, _logger);

        return window;
    }

    private Window? InferOwnerOf(Window window)
    {
        var active = _getActiveWindow() as Window;
        return ReferenceEquals(active, window) ? null : active;
    }


}
