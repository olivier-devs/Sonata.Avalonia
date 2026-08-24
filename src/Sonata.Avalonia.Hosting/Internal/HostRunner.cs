using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Sonata.Avalonia.Hosting.Internal;

/// <summary>
/// Owns an <see cref="IHost"/>'s lifecycle: start, stop, dispose. Kept separate from
/// application wiring so it is unit-testable without a UI.
/// </summary>
internal sealed class HostRunner
{
    private readonly IHost _host;
    private bool _started;

    public HostRunner(IHost host)
    {
        _host = host ?? throw new ArgumentNullException(nameof(host));
    }

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        if (_started)
            return;

        await _host.StartAsync(cancellationToken);
        _started = true;
    }

    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        if (!_started)
            return;

        _started = false;
        await _host.StopAsync(cancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        var hostedServices = _host.Services.GetServices<IHostedService>().ToList();

        await StopAsync();
        _host.Dispose();

        foreach (var hostedService in hostedServices)
            (hostedService as IDisposable)?.Dispose();
    }
}
