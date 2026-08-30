using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Headless.XUnit;
using Sonata.Avalonia;
using Xunit;

namespace Sonata.Avalonia.Headless.Tests;

public class HostingHeadlessTests
{
    /// <summary>Minimal SonataApplicationBase subclass — never initialized, only used for GetActiveWindow.</summary>
    private sealed class FakeSonataApp : SonataApplicationBase<object>
    {
        protected override IEnumerable<object> GetInstances(Type service) => Array.Empty<object>();
        protected override object GetInstance(Type type) => throw new NotSupportedException();
        protected override object GetInstance(Type service, string? key) => throw new NotSupportedException();
    }

    /// <summary>
    /// Exposes an event with the same shape as IClassicDesktopStyleApplicationLifetime.Exit.
    /// Avalonia 12 marks the lifetime interfaces [NotClientImplementable], and raising the
    /// real lifetime's Exit requires Shutdown — which would shut down the headless session's
    /// dispatcher — so the pattern is documented against an equivalent event here.
    /// </summary>
    private sealed class ExitSource
    {
        public event EventHandler<ControlledApplicationLifetimeExitEventArgs>? Exit;

        public void RaiseExit() => Exit?.Invoke(this, new ControlledApplicationLifetimeExitEventArgs(0));
    }

    [AvaloniaFact]
    public void GetActiveWindow_WithoutClassicDesktopLifetime_ReturnsNull()
    {
        // Arrange — an app which was never initialized has no lifetime at all
        var app = new FakeSonataApp();
        Assert.Null(app.ApplicationLifetime);

        // Act / Assert — a missing lifetime is not a classic desktop lifetime
        Assert.Null(app.GetActiveWindow());

        // Arrange — a classic desktop lifetime with no open windows
        app.ApplicationLifetime = new ClassicDesktopStyleApplicationLifetime();

        // Act / Assert — no active window can be determined
        Assert.Null(app.GetActiveWindow());
    }

    [AvaloniaFact]
    public async Task DesktopLifetime_Exit_AsyncVoidHandler_CompletesWithoutCrashing()
    {
        // Arrange — mirrors HostLifetimeParticipant.OnApplicationExit: an async void
        // handler subscribed to the lifetime's Exit must run to completion without
        // crashing the exit path.
        var source = new ExitSource();
        var release = new TaskCompletionSource<bool>();
        var handlerCompleted = new TaskCompletionSource<bool>();

        async void OnExit(object? sender, EventArgs e)
        {
            try
            {
                await release.Task;
                handlerCompleted.SetResult(true);
            }
            catch (Exception ex)
            {
                handlerCompleted.SetException(ex);
            }
        }

        source.Exit += OnExit;

        // Act — raise Exit: the handler starts and suspends at its await
        source.RaiseExit();
        Assert.False(handlerCompleted.Task.IsCompleted);

        // Let the handler's awaited work complete
        release.SetResult(true);

        // Assert — the handler ran to completion without crashing
        await handlerCompleted.Task;
    }
}
