using Sonata.Avalonia;

namespace Sonata.Samples.TabNavigation;

public class ShellViewModel : Conductor<IScreen>.Collection.OneActive
{
    public ShellViewModel(Page1ViewModel page1, Page2ViewModel page2)
    {
        Items.Add(page1);
        Items.Add(page2);
        ActiveItem = page1;
    }
}
