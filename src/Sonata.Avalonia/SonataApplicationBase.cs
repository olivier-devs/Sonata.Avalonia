using Sonata.Avalonia.Internal;

namespace Sonata.Avalonia;

/// <summary>
/// Base class for Sonata applications which want to use their own IoC container.
/// Use <see cref="SonataApplication{T}"/> for Microsoft.Extensions.DependencyInjection,
/// or StyletApplication{T} from the Sonata.Avalonia.StyletIoC package.
/// </summary>
public abstract class SonataApplicationBase<T> : Application, IWindowManagerConfig, IDisposable
    where T : class
{
    public override void Initialize()
    {
        IoC.GetInstance = GetInstance;
        IoC.GetInstances = GetInstances;
        base.Initialize();
        Configure();
        SonataLogManager.SetFactory(GetLoggerFactory());
        UiThreadDispatch.Dispatcher = ResolveDispatcher();
    }

    private IDispatcher ResolveDispatcher()
    {
        try
        {
            return (IDispatcher?)GetInstance(typeof(IDispatcher)) ?? new ApplicationDispatcher();
        }
        catch (Exception e)
        {
            SonataLogManager.GetLogger(GetType())
                .LogWarning(e, "Unable to resolve IDispatcher from the container; falling back to ApplicationDispatcher. Register IDispatcher in your container to override.");
            return new ApplicationDispatcher();
        }
    }

    /// <summary>
    /// Retrieves the <see cref="ILoggerFactory"/> used for framework logging, or null to keep the no-op fallback.
    /// </summary>
    protected virtual ILoggerFactory? GetLoggerFactory() => null;
    protected abstract IEnumerable<object> GetInstances(Type service);

    /// <summary>
    /// Given a type, use the IoC container to fetch an instance of it
    /// </summary>
    /// <param name="type">Type of instance to fetch</param>
    /// <returns>Fetched instance</returns>
    protected abstract object GetInstance(Type type);

    protected abstract object GetInstance(Type service, string? key);

    /// <summary>
    /// Override to configure your IoC container, and anything else
    /// </summary>
    protected virtual void Configure() { }

    /// <summary>
    /// Called on application startup. This occur after this.Args has been assigned, but before the IoC container has been configured
    /// </summary>
    protected virtual void OnStart() { }

    /// <summary>
    /// Returns the currently-displayed window, or null if there is none (or it can't be determined)
    /// </summary>
    public virtual TopLevel? GetActiveWindow()
    {
        if (ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desk)
            return null;

        var win = desk.Windows.OfType<Window>().FirstOrDefault(x => x.IsActive);
        return TopLevel.GetTopLevel(win);
    }

    public sealed override void OnFrameworkInitializationCompleted()
    {
        OnStart();

        T vm;
        try
        {
            vm = IoC.Get<T>();
        }
        catch (Exception e)
        {
            throw new InvalidOperationException(
                $"Unable to resolve the root ViewModel of type '{typeof(T).Name}'. " +
                $"Make sure it is registered with your IoC container (e.g. services.AddSingleton<{typeof(T).Name}>() in ConfigureServices), " +
                "and that base.Initialize() is called from your App's Initialize() method.", e);
        }

        var viewManager = IoC.Get<IViewManager>();
        var view = viewManager.CreateAndBindViewForModelIfNecessary(vm);

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = view as Window;

            // The root ViewModel must have its lifecycle tied to the main window.
            // Without a WindowConductor, ActivateAsync is never called and Screen lifecycle hooks do not run.
            if (view is Window window)
                new WindowConductor(new WindowAdapter(window), vm, SonataLogManager.GetLogger(GetType()));
        }

        if (ApplicationLifetime is ISingleViewApplicationLifetime single)
            single.MainView = view;

        OnFrameworkInitialized();

        base.OnFrameworkInitializationCompleted();
    }

    /// <summary>
    /// Called at the end of framework initialization, after the root view has been displayed.
    /// </summary>
    protected virtual void OnFrameworkInitialized() { }

    /// <summary>
    /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
    /// </summary>
    public virtual void Dispose()
    {
    }
}
