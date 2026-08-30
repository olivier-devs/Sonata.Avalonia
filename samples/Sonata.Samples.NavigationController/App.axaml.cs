using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using Sonata.Avalonia;
using Sonata.Samples.NavigationController.Pages;

namespace Sonata.Samples.NavigationController;

public partial class App : SonataApplication<ShellViewModel>
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
        base.Initialize();
    }

    protected override void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton<INavigationController, NavigationController>();
        services.AddSingleton<INavigationControllerDelegate>(sp => sp.GetRequiredService<ShellViewModel>());
        services.AddTransient<Func<Page1ViewModel>>(sp => () => sp.GetRequiredService<Page1ViewModel>());
        services.AddTransient<Func<Page2ViewModel>>(sp => () => sp.GetRequiredService<Page2ViewModel>());
        services.AddTransient<Func<INavigationControllerDelegate>>(sp => () => sp.GetRequiredService<INavigationControllerDelegate>());
        // ShellViewModel, HeaderViewModel, Page1ViewModel, Page2ViewModel are registered by convention.
    }
}
