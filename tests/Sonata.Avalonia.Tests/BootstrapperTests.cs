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

    [Fact]
    public void RootViewModel_IsResolvedFromContainer()
    {
        var app = new TestApp();
        app.Initialize();

        var vm1 = app.PublicServices.GetRequiredService<TestRootViewModel>();
        var vm2 = app.PublicServices.GetRequiredService<TestRootViewModel>();

        Assert.Same(vm1, vm2);
    }

    [Fact]
    public void Convention_Views_AreRegisteredAsTransient()
    {
        var app = new TestApp();
        app.Initialize();

        var v1 = app.PublicServices.GetRequiredService<TestRootView>();
        var v2 = app.PublicServices.GetRequiredService<TestRootView>();

        Assert.NotSame(v1, v2);
    }

    [Fact]
    public void Convention_CanBeDisabled()
    {
        var app = new TestAppNoConvention();
        app.Initialize();

        Assert.Null(app.PublicServices.GetService<TestRootViewModel>());
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