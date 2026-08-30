using Sonata.Avalonia;
using Sonata.Avalonia.Internal;
using Xunit;

namespace Sonata.Avalonia.Tests;

[Collection("Ambient")]
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
        try
        {
            var executed = false;
            UiThreadDispatch.PostToUIThread(() => executed = true);

            Assert.Single(recording.Posted);
            recording.Posted[0]();
            Assert.True(executed);
        }
        finally
        {
            UiThreadDispatch.Dispatcher = null;
        }
    }

    [Fact]
    public void UiThreadDispatch_OnUIThread_RunsInlineWhenCurrent()
    {
        var recording = new RecordingDispatcher { IsCurrent = true };
        UiThreadDispatch.Dispatcher = recording;
        try
        {
            var executed = false;
            UiThreadDispatch.OnUIThread(() => executed = true);

            Assert.Empty(recording.Posted);   // no Post: runs inline
            Assert.True(executed);
        }
        finally
        {
            UiThreadDispatch.Dispatcher = null;
        }
    }

    [Fact]
    public void UiThreadDispatch_OnUIThread_PostsWhenNotCurrent()
    {
        var recording = new RecordingDispatcher { IsCurrent = false };
        UiThreadDispatch.Dispatcher = recording;
        try
        {
            var executed = false;
            UiThreadDispatch.OnUIThread(() => executed = true);

            Assert.Single(recording.Posted);
            Assert.False(executed);   // not run inline: queued on the dispatcher
        }
        finally
        {
            UiThreadDispatch.Dispatcher = null;
        }
    }

    [Fact]
    public void ExecuteFacade_DelegatesToUiThreadDispatch()
    {
        var recording = new RecordingDispatcher();
        UiThreadDispatch.Dispatcher = recording;
        try
        {
#pragma warning disable CS0618 // Type or member is obsolete
            Execute.OnUIThread(() => { });
            Execute.Dispatcher = recording;
#pragma warning restore CS0618

            Assert.Single(recording.Posted);
            Assert.Same(recording, UiThreadDispatch.Dispatcher);
        }
        finally
        {
            UiThreadDispatch.Dispatcher = null;
        }
    }
}
