using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Sonata.Avalonia.Hosting.Internal;
using Sonata.Avalonia.Internal;

namespace Sonata.Avalonia.Hosting;

public static class SonataHostingServiceCollectionExtensions
{
    /// <summary>
    /// Ties an <see cref="IHost"/>'s lifecycle to the application: it is started after
    /// framework initialization, and stopped and disposed when the application exits.
    /// </summary>
    public static IServiceCollection AddSonataHosting(this IServiceCollection services, IHost host)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(host);

        services.AddSingleton(host);
        services.AddSingleton<HostRunner>();
        services.AddSingleton<IApplicationLifetimeParticipant, HostLifetimeParticipant>();
        return services;
    }
}
