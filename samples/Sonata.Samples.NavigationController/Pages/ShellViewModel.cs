using Sonata.Avalonia;

namespace Sonata.Samples.NavigationController.Pages;

public class ShellViewModel : Conductor<IScreen>, INavigationControllerDelegate
{
    public HeaderViewModel HeaderViewModel { get; }

    public ShellViewModel(HeaderViewModel headerViewModel)
    {
        HeaderViewModel = headerViewModel ?? throw new ArgumentNullException(nameof(headerViewModel));
    }

    public void NavigateTo(IScreen screen) => _ = ActivateItemAsync(screen);
}
