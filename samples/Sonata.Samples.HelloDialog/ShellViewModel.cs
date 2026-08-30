using Sonata.Avalonia;

namespace Sonata.Samples.HelloDialog;

public class ShellViewModel : Screen
{
    private readonly IWindowManager _windowManager;
    private readonly Func<Dialog1ViewModel> _dialogFactory;

    private string _nameString = "Click the button to show the dialog";
    public string NameString
    {
        get => _nameString;
        set => SetAndNotify(ref _nameString, value);
    }

    public ShellViewModel(IWindowManager windowManager, Func<Dialog1ViewModel> dialogFactory)
    {
        DisplayName = "Hello Dialog";
        _windowManager = windowManager;
        _dialogFactory = dialogFactory;
    }

    public async Task ShowDialog()
    {
        var dialogVm = _dialogFactory();
        var result = await _windowManager.ShowDialog<bool>(dialogVm);
        NameString = result
            ? string.Format("Your name is {0}", dialogVm.Name)
            : "Dialog cancelled";
    }
}
