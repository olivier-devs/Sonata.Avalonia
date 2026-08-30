using Sonata.Avalonia;

namespace Sonata.Samples.NavigationController.Pages;

public class Page1ViewModel : Screen
{
    private readonly INavigationController _navigationController;

    public Page1ViewModel(INavigationController navigationController)
    {
        _navigationController = navigationController ?? throw new ArgumentNullException(nameof(navigationController));
    }

    public void NavigateToPage2() => _navigationController.NavigateToPage2("Page 1");
}
