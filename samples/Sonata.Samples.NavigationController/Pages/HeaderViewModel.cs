using Sonata.Avalonia;

namespace Sonata.Samples.NavigationController.Pages;

public class HeaderViewModel : Screen
{
    private readonly INavigationController _navigationController;

    public HeaderViewModel(INavigationController navigationController)
    {
        _navigationController = navigationController ?? throw new ArgumentNullException(nameof(navigationController));
    }

    public void NavigateToPage1() => _navigationController.NavigateToPage1();
    public void NavigateToPage2() => _navigationController.NavigateToPage2("the Header");
}
