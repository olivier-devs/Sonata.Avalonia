namespace Sonata.Avalonia;

/// <summary>
/// Implementation of IScreen. Useful as a base class for your ViewModels
/// </summary>
public class Screen : ValidatingModelBase, IScreen, IAsyncDisposable
{
    /// <summary>
    /// Logger used by this screen and derived conductor types.
    /// </summary>
    protected ILogger Logger => SonataLogManager.GetLogger(GetType());

    /// <summary>
    /// Initialises a new instance of the <see cref="Screen"/> class, without setting up a validator
    /// </summary>
    public Screen() : this(null) { }

    /// <summary>
    /// Initialises a new instance of the <see cref="Screen"/> class, which can validate properties using the given validator
    /// </summary>
    /// <param name="validator">Validator to use</param>
    [SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors", Justification = "Can be safely called from the Ctor, as it doesn't depend on state being set")]
    public Screen(IModelValidator? validator) : base(validator)
    {
        var type = GetType();
        DisplayName = type.FullName!; // Runtime types always have a full name
    }

    #region IHaveDisplayName

    private string _displayName = string.Empty;

    /// <summary>
    /// Gets or sets the name associated with this ViewModel.
    /// Shown e.g. in a window's title bar, or as a tab's displayName
    /// </summary>
    public string DisplayName
    {
        get { return _displayName; }
        set { SetAndNotify(ref _displayName, value); }
    }

    #endregion

    #region IScreenState

    private ScreenState _screenState = ScreenState.Deactivated;

    /// <summary>
    /// Gets or sets the current state of the Screen
    /// </summary>
    public virtual ScreenState ScreenState
    {
        get { return _screenState; }
        protected set
        {
            if (SetAndNotify(ref _screenState, value))
            {
                NotifyOfPropertyChange("IsActive");
            }
        }
    }

    /// <summary>
    /// Gets a value indicating whether the current state is ScreenState.Active
    /// </summary>
    public bool IsActive
    {
        get { return ScreenState == ScreenState.Active; }
    }

    private bool haveActivated = false;

    /// <summary>
    /// Raised when the Screen's state changed, for any reason
    /// </summary>
    public event EventHandler<ScreenStateChangedEventArgs>? StateChanged;

    /// <summary>
    /// Fired whenever the Screen is activated
    /// </summary>
    public event EventHandler<ActivationEventArgs>? Activated;

    /// <summary>
    /// Fired whenever the Screen is deactivated
    /// </summary>
    public event EventHandler<DeactivationEventArgs>? Deactivated;

    /// <summary>
    /// Called whenever this Screen is closed
    /// </summary>
    public event EventHandler<CloseEventArgs>? Closed;

    /// <summary>
    /// Called the very first time this Screen is activated, and never again
    /// </summary>
    protected virtual Task OnInitialActivateAsync(CancellationToken ct) => Task.CompletedTask;

    /// <summary>
    /// Called every time this screen is activated
    /// </summary>
    protected virtual Task OnActivateAsync(CancellationToken ct) => Task.CompletedTask;

    /// <summary>
    /// Called every time this screen is deactivated
    /// </summary>
    protected virtual Task OnDeactivateAsync(CancellationToken ct) => Task.CompletedTask;

    /// <summary>
    /// Called when this screen is closed
    /// </summary>
    protected virtual Task OnCloseAsync(CancellationToken ct) => Task.CompletedTask;

    /// <summary>
    /// Called on any state transition
    /// </summary>
    /// <param name="previousState">Previous state state</param>
    /// <param name="newState">New state</param>
    protected virtual Task OnStateChangedAsync(ScreenState previousState, ScreenState newState, CancellationToken ct) => Task.CompletedTask;

    /// <summary>
    /// Sets the screen's state to the given state, if it differs from the current state
    /// </summary>
    /// <param name="newState">State to transition to</param>
    /// <param name="changedHandler">Called if the transition occurs. Arguments are (previousState, newState)</param>
    protected virtual async Task SetStateAsync(ScreenState newState, Func<ScreenState, ScreenState, Task> changedHandler, CancellationToken ct)
    {
        if (newState == ScreenState)
            return;

        var previousState = ScreenState;
        ScreenState = newState;

        Logger.LogInformation("Setting state from {0} to {1}", previousState, newState);

        await OnStateChangedAsync(previousState, newState, ct);
        await changedHandler(previousState, newState);

        var handler = StateChanged;
        if (handler != null)
            UiThreadDispatch.PostToUIThread(() => handler(this, new ScreenStateChangedEventArgs(newState, previousState)));
    }

    [SuppressMessage("Microsoft.Design", "CA1033:InterfaceMethodsShouldBeCallableByChildTypes", Justification = "As this is a framework type, don't want to make it too easy for users to call this method")]
    async Task IScreenState.ActivateAsync(CancellationToken ct)
    {
        await SetStateAsync(ScreenState.Active, async (oldState, newState) =>
        {
            bool isInitialActivate = !haveActivated;
            if (!haveActivated)
            {
                await OnInitialActivateAsync(ct);
                haveActivated = true;
            }

            await OnActivateAsync(ct);

            var handler = Activated;
            if (handler != null)
                UiThreadDispatch.PostToUIThread(() => handler(this, new ActivationEventArgs(oldState, isInitialActivate)));
        }, ct);
    }

    [SuppressMessage("Microsoft.Design", "CA1033:InterfaceMethodsShouldBeCallableByChildTypes", Justification = "As this is a framework type, don't want to make it too easy for users to call this method")]
    async Task IScreenState.DeactivateAsync(CancellationToken ct)
    {
        // Avoid going from Closed -> Deactivated without going via Activated
        if (ScreenState == ScreenState.Closed)
            await ((IScreenState)this).ActivateAsync(ct);

        await SetStateAsync(ScreenState.Deactivated, async (oldState, newState) =>
        {
            await OnDeactivateAsync(ct);

            var handler = Deactivated;
            if (handler != null)
                UiThreadDispatch.PostToUIThread(() => handler(this, new DeactivationEventArgs(oldState)));
        }, ct);
    }

    [SuppressMessage("Microsoft.Design", "CA1033:InterfaceMethodsShouldBeCallableByChildTypes", Justification = "As this is a framework type, don't want to make it too easy for users to call this method")]
    async Task IScreenState.CloseAsync(CancellationToken ct)
    {
        // Avoid going from Activated -> Closed without going via Deactivated
        if (ScreenState != ScreenState.Closed)
            await ((IScreenState)this).DeactivateAsync(ct);

        DetachView();

        // Reset, so we can initially activate again
        haveActivated = false;

        await SetStateAsync(ScreenState.Closed, async (oldState, newState) =>
        {
            await OnCloseAsync(ct);

            var handler = Closed;
            if (handler != null)
                UiThreadDispatch.PostToUIThread(() => handler(this, new CloseEventArgs(oldState)));
        }, ct);
    }

    #endregion

    #region IViewAware

    /// <summary>
    /// Gets the View attached to this ViewModel, if any. Using this should be a last resort
    /// </summary>
    public Control? View { get; private set; }

    [SuppressMessage("Microsoft.Design", "CA1033:InterfaceMethodsShouldBeCallableByChildTypes", Justification = "As this is a framework type, don't want to make it too easy for users to call this method")]
    void IViewAware.AttachView(Control view)
    {
        if (View != null)
            throw new InvalidOperationException(string.Format("Tried to attach View {0} to ViewModel {1}, but it already has a view attached", view.GetType().Name, GetType().Name));

        View = view;

        Logger.LogInformation("Attaching view {0}", view);

        if (view is Control viewAsFrameworkElement)
        {
            if (viewAsFrameworkElement.IsLoaded)
                OnViewLoaded();
            else
                viewAsFrameworkElement.Loaded += OnViewLoadedHandler;
        }
    }

    /// <summary>
    /// Called when the view attaches to the Screen loads
    /// </summary>
    protected virtual void OnViewLoaded() { }

    private void OnViewLoadedHandler(object? sender, RoutedEventArgs e)
    {
        // One-shot: remove ourselves so a surviving view never keeps this ViewModel alive
        if (sender is Control control)
            control.Loaded -= OnViewLoadedHandler;
        OnViewLoaded();
    }

    private void DetachView()
    {
        if (View is Control view)
            view.Loaded -= OnViewLoadedHandler;
        View = null;
    }

    #endregion

    #region IChild

    private object? _parent;

    /// <summary>
    /// Gets or sets the parent conductor of this screen. Used to RequestClose to request a closure
    /// </summary>
    public object? Parent
    {
        get => _parent;
        set => SetAndNotify(ref _parent, value);
    }

    #endregion

    #region IGuardClose

    /// <summary>
    /// Called when a conductor wants to know whether this screen can close.
    /// </summary>
    /// <param name="ct">Cancellation token to observe</param>
    /// <returns>A task returning true (can close) or false (can't close)</returns>
    public virtual Task<bool> CanCloseAsync(CancellationToken ct = default)
    {
        return Task.FromResult(true);
    }

    #endregion

    #region IRequestClose

    /// <summary>
    /// Request that the conductor responsible for this screen close it
    /// </summary>
    /// <param name="dialogResult">DialogResult to return, if this is a dialog</param>
    public virtual void RequestClose(bool? dialogResult = null)
    {
        var conductor = Parent as IChildDelegate;
        if (conductor != null)
        {
            Logger.LogInformation("RequstClose called. Conductor: {0}; DialogResult: {1}", conductor, dialogResult);
            FireAndForget.Run(conductor.CloseItemAsync(this, dialogResult), Logger);
        }
        else
        {
            var e = new InvalidOperationException(string.Format("Unable to close ViewModel {0} as it must have a conductor as a parent (note that windows and dialogs automatically have such a parent)", GetType()));
            Logger.LogError(e, "Unable to close ViewModel — no conductor parent");
            throw e;
        }
    }

    #endregion

    #region IAsyncDisposable

    /// <summary>
    /// Closes the screen if it's still open, then releases its resources.
    /// Idempotent: disposing an already-closed screen is a no-op.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        if (ScreenState != ScreenState.Closed)
            await ((IScreenState)this).CloseAsync();
    }

    #endregion
}
