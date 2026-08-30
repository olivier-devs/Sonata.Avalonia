using Sonata.Avalonia;
using Sonata.Samples.NavigationController.Pages;

namespace Sonata.Samples.NavigationController;

public interface INavigationController
{
    void NavigateToPage1();
    void NavigateToPage2(string initiator);
}

public interface INavigationControllerDelegate
{
    void NavigateTo(IScreen screen);
}

public class NavigationController : INavigationController
{
    private readonly Func<Page1ViewModel> _page1ViewModelFactory;
    private readonly Func<Page2ViewModel> _page2ViewModelFactory;
    private readonly Func<INavigationControllerDelegate> _delegateFactory;

    public NavigationController(
        Func<Page1ViewModel> page1ViewModelFactory,
        Func<Page2ViewModel> page2ViewModelFactory,
        Func<INavigationControllerDelegate> delegateFactory)
    {
        _page1ViewModelFactory = page1ViewModelFactory ?? throw new ArgumentNullException(nameof(page1ViewModelFactory));
        _page2ViewModelFactory = page2ViewModelFactory ?? throw new ArgumentNullException(nameof(page2ViewModelFactory));
        _delegateFactory = delegateFactory ?? throw new ArgumentNullException(nameof(delegateFactory));
    }

    public void NavigateToPage1() => _delegateFactory().NavigateTo(_page1ViewModelFactory());

    public void NavigateToPage2(string initiator)
    {
        var vm = _page2ViewModelFactory();
        vm.Initiator = initiator;
        _delegateFactory().NavigateTo(vm);
    }
}
