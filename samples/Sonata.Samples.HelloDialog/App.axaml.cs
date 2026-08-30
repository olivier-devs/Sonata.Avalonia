using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using Sonata.Avalonia;

namespace Sonata.Samples.HelloDialog;

public partial class App : SonataApplication<ShellViewModel>
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
        base.Initialize(); // required — builds the service provider
    }

    protected override void ConfigureServices(IServiceCollection services)
    {
        services.AddTransient<Dialog1ViewModel>();
        services.AddTransient<Func<Dialog1ViewModel>>(sp => () => sp.GetRequiredService<Dialog1ViewModel>());
    }
}
