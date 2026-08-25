namespace Sonata.Avalonia;

/// <summary>
/// Deprecated facade over the ambient UI-thread dispatch. Inject <see cref="IDispatcher"/>
/// instead; this facade will be removed in a future version. Framework base classes
/// (Screen, BindableCollection, PropertyChangedBase) use the internal <c>UiThreadDispatch</c>.
/// </summary>
public static class Execute
{
    /// <summary>
    /// Gets or sets the ambient dispatcher.
    /// </summary>
    [Obsolete("Inject IDispatcher instead. This facade will be removed in a future version.")]
    public static IDispatcher Dispatcher
    {
        get => UiThreadDispatch.Dispatcher;
        set => UiThreadDispatch.Dispatcher = value ?? throw new ArgumentNullException(nameof(value));
    }

    /// <summary>
    /// Dispatches the given action to be run on the UI thread asynchronously.
    /// </summary>
    [Obsolete("Inject IDispatcher instead. This facade will be removed in a future version.")]
    public static void PostToUIThread(Action action)
    {
        UiThreadDispatch.PostToUIThread(action);
    }

    /// <summary>
    /// Runs the action on the UI thread synchronously if already on it, otherwise posts.
    /// </summary>
    [Obsolete("Inject IDispatcher instead. This facade will be removed in a future version.")]
    public static void OnUIThread(Action action)
    {
        UiThreadDispatch.OnUIThread(action);
    }

    /// <summary>
    /// Gets or sets a value indicating whether design mode is currently active.
    /// Settable for really obscure unit testing only.
    /// </summary>
    public static bool InDesignMode
    {
        get => UiThreadDispatch.InDesignMode;
        set => UiThreadDispatch.InDesignMode = value;
    }
}
