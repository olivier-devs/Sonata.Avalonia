using Avalonia.Controls;
using Microsoft.Extensions.Logging.Abstractions;
using Sonata.Avalonia;
using Sonata.Avalonia.Primitive;

namespace Sonata.Avalonia.Headless.Tests
{
    /// <summary>
    /// ViewModel/View pairs defined in this assembly, used to exercise the ViewManager's
    /// location conventions (the ViewManager scans the assemblies it is configured with).
    /// </summary>
    public class ShellViewModel : Screen { }

    /// <summary>
    /// Window whose title is cleared: Avalonia's Window.Title defaults to "Window", and
    /// WindowManager only binds Title to DisplayName for windows with an empty (or
    /// convention-default) title, so clear it to exercise that binding deterministically.
    /// </summary>
    public class ShellView : Window
    {
        public ShellView() => Title = string.Empty;
    }

    public class DialogViewModel : Screen { }

    public class DialogView : Window { }

    public class WidgetViewModel : Screen { }

    public class WidgetView : UserControl { }

    /// <summary>ViewModel using a custom 'Vm' suffix, located as 'SuffixPage'.</summary>
    public class SuffixVm { }

    /// <summary>View located for <see cref="SuffixVm"/> when ViewNameSuffix is 'Page'.</summary>
    public class SuffixPage : UserControl { }

    namespace VmLand
    {
        public class NestedViewModel { }
    }

    namespace ViewLand
    {
        public class NestedView : UserControl { }
    }

    /// <summary>
    /// <see cref="IWindowManagerConfig"/> fake returning a chosen active window (or null).
    /// </summary>
    internal sealed class FakeWindowManagerConfig : IWindowManagerConfig
    {
        public TopLevel? ActiveWindow { get; set; }

        public TopLevel? GetActiveWindow() => ActiveWindow;
    }

    /// <summary>Builders for the framework pieces under test, wired to this assembly's conventions.</summary>
    internal static class TestHost
    {
        public static ViewManager CreateViewManager(Action<ViewManager>? configure = null)
        {
            var manager = new ViewManager(
                new ViewManagerConfig
                {
                    ViewFactory = type => Activator.CreateInstance(type)!,
                    ViewAssemblies = [typeof(ShellViewModel).Assembly],
                },
                NullLogger<ViewManager>.Instance);
            configure?.Invoke(manager);
            return manager;
        }

        public static WindowManager CreateWindowManager(
            IWindowManagerConfig config,
            Func<IMessageBoxViewModel> messageBoxViewModelFactory) =>
            new(CreateViewManager(), config, messageBoxViewModelFactory, NullLogger<WindowManager>.Instance);
    }
}
