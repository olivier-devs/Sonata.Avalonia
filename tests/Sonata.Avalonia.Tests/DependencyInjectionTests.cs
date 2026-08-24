using Microsoft.Extensions.DependencyInjection;
using Sonata.Avalonia;
using Xunit;

namespace Sonata.Avalonia.Tests;

public class DependencyInjectionTests
{
    private class KeyedTestApp : SonataApplication<TestRootViewModel>
    {
        public IServiceProvider PublicServices => Services;

        public object ResolveKeyed(Type type, string? key) => GetInstance(type, key);

        public IEnumerable<object> ResolveAll(Type type) => GetInstances(type);

        protected override void ConfigureServices(IServiceCollection services)
        {
            services.AddKeyedSingleton<IEventAggregator, CustomEventAggregator>("keyed");
            services.AddSingleton<IEventAggregator, EventAggregator>();
        }
    }

    private class CustomEventAggregator : IEventAggregator
    {
        public void Subscribe(IHandle handler, params string[] channels) { }
        public void Unsubscribe(IHandle handler, params string[] channels) { }
        public void PublishWithDispatcher(object message, Action<Action> dispatcher, params string[] channels) { }
    }

    [Fact]
    public void GetInstance_WithKey_ResolvesKeyedService()
    {
        var app = new KeyedTestApp();
        app.Initialize();

        var resolved = app.ResolveKeyed(typeof(IEventAggregator), "keyed");

        Assert.IsType<CustomEventAggregator>(resolved);
    }

    [Fact]
    public void GetInstances_ResolvesAllRegistrations()
    {
        var app = new KeyedTestApp();
        app.Initialize();

        var all = app.ResolveAll(typeof(IEventAggregator)).ToList();

        Assert.Equal(2, all.Count);
    }

    [Fact]
    public void Resolution_BeforeInitialize_ThrowsHelpfulError()
    {
        var app = new KeyedTestApp();

        var ex = Assert.Throws<InvalidOperationException>(() => app.ResolveKeyed(typeof(IEventAggregator), null));

        Assert.Contains("base.Initialize()", ex.Message);
    }
}