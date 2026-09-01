using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Sonata.Avalonia.StyletIoC;

/// <summary>
/// StyletApplication to be extended by any application which wants to use StyletIoC, but doesn't have a root ViewModel
/// </summary>
/// <remarks>
/// You would normally use <see cref="StyletApplication"/>, which lets you specify the root ViewModel
/// to display. If you don't want to show a window on startup, override <see cref="SonataApplicationBase"/>
/// but don't call <see cref="SonataApplicationBase.DisplayRootView()"/>. 
/// </remarks>
public abstract class StyletApplication<T> : SonataApplicationBase<T> where T : class
{
    /// <summary>
    /// Gets or sets the StyletApplication's IoC container. This is created after ConfigureIoC has been run.
    /// </summary>
    protected IContainer Container { get; private set; } = null!; // Initialized in Configure() before use

    /// <summary>
    /// Overridden from SonataApplicationBase, this sets up the IoC container
    /// </summary>
    protected sealed override void Configure()
    {
        var builder = new StyletIoCBuilder
        {
            Assemblies = new List<Assembly> { GetType().Assembly }
        };

        // Call DefaultConfigureIoC *after* ConfigureIoIC, so that they can customize builder.Assemblies
        ConfigureIoC(builder);
        Container = builder.BuildContainer();
    }

    /// <summary>
    /// Override to add your own types to the IoC container.
    /// </summary>
    /// <param name="builder">StyletIoC builder to use to configure the container</param>
    protected virtual void ConfigureIoC(IStyletIoCBuilder builder)
    {
        // Mark these as weak-bindings, so the user can replace them if they want
        var viewManagerConfig = new ViewManagerConfig()
            .AddViewAssembly(GetType().Assembly)
            .SetViewFactory(GetInstance);
        builder.Bind<IOptions<ViewManagerConfig>>().ToInstance(Options.Create(viewManagerConfig)).AsWeakBinding();

        builder.Bind<IDispatcher>().ToInstance(new ApplicationDispatcher()).DisposeWithContainer(false).AsWeakBinding();

        // Bind it to both IViewManager and to itself, so that people can get it with Container.Get<ViewManager>()
        builder.Bind<IViewManager>().And<ViewManager>().To<ViewManager>().InSingletonScope().AsWeakBinding();

        builder.Bind<IWindowManagerConfig>().ToInstance(this).DisposeWithContainer(false).AsWeakBinding();
        builder.Bind<IWindowManager>().To<WindowManager>().InSingletonScope().AsWeakBinding();
        builder.Bind<IEventAggregator>().To<EventAggregator>().InSingletonScope().AsWeakBinding();
        builder.Bind<IMessageBoxViewModel>().To<MessageBoxViewModel>().AsWeakBinding();
        builder.Bind<Func<IMessageBoxViewModel>>().ToFactory<Func<IMessageBoxViewModel>>(c => () => c.Get<IMessageBoxViewModel>()).AsWeakBinding();
        // Stylet's assembly isn't added to the container, so add this explicitly
        builder.Bind<MessageBoxView>().ToSelf();

        // Logging: MEL integration (weak bindings so users can replace them)
        builder.Bind<ILoggerFactory>().ToInstance(NullLoggerFactory.Instance).AsWeakBinding();
        builder.Bind(typeof(ILogger<>)).To(typeof(Logger<>)).AsWeakBinding();

        builder.Autobind(GetType().Assembly);
    }

    protected override object GetInstance(Type service, string? key)
    {
        return Container.Get(service);
    }

    protected override IEnumerable<object> GetInstances(Type service)
    {
        return Container.GetAll(service);
    }

    /// <summary>
    /// Given a type, use the IoC container to fetch an instance of it
    /// </summary>
    /// <param name="type">Type to fetch</param>
    /// <returns>Fetched instance</returns>
    /// <inheritdoc/>
    protected override ILoggerFactory? GetLoggerFactory() => Container.Get<ILoggerFactory>();

    protected override object GetInstance(Type type)
    {
        return Container.Get(type);
    }

    /// <summary>
    /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
    /// </summary>
    public override void Dispose()
    {
        base.Dispose();

        // Dispose the container last
        if (Container != null)
            Container.Dispose();
    }
}