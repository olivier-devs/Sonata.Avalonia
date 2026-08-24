using Sonata.Avalonia.Internal;

namespace Sonata.Avalonia;

/// <summary>
/// Sonata application bootstrapper based on Microsoft.Extensions.DependencyInjection.
/// Extend your App class from this, and override <see cref="ConfigureServices"/>
/// to register your services.
/// </summary>
public abstract class SonataApplication<T> : SonataApplicationBase<T> where T : class
{
    private IServiceProvider? _services;
    private IServiceCollection? _serviceDescriptors;

    /// <summary>
    /// Gets the application's service provider. Only available after
    /// <see cref="Application.Initialize"/> has run.
    /// </summary>
    protected IServiceProvider Services
    {
        get => _services ?? throw new InvalidOperationException(
            "The service provider has not been created yet. " +
            "Did you forget to call base.Initialize() from your App's Initialize() override?");
        private set => _services = value;
    }

    /// <summary>
    /// Override to register your own services with the service collection.
    /// </summary>
    protected virtual void ConfigureServices(IServiceCollection services) { }

    /// <summary>
    /// Set to false to disable convention-based registration of Views (transient)
    /// and ViewModels (singleton).
    /// </summary>
    protected virtual bool EnableConventionRegistration => true;

    /// <summary>
    /// Override to replace Sonata's default service registrations.
    /// </summary>
    protected virtual void ConfigureSonataServices(IServiceCollection services)
    {
        services.AddSonata(this, ViewAssemblies, EnableConventionRegistration);
    }

    /// <summary>
    /// Assemblies searched for Views and ViewModels. Defaults to the assembly
    /// containing your App class.
    /// </summary>
    protected virtual Assembly[] ViewAssemblies => new[] { GetType().Assembly };

    protected sealed override void Configure()
    {
        var services = new ServiceCollection();
        ConfigureServices(services);
        ConfigureSonataServices(services);
        _serviceDescriptors = services;
        Services = services.BuildServiceProvider();
    }

    protected override object GetInstance(Type type) => Services.GetRequiredService(type);

    protected override object GetInstance(Type service, string? key) => Services.GetRequiredKeyedService(service, key);

    protected override IEnumerable<object> GetInstances(Type service)
    {
        var descriptors = _serviceDescriptors
            ?? throw new InvalidOperationException(
                "The service collection has not been created yet. " +
                "Did you forget to call base.Initialize() from your App's Initialize() override?");

        var nonKeyed = Services.GetServices(service).OfType<object>();
        var keyed = descriptors
            .Where(d => d.ServiceType == service && d.ServiceKey is not null)
            .Select(d => Services.GetRequiredKeyedService(service, d.ServiceKey));

        return nonKeyed.Concat(keyed);
    }

    protected override ILoggerFactory? GetLoggerFactory() => Services.GetRequiredService<ILoggerFactory>();

    protected override void OnFrameworkInitialized()
    {
        foreach (var participant in Services.GetServices<IApplicationLifetimeParticipant>())
            participant.OnFrameworkInitialized();
    }

    /// <inheritdoc/>
    public override void Dispose()
    {
        base.Dispose();
        (_services as IDisposable)?.Dispose();
        _services = null;
        _serviceDescriptors = null;
    }
}