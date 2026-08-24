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
    public static IServiceCollection AddSonata(
        this IServiceCollection services,
        IWindowManagerConfig windowManagerConfig,
        IEnumerable<Assembly> viewAssemblies)
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
        services.TryAddSingleton<IViewManager, ViewManager>();
        services.TryAddSingleton<ViewManager>();
        services.TryAddSingleton<IWindowManagerConfig>(windowManagerConfig);
        services.TryAddSingleton<IWindowManager, WindowManager>();
        services.TryAddSingleton<IEventAggregator, EventAggregator>();
        services.TryAddTransient<IMessageBoxViewModel, MessageBoxViewModel>();
        services.TryAddTransient<MessageBoxView>();
        services.TryAddSingleton<ILoggerFactory, LoggerFactory>();
        services.TryAdd(ServiceDescriptor.Transient(typeof(ILogger<>), typeof(Logger<>)));

        return services;
    }
}