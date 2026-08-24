using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Sonata.Avalonia.Hosting;

/// <summary>
/// A <see cref="SonataApplication{T}"/> which runs a Generic Host alongside the UI.
/// Override <see cref="ConfigureHost"/> to register <c>IHostedService</c>s.
/// </summary>
public abstract class SonataHostedApplication<T> : SonataApplication<T> where T : class
{
    /// <summary>
    /// Override to configure the host (register hosted services, logging, configuration...).
    /// </summary>
    protected virtual void ConfigureHost(IHostBuilder hostBuilder) { }

    /// <summary>
    /// Creates the host builder. Defaults to <c>Host.CreateDefaultBuilder()</c>.
    /// </summary>
    protected virtual IHostBuilder CreateHostBuilder() => Host.CreateDefaultBuilder();

    protected override void ConfigureSonataServices(IServiceCollection services)
    {
        var builder = CreateHostBuilder();
        ConfigureHost(builder);
        services.AddSonataHosting(builder.Build());
        base.ConfigureSonataServices(services);
    }
}
