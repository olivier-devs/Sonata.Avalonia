using Sonata.Avalonia.Internal;
using Xunit;

namespace Sonata.Avalonia.Tests;

public class EventAggregatorDispatchTests
{
    private class ObjectHandler : IHandle<object>
    {
        public List<object> Received { get; } = new();
        public void Handle(object message) => Received.Add(message);
    }

    [Fact]
    public void PublishOnUIThread_UsesAmbientUiThreadDispatch()
    {
        var recording = new RecordingDispatcher { IsCurrent = false };
        UiThreadDispatch.Dispatcher = recording;
        try
        {
            var aggregator = new EventAggregator();
            var handler = new ObjectHandler();
            aggregator.Subscribe(handler);
            aggregator.PublishOnUIThread(new object());

            // With the ambient dispatcher (IsCurrent = false), OnUIThread posts the action
            Assert.Empty(handler.Received);
            Assert.Single(recording.Posted);
            recording.Posted[0]();
            Assert.Single(handler.Received);
        }
        finally
        {
            UiThreadDispatch.Dispatcher = null;
        }
    }
}
