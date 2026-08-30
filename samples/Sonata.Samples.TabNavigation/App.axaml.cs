using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using Sonata.Avalonia;

namespace Sonata.Samples.TabNavigation;

public partial class App : SonataApplication<ShellViewModel>
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
        base.Initialize();
    }

    protected override void ConfigureServices(IServiceCollection services)
    {
        // ShellViewModel, Page1ViewModel and Page2ViewModel are registered by convention.
    }
}
