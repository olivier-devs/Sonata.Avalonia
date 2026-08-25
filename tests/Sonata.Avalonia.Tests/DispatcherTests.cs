using Sonata.Avalonia;
using Sonata.Avalonia.Internal;
using Xunit;

namespace Sonata.Avalonia.Tests;

public class DispatcherTests
{
    [Fact]
    public void SynchronousDispatcher_ExecutesActionsInline()
    {
        var dispatcher = SynchronousDispatcher.Instance;

        var executed = false;
        dispatcher.Post(() => executed = true);

        Assert.True(executed);
        Assert.True(dispatcher.IsCurrent);
    }

    [Fact]
    public void UiThreadDispatch_Post_RunsActionOnDispatcher()
    {
        var recording = new RecordingDispatcher();
        UiThreadDispatch.Dispatcher = recording;

        var executed = false;
        UiThreadDispatch.PostToUIThread(() => executed = true);

        Assert.Single(recording.Posted);
        recording.Posted[0]();
        Assert.True(executed);

        UiThreadDispatch.Dispatcher = null;
    }

    [Fact]
    public void UiThreadDispatch_OnUIThread_RunsInlineWhenCurrent()
    {
        var recording = new RecordingDispatcher { IsCurrent = true };
        UiThreadDispatch.Dispatcher = recording;

        var executed = false;
        UiThreadDispatch.OnUIThread(() => executed = true);

        Assert.Empty(recording.Posted);   // pas de Post : execution inline
        Assert.True(executed);

        UiThreadDispatch.Dispatcher = null;
    }
}
