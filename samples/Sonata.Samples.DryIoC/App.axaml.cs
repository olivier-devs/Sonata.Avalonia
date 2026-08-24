using Avalonia.Markup.Xaml;

namespace Sonata.Samples.DryIoC;

public partial class App : DryIocSonataApplication<MainViewModel>
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
        base.Initialize();
    }
}
