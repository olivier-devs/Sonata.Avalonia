using Avalonia.Controls;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Sonata.Avalonia;

namespace Sonata.Avalonia.Tests;

public class TestRootViewModel : Screen
{
}

public class TestRootView : UserControl
{
}

public class TestApp : SonataApplication<TestRootViewModel>
{
    public List<string> CallOrder { get; } = new();

    public IServiceProvider PublicServices => Services;

    protected override void ConfigureServices(IServiceCollection services)
    {
        CallOrder.Add("ConfigureServices");
    }

    protected override void ConfigureSonataServices(IServiceCollection services)
    {
        CallOrder.Add("ConfigureSonataServices");
        base.ConfigureSonataServices(services);
    }
}

public class TestAppNoConvention : SonataApplication<TestRootViewModel>
{
    public IServiceProvider? PublicServices
    {
        get
        {
            try { return Services; }
            catch (InvalidOperationException) { return null; }
        }
    }

    protected override bool EnableConventionRegistration => false;
}

public sealed class TestLoggerProvider : ILoggerProvider
{
    public List<(string Category, LogLevel Level, string Message, Exception? Exception)> Entries { get; } = new();

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
            => _owner.Entries.Add((_category, logLevel, formatter(state, exception), exception));
    }
}
