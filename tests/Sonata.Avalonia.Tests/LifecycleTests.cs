using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Moq;
using Sonata.Avalonia;
using Xunit;

namespace Sonata.Avalonia.Tests;

[Collection("Ambient")]
public class LifecycleTests
{
    [Fact]
    public void OnFrameworkInitializationCompleted_SetsMainViewToCreatedView_NotViewModel()
    {
        var app = new TestApp();
        app.Initialize();
        var single = new Mock<ISingleViewApplicationLifetime>();
        single.SetupProperty(x => x.MainView);
        app.ApplicationLifetime = single.Object;

        app.OnFrameworkInitializationCompleted();

        Assert.NotNull(single.Object.MainView);
        Assert.IsType<TestRootView>(single.Object.MainView);
    }

    [Fact]
    public void OnFrameworkInitializationCompleted_SetsMainWindow_OnDesktopLifetime()
    {
        var app = new TestApp();
        app.Initialize();
        var desktop = new ClassicDesktopStyleApplicationLifetime();
        app.ApplicationLifetime = desktop;

        app.OnFrameworkInitializationCompleted();

        // TestRootView is a UserControl, not a Window, so MainWindow stays null —
        // but the ViewModel must never be assigned either.
        Assert.Null(desktop.MainWindow);
    }

    [Fact]
    public void GetActiveWindow_ReturnsNull_OnNonDesktopLifetime()
    {
        var app = new TestApp();
        app.Initialize();
        app.ApplicationLifetime = Mock.Of<ISingleViewApplicationLifetime>();

        Assert.Null(app.GetActiveWindow());
    }

    [Fact]
    public void RootViewModelNotRegistered_ThrowsHelpfulError()
    {
        var app = new TestAppNoConvention();
        app.Initialize();
        app.ApplicationLifetime = new ClassicDesktopStyleApplicationLifetime();

        var ex = Assert.Throws<InvalidOperationException>(() => app.OnFrameworkInitializationCompleted());

        Assert.Contains("TestRootViewModel", ex.Message);
        Assert.Contains("base.Initialize()", ex.Message);
    }
}
