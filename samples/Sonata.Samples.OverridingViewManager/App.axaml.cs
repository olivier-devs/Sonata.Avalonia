using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using Sonata.Avalonia;

namespace Sonata.Samples.OverridingViewManager;

public partial class App : SonataApplication<ShellViewModel>
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
        base.Initialize();
    }

    protected override void ConfigureServices(IServiceCollection services)
    {
        // Replace the default IViewManager with our attribute-based one.
        // Note: the default is registered with TryAdd, so this registration wins.
        services.AddSingleton<IViewManager, CustomViewManager>();
    }
}
