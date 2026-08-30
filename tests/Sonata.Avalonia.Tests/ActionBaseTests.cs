using Microsoft.Extensions.Logging;
using Sonata.Avalonia.Internal;
using Sonata.Avalonia.Xaml;
using Xunit;

namespace Sonata.Avalonia.Tests;

public class ActionBaseTests
{
    public class TestActions
    {
        public Task FailingAction() => Task.FromException(new InvalidOperationException("boom"));
        public void SyncAction() { }
        public Task OkAction() => Task.CompletedTask;
    }

    [Fact]
    public void AsyncAction_ThrowingException_IsLogged()
    {
        var provider = new TestLoggerProvider();
        var factory = new LoggerFactory(new[] { provider });
        SonataLogManager.SetFactory(factory);
        try
        {
            var target = new TestActions();
            var action = new CommandAction(target, "FailingAction", ActionUnavailableBehaviour.Throw, ActionUnavailableBehaviour.Throw);
            action.Execute(null);

            Assert.Contains(provider.Entries, e => e.Level == LogLevel.Error && e.Exception is AggregateException agg && agg.InnerException is InvalidOperationException);
        }
        finally
        {
            SonataLogManager.SetFactory(null);
        }
    }

    [Fact]
    public void SyncAction_Unchanged()
    {
        var provider = new TestLoggerProvider();
        var factory = new LoggerFactory(new[] { provider });
        SonataLogManager.SetFactory(factory);
        try
        {
            var target = new TestActions();
            var action = new CommandAction(target, "SyncAction", ActionUnavailableBehaviour.Throw, ActionUnavailableBehaviour.Throw);
            action.Execute(null);

            Assert.DoesNotContain(provider.Entries, e => e.Level == LogLevel.Error);
        }
        finally
        {
            SonataLogManager.SetFactory(null);
        }
    }

    [Fact]
    public void OkAction_CompletedTask_NoErrorLogged()
    {
        var provider = new TestLoggerProvider();
        var factory = new LoggerFactory(new[] { provider });
        SonataLogManager.SetFactory(factory);
        try
        {
            var target = new TestActions();
            var action = new CommandAction(target, "OkAction", ActionUnavailableBehaviour.Throw, ActionUnavailableBehaviour.Throw);
            action.Execute(null);

            Assert.DoesNotContain(provider.Entries, e => e.Level == LogLevel.Error);
        }
        finally
        {
            SonataLogManager.SetFactory(null);
        }
    }
}
