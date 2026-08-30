using Avalonia.Controls;

namespace Sonata.Samples.OverridingViewManager;

[ViewModel(typeof(ShellViewModel))]
public partial class ShellView : Window
{
    public ShellView() => InitializeComponent();
}
