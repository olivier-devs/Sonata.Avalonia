using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Sonata.Avalonia;

/// <summary>
/// Extension methods for registering Sonata's default services.
/// </summary>
public static class SonataServiceCollectionExtensions
{
    /// <summary>
    /// Adds Sonata's default services. All registrations use TryAdd semantics:
    /// anything the application registered beforehand takes precedence.
    /// </summary>
    /// <param name="services">Service collection to add registrations to.</param>
    /// <param name="windowManagerConfig">The application, used to resolve the active window.</param>
    /// <param name="viewAssemblies">Assemblies searched for Views and ViewModels.</param>
    /// <param name="enableConventionRegistration">When true, scans assemblies for Views (transient) and ViewModels (singleton) and registers them.</param>
    /// <param name="viewModelNameSuffix">Suffix used to identify ViewModel types during convention scanning.</param>
    public static IServiceCollection AddSonata(
        this IServiceCollection services,
        IWindowManagerConfig windowManagerConfig,
        IEnumerable<Assembly> viewAssemblies,
        bool enableConventionRegistration = true,
        string viewModelNameSuffix = "ViewModel")
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(windowManagerConfig);
        ArgumentNullException.ThrowIfNull(viewAssemblies);

        var assemblies = viewAssemblies.Distinct().ToList();

        services.TryAddSingleton(sp => new ViewManagerConfig
        {
            ViewFactory = type => sp.GetRequiredService(type),
            ViewAssemblies = assemblies,
        });
        services.TryAddSingleton<IDispatcher>(_ => new ApplicationDispatcher());
        services.TryAddSingleton<IViewManager, ViewManager>();
        services.TryAddSingleton<ViewManager>();
        services.TryAddSingleton<IWindowManagerConfig>(windowManagerConfig);
        services.TryAddSingleton<IWindowManager, WindowManager>();
        services.TryAddSingleton<IEventAggregator, EventAggregator>();
        services.TryAddTransient<IMessageBoxViewModel, MessageBoxViewModel>();
        services.TryAddTransient<Func<IMessageBoxViewModel>>(sp => () => sp.GetRequiredService<IMessageBoxViewModel>());
        services.TryAddTransient<MessageBoxView>();
        services.TryAddSingleton<ILoggerFactory, LoggerFactory>();
        services.TryAdd(ServiceDescriptor.Transient(typeof(ILogger<>), typeof(Logger<>)));

        if (enableConventionRegistration)
        {
            foreach (var type in assemblies.SelectMany(GetLoadableTypes))
            {
                if (type.IsAbstract || type.IsInterface)
                    continue;

                if (typeof(Control).IsAssignableFrom(type))
                    services.TryAddTransient(type);
                else if (type.Name.EndsWith(viewModelNameSuffix, StringComparison.Ordinal))
                    services.TryAddSingleton(type);
            }
        }

        return services;
    }

    private static IEnumerable<Type> GetLoadableTypes(Assembly assembly)
    {
        try
        {
            return assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException e)
        {
            return e.Types.Where(t => t is not null).Select(t => t!);
        }
    }
}