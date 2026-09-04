using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using Sonata.Avalonia;
using Sonata.Samples.ConfigureViewManager.ViewModels;
using Sonata.Samples.ConfigureViewManager.Views;

namespace Sonata.Samples.ConfigureViewManager;

public partial class App : SonataApplication<MainViewModel>
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
        base.Initialize();
    }

    protected override void ConfigureServices(IServiceCollection services)
    {
        services.ConfigureViewManager(options =>
        {
            options
                .AddViewAssembly<App>()
                .AddViewAssembly<MainView>() // same assembly as App, demonstrates deduplication
                .MapNamespace("Sonata.Samples.ConfigureViewManager.ViewModels",
                              "Sonata.Samples.ConfigureViewManager.Views")
                .AddView<MainView, MainViewModel>()
                .AddView<CustomEditorControl, LegacyEditorViewModel>();
        });
    }
}
