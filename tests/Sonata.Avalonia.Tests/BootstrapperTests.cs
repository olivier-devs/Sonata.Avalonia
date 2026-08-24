using Microsoft.Extensions.DependencyInjection;
using Sonata.Avalonia;
using Xunit;

namespace Sonata.Avalonia.Tests;

public class BootstrapperTests
{
    [Fact]
    public void ConfigureServices_IsCalledBeforeSonataDefaults()
    {
        var app = new TestApp();
        app.Initialize();

        Assert.Equal(new[] { "ConfigureServices", "ConfigureSonataServices" }, app.CallOrder);
    }

    [Fact]
    public void DefaultServices_AreRegistered()
    {
        var app = new TestApp();
        app.Initialize();

        Assert.IsType<ViewManager>(app.PublicServices.GetRequiredService<IViewManager>());
        Assert.IsType<WindowManager>(app.PublicServices.GetRequiredService<IWindowManager>());
        Assert.IsType<EventAggregator>(app.PublicServices.GetRequiredService<IEventAggregator>());
        Assert.Same(app, app.PublicServices.GetRequiredService<IWindowManagerConfig>());
    }

    [Fact]
    public void UserRegistration_OverridesSonataDefault()
    {
        var app = new TestAppWithCustomEventAggregator();
        app.Initialize();

        Assert.IsType<CustomEventAggregator>(app.PublicServices.GetRequiredService<IEventAggregator>());
    }

    private class CustomEventAggregator : IEventAggregator
    {
        public void Subscribe(IHandle handler, params string[] channels) { }
        public void Unsubscribe(IHandle handler, params string[] channels) { }
        public void PublishWithDispatcher(object message, Action<Action> dispatcher, params string[] channels) { }
    }

    private class TestAppWithCustomEventAggregator : SonataApplication<TestRootViewModel>
    {
        public IServiceProvider PublicServices => Services;

        protected override void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton<IEventAggregator, CustomEventAggregator>();
        }
    }
}