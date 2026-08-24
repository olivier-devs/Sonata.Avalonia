using Sonata.Avalonia;
using Xunit;

namespace Sonata.Avalonia.Tests;

public class ConductorTests
{
    public ConductorTests()
    {
        // Ensure collection/lifecycle handlers run synchronously in unit tests
        Execute.Dispatcher = SynchronousDispatcher.Instance;
    }

    [Fact]
    public async Task Conductor_ActivateItemAsync_ReplacesActiveItem()
    {
        var first = new TestScreen();
        var second = new TestScreen();
        var conductor = new Conductor<TestScreen>();
        await ((IScreenState)conductor).ActivateAsync();

        await conductor.ActivateItemAsync(first);
        Assert.Same(first, conductor.ActiveItem);
        Assert.Equal(ScreenState.Active, first.ScreenState);

        await conductor.ActivateItemAsync(second);
        Assert.Same(second, conductor.ActiveItem);
        Assert.Equal(ScreenState.Closed, first.ScreenState);
        Assert.Equal(ScreenState.Active, second.ScreenState);
    }

    [Fact]
    public async Task Conductor_CloseItemAsync_RespectsCanCloseAsync()
    {
        var item = new TestScreen { CanCloseResult = false };
        var conductor = new Conductor<TestScreen>();
        await ((IScreenState)conductor).ActivateAsync();
        await conductor.ActivateItemAsync(item);

        await conductor.CloseItemAsync(item);

        Assert.Same(item, conductor.ActiveItem);
        Assert.Equal(ScreenState.Active, item.ScreenState);
    }

    [Fact]
    public void OneActive_DetermineNextItemToActivate_ReturnsPreviousFirstOrNull()
    {
        var conductor = new TestOneActiveConductor();
        var a = new TestScreen();
        var b = new TestScreen();
        var c = new TestScreen();
        conductor.Items.AddRange(new[] { a, b, c });

        Assert.Same(b, conductor.DetermineNext(a)); // index 0 -> next
        Assert.Same(a, conductor.DetermineNext(b)); // index 1 -> previous
        Assert.Same(b, conductor.DetermineNext(c)); // index 2 -> previous
    }

    [Fact]
    public async Task OneActive_ActivateItemAsync_AddsToItems_AndActivates_WhenActive()
    {
        var conductor = new Conductor<TestScreen>.Collection.OneActive();
        await ((IScreenState)conductor).ActivateAsync();
        var item = new TestScreen();

        await conductor.ActivateItemAsync(item);

        Assert.Contains(item, conductor.Items);
        Assert.Same(item, conductor.ActiveItem);
        Assert.Equal(ScreenState.Active, item.ScreenState);
    }

    [Fact]
    public async Task OneActive_ItemsRemove_ClosesAndCleansUp()
    {
        var conductor = new Conductor<TestScreen>.Collection.OneActive();
        await ((IScreenState)conductor).ActivateAsync();
        var item = new TestScreen();
        await conductor.ActivateItemAsync(item);

        conductor.Items.Remove(item);

        Assert.DoesNotContain(item, conductor.Items);
        Assert.Equal(ScreenState.Closed, item.ScreenState);
        Assert.Null(item.Parent);
    }

    [Fact]
    public async Task AllActive_ActivatesAndDeactivatesAllItems()
    {
        var a = new TestScreen();
        var b = new TestScreen();
        var conductor = new Conductor<TestScreen>.Collection.AllActive();
        conductor.Items.Add(a);
        conductor.Items.Add(b);

        await ((IScreenState)conductor).ActivateAsync();
        Assert.Equal(ScreenState.Active, a.ScreenState);
        Assert.Equal(ScreenState.Active, b.ScreenState);

        await ((IScreenState)conductor).DeactivateAsync();
        Assert.Equal(ScreenState.Deactivated, a.ScreenState);
        Assert.Equal(ScreenState.Deactivated, b.ScreenState);
    }

    [Fact]
    public async Task StackNavigation_GoBackAsync_ReturnsToHistory()
    {
        var first = new TestScreen();
        var second = new TestScreen();
        var conductor = new Conductor<TestScreen>.StackNavigation();
        await ((IScreenState)conductor).ActivateAsync();

        await conductor.ActivateItemAsync(first);
        await conductor.ActivateItemAsync(second);
        Assert.Same(second, conductor.ActiveItem);

        await conductor.GoBackAsync();
        Assert.Same(first, conductor.ActiveItem);
    }

    [Fact]
    public async Task DisposeChildren_True_DisposesChildren_OnClose()
    {
        var child = new DisposableItem();
        var conductor = new Conductor<DisposableItem>();
        await conductor.ActivateItemAsync(child);

        await ((IScreenState)conductor).CloseAsync();

        Assert.True(child.Disposed);
    }

    [Fact]
    public async Task DisposeChildren_False_DoesNotDisposeChildren_OnClose()
    {
        var child = new DisposableItem();
        var conductor = new Conductor<DisposableItem> { DisposeChildren = false };
        await conductor.ActivateItemAsync(child);

        await ((IScreenState)conductor).CloseAsync();

        Assert.False(child.Disposed);
    }

    [Fact]
    public async Task ConcurrentActivateItemAsync_DoNotInterleave()
    {
        var a = new TestScreen { ActivateGate = new TaskCompletionSource<bool>() };
        var b = new TestScreen();
        var conductor = new Conductor<TestScreen>();
        await ((IScreenState)conductor).ActivateAsync();

        var first = conductor.ActivateItemAsync(a);
        var second = conductor.ActivateItemAsync(b);

        // first is still in-flight (blocked on a's gate): b must not have been activated yet
        Assert.Same(a, conductor.ActiveItem);

        a.ActivateGate.SetResult(true);
        await first;
        await second;

        Assert.Same(b, conductor.ActiveItem);
        Assert.Equal(ScreenState.Closed, a.ScreenState);
    }

    [Fact]
    public async Task ActiveItemTransition_CompletesAfterTransition()
    {
        var a = new TestScreen { ActivateGate = new TaskCompletionSource<bool>() };
        var conductor = new Conductor<TestScreen>();
        await ((IScreenState)conductor).ActivateAsync();

        var pending = conductor.ActivateItemAsync(a);
        Assert.False(pending.IsCompleted);

        a.ActivateGate.SetResult(true);
        await conductor.ActiveItemTransition;

        Assert.True(pending.IsCompleted);
        Assert.Same(a, conductor.ActiveItem);
    }

    [Fact]
    public async Task ReentrantActivateItemAsync_ThrowsClearException()
    {
        var conductor = new Conductor<ReentrantScreen>();
        await ((IScreenState)conductor).ActivateAsync();

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => conductor.ActivateItemAsync(new ReentrantScreen(conductor)));

        Assert.Contains("transition", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    private class ReentrantScreen : Screen
    {
        private readonly IConductor<ReentrantScreen> _conductor;

        public ReentrantScreen() { }

        public ReentrantScreen(IConductor<ReentrantScreen> conductor)
        {
            _conductor = conductor;
        }

        protected override Task OnActivateAsync(CancellationToken ct)
        {
            // Re-entrant call: this hook runs inside the conductor's transition
            if (_conductor != null)
                _ = _conductor.ActivateItemAsync(this);
            return Task.CompletedTask;
        }
    }
}
