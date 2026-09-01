using System.Reflection;
using Avalonia.Controls;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Sonata.Avalonia;
using Sonata.Avalonia.Internal;
using Xunit;

namespace Sonata.Avalonia.Tests;

public class LoggingTests
{
    private sealed class TestLoggerProvider : ILoggerProvider
    {
        public List<(string Category, LogLevel Level, string Message)> Entries { get; } = new();

        public ILogger CreateLogger(string categoryName) => new TestLogger(this, categoryName);

        public void Dispose() { }

        private sealed class TestLogger : ILogger
        {
            private readonly TestLoggerProvider _owner;
            private readonly string _category;

            public TestLogger(TestLoggerProvider owner, string category)
            {
                _owner = owner;
                _category = category;
            }

            public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

            public bool IsEnabled(LogLevel logLevel) => true;

            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
                Func<TState, Exception?, string> formatter)
                => _owner.Entries.Add((_category, logLevel, formatter(state, exception)));
        }
    }

    [Fact]
    public void ViewManager_LogsThroughInjectedLogger()
    {
        var provider = new TestLoggerProvider();
        var factory = new LoggerFactory(new[] { provider });
        var config = new ViewManagerConfig()
            .SetViewFactory(type => (Control)Activator.CreateInstance(type)!)
            .AddViewAssembly(typeof(LoggingTests).Assembly);
        var viewManager = new ViewManager(Options.Create(config), factory.CreateLogger<ViewManager>());

        viewManager.CreateAndBindViewForModelIfNecessary(new TestRootViewModel());

        Assert.Contains(provider.Entries,
            e => e.Category.Contains("ViewManager") && e.Level == LogLevel.Information);
    }

    [Fact]
    public void SonataLogManager_GetLogger_ReturnsUsableLogger()
    {
        var logger = SonataLogManager.GetLogger(typeof(LoggingTests));

        logger.LogInformation("This must not throw");

        Assert.NotNull(logger);
    }
}
