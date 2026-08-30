using Sonata.Avalonia;

namespace Sonata.Samples.NavigationController.Pages;

public class Page2ViewModel : Screen
{
    private readonly INavigationController _navigationController;

    private string _initiator = string.Empty;
    public string Initiator
    {
        get => _initiator;
        set => SetAndNotify(ref _initiator, value);
    }

    public Page2ViewModel(INavigationController navigationController)
    {
        _navigationController = navigationController ?? throw new ArgumentNullException(nameof(navigationController));
    }

    public void NavigateToPage1() => _navigationController.NavigateToPage1();
}
