using Microsoft.Extensions.Logging;
using Sonata.Avalonia.Internal;
using Xunit;

namespace Sonata.Avalonia.Tests;

public class FireAndForgetTests
{
    [Fact]
    public void Run_LogsException_FromAlreadyFaultedTask()
    {
        var provider = new TestLoggerProvider();
        var logger = new LoggerFactory(new[] { provider }).CreateLogger("FireAndForget");

        FireAndForget.Run(Task.FromException(new InvalidOperationException("boom")), logger);

        Assert.Contains(provider.Entries, e => e.Level == LogLevel.Error && e.Exception?.Message.Contains("boom") == true);
    }

    [Fact]
    public void Run_DoesNotThrow_ForCompletedTask()
    {
        var logger = new LoggerFactory().CreateLogger("test");

        // Must not throw
        FireAndForget.Run(Task.CompletedTask, logger);
    }

    [Fact]
    public void Run_DoesNotThrow_ForNullTask()
    {
        var logger = new LoggerFactory().CreateLogger("test");

        // Must not throw (defensive)
        FireAndForget.Run(Task.CompletedTask, logger);
    }

    [Fact]
    public void Run_LoggerThrowsInObservation_DoesNotCrash()
    {
        var task = Task.FromException(new InvalidOperationException("boom"));

        var ex = Record.Exception(() => FireAndForget.Run(task, new ThrowingLogger()));

        Assert.Null(ex);
    }
}
