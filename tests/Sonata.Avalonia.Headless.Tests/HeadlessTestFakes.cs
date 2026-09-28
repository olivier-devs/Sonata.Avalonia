using Avalonia.Controls;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
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

    public class ShellViewModelWithParameters : Screen
    {
        public int LastId { get; private set; }

        public string LastSave { get; private set; } = "";

        public int ClickCount { get; private set; }

        public int LastSum { get; private set; }

        public string LastGreeting { get; private set; } = "";

        public void Load(int id) => LastId = id;

        public void Save(string name, int age) => LastSave = $"{name}:{age}";

        public void RecordClick() => ClickCount++;

        public void Add(int a, int b) => LastSum = a + b;

        public void Greet(string name, int times) => LastGreeting = $"{name}x{times}";
    }

    /// <summary>Identifiable item rendered by the DataTemplate in the ActionParameters e2e tests.</summary>
    public record Widget(int Id);

    /// <summary>
    /// Parent ViewModel for the spec §2 flagship scenario: the ActionTarget points here while
    /// each templated button's DataContext is the current item.
    /// </summary>
    public class ParameterizedParentViewModel
    {
        public IReadOnlyList<Widget> Items { get; } = new[] { new Widget(1), new Widget(2), new Widget(3) };

        public Widget? DeletedItem { get; private set; }

        public void Delete(Widget widget) => DeletedItem = widget;
    }

    /// <summary>ViewModel for the named-element flagship test: parameterized guard on a TextBox.Text.</summary>
    public class NamedElementViewModel
    {
        public string LastSaved { get; private set; } = "";

        public bool CanSave(string name) => !string.IsNullOrWhiteSpace(name);

        public void Save(string name) => LastSaved = name;
    }

    /// <summary>ViewModel capturing the raw values passed to special-token actions.</summary>
    public class SpecialTokenViewModel
    {
        public object? ReceivedEventArgs { get; private set; }
        public object? ReceivedSource { get; private set; }
        public object? ReceivedView { get; private set; }

        public void OnArgs(EventArgs args) => ReceivedEventArgs = args;

        public void OnSource(Control source) => ReceivedSource = source;

        public void OnView(object view) => ReceivedView = view;
    }

    /// <summary>Builders for the framework pieces under test, wired to this assembly's conventions.</summary>
    internal static class TestHost
    {
        public static ViewManager CreateViewManager(System.Action<ViewManagerConfig>? configure = null)
        {
            var config = new ViewManagerConfig()
                .SetViewFactory(type => Activator.CreateInstance(type)!)
                .AddViewAssembly(typeof(ShellViewModel).Assembly);
            configure?.Invoke(config);
            return new ViewManager(Options.Create(config), NullLogger<ViewManager>.Instance);
        }

        public static WindowManager CreateWindowManager(
            IWindowManagerConfig config,
            Func<IMessageBoxViewModel> messageBoxViewModelFactory) =>
            new(CreateViewManager(), config, messageBoxViewModelFactory, NullLogger<WindowManager>.Instance);
    }
}
