using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Headless.XUnit;
using Sonata.Avalonia;
using Xunit;

namespace Sonata.Avalonia.Headless.Tests;

public class BootstrapperHeadlessTests
{
    private class TestRootViewModel : Screen
    {
    }

    private class TestRootView : Window
    {
    }

    private class TestApp : SonataApplication<TestRootViewModel>
    {
    }

    [AvaloniaFact]
    public void OnFrameworkInitializationCompleted_ActivatesRootViewModel()
    {
        var app = new TestApp();
        app.Initialize();

        var lifetime = new ClassicDesktopStyleApplicationLifetime();
        app.ApplicationLifetime = lifetime;

        app.OnFrameworkInitializationCompleted();

        Assert.NotNull(lifetime.MainWindow);
        Assert.IsType<TestRootView>(lifetime.MainWindow);

        var rootViewModel = IoC.Get<TestRootViewModel>();
        Assert.True(rootViewModel.IsActive);
    }
}
