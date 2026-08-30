using Sonata.Avalonia;
using Sonata.Avalonia.Internal;
using Xunit;

namespace Sonata.Avalonia.Tests;

[Collection("Ambient")]
public class ScreenTests
{
    public ScreenTests()
    {
        // Ensure lifecycle events are raised synchronously in unit tests
        UiThreadDispatch.Dispatcher = SynchronousDispatcher.Instance;
    }

    [Fact]
    public async Task ActivateAsync_TransitionsToActive_AndRaisesEvents()
    {
        var screen = new TestScreen();
        var activated = 0;
        var stateChanges = new List<ScreenState>();
        screen.Activated += (o, e) => activated++;
        screen.StateChanged += (o, e) => stateChanges.Add(e.NewState);

        await ((IScreenState)screen).ActivateAsync();

        Assert.Equal(ScreenState.Active, screen.ScreenState);
        Assert.True(screen.IsActive);
        Assert.Equal(1, activated);
        Assert.Contains(ScreenState.Active, stateChanges);
        Assert.Equal(new[] { "OnInitialActivateAsync", "OnActivateAsync" }, screen.Calls);
    }

    [Fact]
    public async Task ActivateAsync_Twice_DoesNotCallOnActivateTwice()
    {
        var screen = new TestScreen();
        await ((IScreenState)screen).ActivateAsync();
        await ((IScreenState)screen).ActivateAsync();

        Assert.Equal(new[] { "OnInitialActivateAsync", "OnActivateAsync" }, screen.Calls);
    }

    [Fact]
    public async Task InitialActivate_CalledOnceEver_ButNotAfterClose()
    {
        var screen = new TestScreen();
        await ((IScreenState)screen).ActivateAsync();
        await ((IScreenState)screen).CloseAsync();
        screen.Calls.Clear();
        await ((IScreenState)screen).ActivateAsync();

        Assert.Equal(1, screen.Calls.Count(c => c == "OnActivateAsync"));
        // haveActivated was reset by Close, so this counts as a fresh initial activation
        Assert.Equal(1, screen.Calls.Count(c => c == "OnInitialActivateAsync"));
    }

    [Fact]
    public async Task DeactivateAsync_FromClosed_GoesThroughActivated()
    {
        var screen = new TestScreen();
        await ((IScreenState)screen).CloseAsync();
        screen.Calls.Clear();

        await ((IScreenState)screen).DeactivateAsync();

        Assert.Equal(ScreenState.Deactivated, screen.ScreenState);
        Assert.Contains("OnActivateAsync", screen.Calls);
        Assert.Contains("OnDeactivateAsync", screen.Calls);
    }

    [Fact]
    public async Task CloseAsync_DetachesView_AndRaisesClosed()
    {
        var screen = new TestScreen();
        ((IViewAware)screen).AttachView(new TestRootView());
        var closed = 0;
        screen.Closed += (o, e) => closed++;

        await ((IScreenState)screen).CloseAsync();

        Assert.Equal(ScreenState.Closed, screen.ScreenState);
        Assert.Null(screen.View);
        Assert.Equal(1, closed);
    }

    [Fact]
    public async Task CloseAsync_ReactivationPossible_AfterClose()
    {
        var screen = new TestScreen();
        await ((IScreenState)screen).ActivateAsync();
        await ((IScreenState)screen).CloseAsync();
        await ((IScreenState)screen).ActivateAsync();

        Assert.Equal(ScreenState.Active, screen.ScreenState);
        Assert.Equal(2, screen.Calls.Count(c => c == "OnActivateAsync"));
    }

    [Fact]
    public async Task CancellationToken_OperationCanceledException_Propagates()
    {
        var screen = new TestScreen { ThrowOperationCanceled = true };

        await Assert.ThrowsAsync<OperationCanceledException>(() => ((IScreenState)screen).ActivateAsync());
    }

    [Fact]
    public async Task DisposeAsync_ClosesIfActive_AndIsIdempotent()
    {
        var screen = new TestScreen();
        await ((IScreenState)screen).ActivateAsync();

        await screen.DisposeAsync();
        Assert.Equal(ScreenState.Closed, screen.ScreenState);

        await screen.DisposeAsync(); // idempotent
        Assert.Equal(ScreenState.Closed, screen.ScreenState);
    }
}
