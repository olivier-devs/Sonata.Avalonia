using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using Sonata.Avalonia;

namespace Sonata.Samples.MSIoC;

public partial class App : SonataApplication<MainViewModel>
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
        base.Initialize(); // required — builds the service provider
    }

    protected override void ConfigureServices(IServiceCollection services)
    {
        // MainViewModel is registered automatically by convention.
        // Register additional services here, e.g.:
        // services.AddSingleton<IMyService, MyService>();
    }
}
