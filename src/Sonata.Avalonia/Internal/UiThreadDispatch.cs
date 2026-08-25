namespace Sonata.Avalonia.Internal;

/// <summary>
/// Ambient UI-thread dispatch used by framework base classes (Screen, conductors,
/// BindableCollection, PropertyChangedBase) which cannot receive constructor
/// injection. DI-managed services should inject <see cref="IDispatcher"/> instead.
/// </summary>
internal static class UiThreadDispatch
{
    private static IDispatcher? _dispatcher;

    /// <summary>
    /// Gets or sets the ambient dispatcher. Defaults to <see cref="SynchronousDispatcher.Instance"/>
    /// when no bootstrapper has installed one (headless/tests). Assigning null resets to the default.
    /// </summary>
    internal static IDispatcher Dispatcher
    {
        get => _dispatcher ?? SynchronousDispatcher.Instance;
        set => _dispatcher = value;
    }

    /// <summary>
    /// Dispatches the action to the UI thread asynchronously, even if already on it.
    /// </summary>
    internal static void PostToUIThread(Action action) => Dispatcher.Post(action);

    /// <summary>
    /// Runs the action on the UI thread synchronously if already on it, otherwise posts.
    /// </summary>
    internal static void OnUIThread(Action action)
    {
        if (Dispatcher.IsCurrent)
            action();
        else
            Dispatcher.Post(action);
    }

    private static bool? _inDesignMode;

    /// <summary>
    /// Gets or sets whether design mode is active. Backs <see cref="Execute.InDesignMode"/>.
    /// </summary>
    internal static bool InDesignMode
    {
        get
        {
            _inDesignMode ??= Design.IsDesignMode;
            return _inDesignMode.Value;
        }
        set => _inDesignMode = value;
    }
}
