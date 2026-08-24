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

public class TestScreen : Screen
{
    public List<string> Calls { get; } = new();
    public int ViewLoadedCount { get; private set; }
    public bool ThrowOnActivate { get; set; }
    public bool ThrowOperationCanceled { get; set; }
    public bool? CanCloseResult { get; set; }
    public TaskCompletionSource<bool>? ActivateGate { get; set; }

    public override Task<bool> CanCloseAsync(CancellationToken ct = default)
        => Task.FromResult(CanCloseResult ?? true);

    protected override Task OnInitialActivateAsync(CancellationToken ct)
    {
        Calls.Add("OnInitialActivateAsync");
        return Task.CompletedTask;
    }

    protected override Task OnActivateAsync(CancellationToken ct)
    {
        Calls.Add("OnActivateAsync");
        if (ThrowOperationCanceled)
            throw new OperationCanceledException();
        if (ThrowOnActivate)
            throw new InvalidOperationException("boom");
        return ActivateGate?.Task ?? Task.CompletedTask;
    }

    protected override Task OnDeactivateAsync(CancellationToken ct)
    {
        Calls.Add("OnDeactivateAsync");
        return Task.CompletedTask;
    }

    protected override Task OnCloseAsync(CancellationToken ct)
    {
        Calls.Add("OnCloseAsync");
        return Task.CompletedTask;
    }

    protected override void OnViewLoaded() => ViewLoadedCount++;
}

public class DisposableItem : IAsyncDisposable
{
    public bool Disposed { get; private set; }

    public ValueTask DisposeAsync()
    {
        Disposed = true;
        return default;
    }
}

public class TestOneActiveConductor : Conductor<TestScreen>.Collection.OneActive
{
    public TestScreen DetermineNext(TestScreen itemToRemove) => DetermineNextItemToActivate(itemToRemove);
}

public class TestValidator : IModelValidator
{
    public Dictionary<string, string[]> Errors { get; } = new();
    public string LastValidatedProperty { get; private set; }

    public void Initialize(object subject) { }

    public Task<IEnumerable<string>> ValidatePropertyAsync(string propertyName)
    {
        LastValidatedProperty = propertyName;
        return Task.FromResult<IEnumerable<string>>(
            Errors.TryGetValue(propertyName, out var e) ? e : null);
    }

    public Task<Dictionary<string, IEnumerable<string>>> ValidateAllPropertiesAsync()
    {
        return Task.FromResult(Errors.ToDictionary(k => k.Key, v => (IEnumerable<string>)v.Value));
    }
}

public class ValidatingScreen : Screen
{
    public ValidatingScreen(IModelValidator validator) : base(validator) { }

    private string _name;
    public string Name
    {
        get => _name;
        set => SetAndNotify(ref _name, value);
    }

    public void RecordError(string propertyName, string[] errors) => RecordPropertyError(propertyName, errors);
    public void ClearErrors() => ClearAllPropertyErrors();
}

public class TestMessageHandler : IHandle<string>
{
    public int Received { get; private set; }

    public void Handle(string message) => Received++;
}
