using Avalonia.Media;
using Sonata.Avalonia;
using Sonata.Avalonia.Primitive;

namespace Sonata.Samples.Hello;

public class MainViewModel : Screen
{
    private string _name = string.Empty;
    private readonly IWindowManager _windowManager;

    public string Name
    {
        get => _name;
        set
        {
            SetAndNotify(ref _name, value);
            NotifyOfPropertyChange(nameof(CanSayHello));
        }
    }

    public MainViewModel(IWindowManager windowManager)
    {
        DisplayName = "Hello, Sonata";
        _windowManager = windowManager;
    }

    public bool CanSayHello => !string.IsNullOrEmpty(Name);

    public async Task SayHello()
    {
        await _windowManager.ShowMessageBox<bool>(
            $"Hello, {Name}",
            "Message box",
            MessageBoxButton.OKCancel,
            icon: MessageBoxImage.Information,
            textAlignment: TextAlignment.Center);
    }
}
