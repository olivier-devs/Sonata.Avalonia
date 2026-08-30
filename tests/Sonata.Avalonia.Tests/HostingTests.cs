using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Sonata.Avalonia;
using Sonata.Avalonia.Hosting;
using Sonata.Avalonia.Hosting.Internal;
using Sonata.Avalonia.Internal;
using Xunit;

namespace Sonata.Avalonia.Tests;

[Collection("Ambient")]
public class HostingTests
{
    private sealed class TestHostedService : IHostedService, IDisposable
    {
        public bool Started { get; private set; }
        public bool Stopped { get; private set; }
        public bool Disposed { get; private set; }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            Started = true;
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            Stopped = true;
            return Task.CompletedTask;
        }

        public void Dispose() => Disposed = true;
    }

    [Fact]
    public async Task HostRunner_StartsAndStopsHost()
    {
        var service = new TestHostedService();
        var host = new HostBuilder()
            .ConfigureServices(s => s.AddSingleton<IHostedService>(service))
            .Build();
        var runner = new HostRunner(host);

        await runner.StartAsync();
        Assert.True(service.Started);

        await runner.DisposeAsync();
        Assert.True(service.Stopped);
        Assert.True(service.Disposed);
    }

    [Fact]
    public void AddSonataHosting_RegistersParticipant()
    {
        var services = new ServiceCollection();
        services.AddSonataHosting(new HostBuilder().Build());
        var provider = services.BuildServiceProvider();

        var participant = provider.GetRequiredService<IApplicationLifetimeParticipant>();

        Assert.IsType<HostLifetimeParticipant>(participant);
    }

    private sealed class SlowStartingHostedService : IHostedService
    {
        private readonly TaskCompletionSource _started = new();
        private readonly TaskCompletionSource _releaseStart = new();

        public Task WaitForStartedAsync() => _started.Task;

        public void ReleaseStart() => _releaseStart.TrySetResult();

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _started.TrySetResult();
            await _releaseStart.Task;
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }

    [Fact]
    public async Task HostRunner_DisposeDuringStart_WaitsForStartToComplete()
    {
        var service = new SlowStartingHostedService();
        var host = new HostBuilder()
            .ConfigureServices(s => s.AddSingleton<IHostedService>(service))
            .Build();
        var runner = new HostRunner(host);

        var startTask = runner.StartAsync();
        await service.WaitForStartedAsync();   // start is in flight, blocked on release

        var disposeTask = runner.DisposeAsync();
        // Give the dispose a chance to (incorrectly) run before start completes —
        // with the fix it must be waiting on the lifecycle lock.
        await Task.Delay(50);

        service.ReleaseStart();                 // let start finish
        await startTask;
        await disposeTask;                      // dispose proceeds after start

        // The host was stopped+disposed without ObjectDisposedException mid-start
        Assert.True(startTask.IsCompletedSuccessfully);
    }

    [Fact]
    public void SonataHostedApplication_StartsHostOnFrameworkInitialized()
    {
        var service = new TestHostedService();
        var app = new TestHostedApp(service);
        app.Initialize();
        app.OnFrameworkInitializationCompleted();

        // The hosted service's StartAsync completes synchronously, so the
        // fire-and-forget start has already run.
        Assert.True(service.Started);
    }

    private sealed class TestHostedApp : SonataHostedApplication<TestRootViewModel>
    {
        private readonly TestHostedService _service;

        public TestHostedApp(TestHostedService service)
        {
            _service = service;
        }

        protected override IHostBuilder CreateHostBuilder()
        {
            var builder = new HostBuilder();
            builder.ConfigureServices(s => s.AddSingleton<IHostedService>(_service));
            return builder;
        }
    }
}
