using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Sonata.Avalonia.Hosting.Internal;

/// <summary>
/// Owns an <see cref="IHost"/>'s lifecycle: start, stop, dispose. All lifecycle
/// operations are serialized so that a stop/dispose triggered while a start is
/// still in flight waits for the start to complete first. Kept separate from
/// application wiring so it is unit-testable without a UI.
/// </summary>
internal sealed class HostRunner
{
    private readonly IHost _host;
    private readonly SemaphoreSlim _lifecycleLock = new(1, 1);
    private Task? _startTask;

    public HostRunner(IHost host)
    {
        _host = host ?? throw new ArgumentNullException(nameof(host));
    }

    /// <summary>
    /// Starts the host. If a start is already in flight (or completed), awaits it
    /// rather than starting again.
    /// </summary>
    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        await _lifecycleLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            _startTask ??= _host.StartAsync(cancellationToken);
            await _startTask.ConfigureAwait(false);
        }
        finally
        {
            _lifecycleLock.Release();
        }
    }

    /// <summary>
    /// Stops the host if it was started. Waits for any in-flight start first.
    /// </summary>
    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        await _lifecycleLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_startTask is not null)
                await _host.StopAsync(cancellationToken).ConfigureAwait(false);
            _startTask = null;
        }
        finally
        {
            _lifecycleLock.Release();
        }
    }

    /// <summary>
    /// Stops and disposes the host. Waits for any in-flight start first, so a
    /// start racing with application exit cannot be disposed mid-flight.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        await _lifecycleLock.WaitAsync().ConfigureAwait(false);
        try
        {
            var hostedServices = _host.Services.GetServices<IHostedService>().ToList();

            if (_startTask is not null)
                await _host.StopAsync().ConfigureAwait(false);
            _startTask = null;

            _host.Dispose();

            // The host's service provider does not dispose instance-registered
            // hosted services, so dispose them explicitly (idempotent by convention).
            foreach (var hostedService in hostedServices)
                (hostedService as IDisposable)?.Dispose();
        }
        finally
        {
            _lifecycleLock.Release();
        }
    }
}
