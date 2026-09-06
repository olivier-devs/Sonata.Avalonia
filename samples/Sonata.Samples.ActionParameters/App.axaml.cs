using Avalonia.Markup.Xaml;
using Sonata.Avalonia;

namespace Sonata.Samples.ActionParameters;

public partial class App : SonataApplication<ShellViewModel>
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
        base.Initialize(); // required — builds the service provider
    }
}
