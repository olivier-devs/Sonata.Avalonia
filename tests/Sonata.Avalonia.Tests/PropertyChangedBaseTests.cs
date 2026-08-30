using Sonata.Avalonia;
using Sonata.Avalonia.Internal;
using Xunit;

namespace Sonata.Avalonia.Tests;

[Collection("Ambient")]
public class PropertyChangedBaseTests
{
    private class TestVm : PropertyChangedBase
    {
        private string _name = string.Empty;
        public string Name
        {
            get => _name;
            set => SetAndNotify(ref _name, value);
        }

        public void Raise(string name) => NotifyOfPropertyChange(name);
    }

    [Fact]
    public void NotifyOfPropertyChange_RaisesHandler_InlineWithSynchronousDispatcher()
    {
        UiThreadDispatch.Dispatcher = SynchronousDispatcher.Instance;
        try
        {
            var vm = new TestVm();
            var received = new List<string>();
            vm.PropertyChanged += (s, e) => received.Add(e.PropertyName ?? string.Empty);

            vm.Raise("Name");

            Assert.Equal(new[] { "Name" }, received);
        }
        finally
        {
            UiThreadDispatch.Dispatcher = null;
        }
    }

    [Fact]
    public void SetAndNotify_RaisesOnlyWhenValueChanges()
    {
        UiThreadDispatch.Dispatcher = SynchronousDispatcher.Instance;
        try
        {
            var vm = new TestVm();
            var count = 0;
            vm.PropertyChanged += (s, e) => count++;

            vm.Name = "a";
            vm.Name = "a";   // same value: no notification
            vm.Name = "b";

            Assert.Equal(2, count);
        }
        finally
        {
            UiThreadDispatch.Dispatcher = null;
        }
    }

    [Fact]
    public void OnPropertyChanged_UsesPost_NotBlockingSend()
    {
        var recording = new RecordingDispatcher();
        UiThreadDispatch.Dispatcher = recording;
        try
        {
            var vm = new TestVm();
            vm.PropertyChanged += (s, e) => { };
            vm.Raise("Name");

            Assert.Empty(recording.Sent);    // never a blocking Send
            Assert.Single(recording.Posted);
        }
        finally
        {
            UiThreadDispatch.Dispatcher = null;
        }
    }

    [Fact]
    public void ErrorsChanged_IsDispatchedViaPost()
    {
        var recording = new RecordingDispatcher();
        UiThreadDispatch.Dispatcher = recording;
        try
        {
            var vm = new ValidatingScreen(new TestValidator());
            var raised = new List<string>();
            vm.ErrorsChanged += (s, e) => raised.Add(e.PropertyName ?? string.Empty);

            vm.RecordError("Name", new[] { "bad" });

            foreach (var action in recording.Posted)
                action();                     // run the posted notifications

            Assert.Contains("Name", raised);
        }
        finally
        {
            UiThreadDispatch.Dispatcher = null;
        }
    }
}
