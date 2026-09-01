namespace Sonata.Avalonia;

/// <summary>
/// Extension methods for configuring <see cref="ViewManagerConfig"/> through the
/// Microsoft.Extensions.Options pattern.
/// </summary>
public static class ViewManagerServiceCollectionExtensions
{
    /// <summary>
    /// Configures <see cref="ViewManagerConfig"/> via the standard options pattern.
    /// Multiple calls are cumulative and apply in registration order.
    /// </summary>
    /// <param name="services">Service collection to add configuration to.</param>
    /// <param name="configure">Configuration delegate applied to <see cref="ViewManagerConfig"/>.</param>
    /// <returns>The same <see cref="IServiceCollection"/> instance.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="services"/> or <paramref name="configure"/> is null.</exception>
    public static IServiceCollection ConfigureViewManager(this IServiceCollection services, Action<ViewManagerConfig> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);
        services.Configure<ViewManagerConfig>(configure);
        return services;
    }
}
