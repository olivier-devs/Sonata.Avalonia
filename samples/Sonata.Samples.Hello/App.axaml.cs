using Avalonia.Markup.Xaml;
using Sonata.Avalonia.StyletIoC;

namespace Sonata.Samples.Hello;

public partial class App : StyletApplication<MainViewModel>
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
        base.Initialize(); // required — builds the StyletIoC container
    }
}
