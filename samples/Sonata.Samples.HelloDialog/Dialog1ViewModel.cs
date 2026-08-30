using Sonata.Avalonia;

namespace Sonata.Samples.HelloDialog;

public class Dialog1ViewModel : Screen
{
    public string? Name { get; set; }

    public Dialog1ViewModel()
    {
        DisplayName = "I'm Dialog 1";
    }

    public void Close() => RequestClose(null);
    public void Save() => RequestClose(true);
}
