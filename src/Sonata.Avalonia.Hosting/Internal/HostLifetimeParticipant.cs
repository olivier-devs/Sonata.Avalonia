using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Microsoft.Extensions.Logging;
using Sonata.Avalonia.Internal;

namespace Sonata.Avalonia.Hosting.Internal;

/// <summary>
/// Starts the host after framework initialization, and stops/disposes it when a
/// desktop application exits. Lifecycle operations are serialized by <see cref="HostRunner"/>,
/// so an exit racing with host startup waits for the start to complete. The stop at
/// exit is best-effort: long-running hosted services should honor their cancellation
/// tokens, as the process may terminate before disposal completes.
/// </summary>
internal sealed class HostLifetimeParticipant : IApplicationLifetimeParticipant
{
    private readonly HostRunner _runner;
    private readonly ILogger _logger;

    public HostLifetimeParticipant(HostRunner runner)
    {
        _runner = runner;
        _logger = SonataLogManager.GetLogger(typeof(HostLifetimeParticipant));
    }

    public void OnFrameworkInitialized()
    {
        _ = StartHostAsync();

        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            desktop.Exit += OnApplicationExit;
    }

    private async Task StartHostAsync()
    {
        try
        {
            await _runner.StartAsync();
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to start the host — shutting down the application");
            (Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.Shutdown();
        }
    }

    private async void OnApplicationExit(object? sender, EventArgs e)
    {
        try
        {
            await _runner.DisposeAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to stop the host on application exit");
        }
    }
}
