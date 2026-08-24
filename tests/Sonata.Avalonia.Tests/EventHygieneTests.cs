using System.Runtime.CompilerServices;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Sonata.Avalonia;
using Xunit;

namespace Sonata.Avalonia.Tests;

public class EventHygieneTests
{
    public EventHygieneTests()
    {
        // Ensure lifecycle events are raised synchronously in unit tests.
        // BootstrapperTests run TestApp.Initialize() which sets the static
        // Execute.Dispatcher to an ApplicationDispatcher (async); without this
        // guard, the ActivateWith test's PostToUIThread(Activated) would be
        // queued but never pumped headlessly. Matches ScreenTests/ConductorTests.
        Execute.Dispatcher = SynchronousDispatcher.Instance;
    }

    [Fact]
    public void AttachView_LoadedHandler_IsOneShot()
    {
        var screen = new TestScreen();
        var view = new TestRootView();
        ((IViewAware)screen).AttachView(view);

        view.RaiseEvent(new RoutedEventArgs(Control.LoadedEvent));
        view.RaiseEvent(new RoutedEventArgs(Control.LoadedEvent));

        Assert.Equal(1, screen.ViewLoadedCount);
    }

    [Fact]
    public async Task AttachView_CloseAsync_RemovesLoadedHandler()
    {
        var screen = new TestScreen();
        var view = new TestRootView();
        ((IViewAware)screen).AttachView(view);

        await ((IScreenState)screen).CloseAsync();

        view.RaiseEvent(new RoutedEventArgs(Control.LoadedEvent));
        Assert.Equal(0, screen.ViewLoadedCount);
    }

    [Fact]
    public async Task ActivateWith_ActivatesChild_WhenParentActivated()
    {
        var child = new TestScreen();
        var parent = new TestScreen();
        child.ActivateWith(parent);

        await ((IScreenState)parent).ActivateAsync();

        Assert.Equal(ScreenState.Active, child.ScreenState);
    }

    [Fact]
    public void EventAggregator_WeakHandler_IsCollected()
    {
        var aggregator = new EventAggregator();
        var weak = SubscribeAndDrop(aggregator);

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        Assert.False(weak.IsAlive);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference SubscribeAndDrop(EventAggregator aggregator)
    {
        var handler = new TestMessageHandler();
        aggregator.Subscribe(handler);
        return new WeakReference(handler);
    }
}
