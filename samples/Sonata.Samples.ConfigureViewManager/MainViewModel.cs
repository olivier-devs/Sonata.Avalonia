using Sonata.Samples.ConfigureViewManager.ViewModels;

namespace Sonata.Samples.ConfigureViewManager;

public class MainViewModel
{
    public AboutViewModel About { get; } = new();

    public LegacyEditorViewModel Editor { get; } = new();
}
