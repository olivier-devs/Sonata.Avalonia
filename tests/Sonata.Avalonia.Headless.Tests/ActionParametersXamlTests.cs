using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Sonata.Avalonia.Xaml;
using Xunit;

namespace Sonata.Avalonia.Headless.Tests;

public class ActionParametersXamlTests
{
    [AvaloniaFact]
    public void ActionParameters_AttachedProperty_RoundTrips()
    {
        var button = new Button();
        var collection = new ActionParameterCollection { new Parameter { Value = 1 } };

        Sonata.Avalonia.Xaml.Action.SetParameters(button, collection);

        Assert.Same(collection, Sonata.Avalonia.Xaml.Action.GetParameters(button));
    }

    [AvaloniaFact]
    public void ActionParameters_LoadsFromXaml_AsCollection()
    {
        const string xaml = """
            <Button xmlns="https://github.com/avaloniaui"
                    xmlns:s="clr-namespace:Sonata.Avalonia.Xaml;assembly=Sonata.Avalonia">
                <s:Action.Parameters>
                    <s:Parameter Value="{Binding Name}" />
                    <s:DataContextParameter />
                </s:Action.Parameters>
            </Button>
            """;

        var button = (Button)global::Avalonia.Markup.Xaml.AvaloniaRuntimeXamlLoader.Load(xaml);

        var parameters = Sonata.Avalonia.Xaml.Action.GetParameters(button);
        Assert.NotNull(parameters);
        Assert.Equal(2, parameters.Count);
        Assert.IsType<Parameter>(parameters[0]);
        Assert.IsType<DataContextParameter>(parameters[1]);
    }

    [AvaloniaFact]
    public void ActionParameters_GetOnFreshControl_CreatesEmptyCollection()
    {
        var button = new Button();

        var collection = Sonata.Avalonia.Xaml.Action.GetParameters(button);

        Assert.NotNull(collection);
        Assert.Empty(collection);
        Assert.Same(collection, Sonata.Avalonia.Xaml.Action.GetParameters(button));
    }

    [AvaloniaFact]
    public void Action_XmlEndToEnd_CompactSyntax_InvokesMethod()
    {
        const string xaml = """
            <Button xmlns="https://github.com/avaloniaui"
                    xmlns:s="clr-namespace:Sonata.Avalonia.Xaml;assembly=Sonata.Avalonia"
                    Command="{s:Action Load(42)}" />
            """;
        var button = (Button)global::Avalonia.Markup.Xaml.AvaloniaRuntimeXamlLoader.Load(xaml);
        var vm = new ShellViewModelWithParameters();
        View.SetActionTarget(button, vm);

        button.Command!.Execute(null);

        Assert.Equal(42, vm.LastId);
    }

    [AvaloniaFact]
    public void Action_XmlEndToEnd_AttachedParameters_InvokesMethod()
    {
        const string xaml = """
            <Button xmlns="https://github.com/avaloniaui"
                    xmlns:s="clr-namespace:Sonata.Avalonia.Xaml;assembly=Sonata.Avalonia"
                    Command="{s:Action Load}">
                <s:Action.Parameters>
                    <s:Parameter Value="7" />
                </s:Action.Parameters>
            </Button>
            """;
        var button = (Button)global::Avalonia.Markup.Xaml.AvaloniaRuntimeXamlLoader.Load(xaml);
        var vm = new ShellViewModelWithParameters();
        View.SetActionTarget(button, vm);

        button.Command!.Execute(null);

        Assert.Equal(7, vm.LastId);
    }

    [AvaloniaFact]
    public void Action_MixedInlineAndAttachedParameters_ThrowsAtExecute()
    {
        const string xaml = """
            <Button xmlns="https://github.com/avaloniaui"
                    xmlns:s="clr-namespace:Sonata.Avalonia.Xaml;assembly=Sonata.Avalonia"
                    Command="{s:Action Load(42)}">
                <s:Action.Parameters>
                    <s:Parameter Value="7" />
                </s:Action.Parameters>
            </Button>
            """;
        var button = (Button)global::Avalonia.Markup.Xaml.AvaloniaRuntimeXamlLoader.Load(xaml);
        var vm = new ShellViewModelWithParameters();
        View.SetActionTarget(button, vm);

        Assert.Throws<InvalidOperationException>(() => button.Command!.Execute(null));
    }

    [AvaloniaFact]
    public void Action_XmlEndToEnd_CompactSyntax_QuotedString_ThrowsAtLoad()
    {
        // Platform limitation (XamlX markup extension tokenizer): single quotes inside a markup
        // extension argument are rejected as 'Quote characters out of place'. Quoted string
        // literals are therefore unavailable in compact XAML syntax — use the declarative
        // <s:Action.Parameters> syntax instead.
        const string xaml = """
            <Button xmlns="https://github.com/avaloniaui"
                    xmlns:s="clr-namespace:Sonata.Avalonia.Xaml;assembly=Sonata.Avalonia"
                    Command="{s:Action Save('Draft')}" />
            """;

        var ex = Assert.ThrowsAny<Exception>(() => global::Avalonia.Markup.Xaml.AvaloniaRuntimeXamlLoader.Load(xaml));
        Assert.Contains("Quote characters out of place", ex.Message);
    }

    [AvaloniaFact]
    public void Action_XmlEndToEnd_CompactSyntax_MultipleArguments_ThrowsAtLoad()
    {
        // Platform limitation (XamlX markup extension tokenizer): nested quotes/commas inside a
        // markup extension argument are not supported. Multi-argument compact syntax is therefore
        // unavailable in XAML — use the declarative <s:Action.Parameters> syntax instead.
        // This test pins the limitation: if Avalonia ever lifts it, this test will fail and
        // surface the change.
        const string xaml = """
            <Button xmlns="https://github.com/avaloniaui"
                    xmlns:s="clr-namespace:Sonata.Avalonia.Xaml;assembly=Sonata.Avalonia"
                    Command="{s:Action Save('Alice', 42)}" />
            """;

        var ex = Assert.ThrowsAny<Exception>(() => global::Avalonia.Markup.Xaml.AvaloniaRuntimeXamlLoader.Load(xaml));
        Assert.Contains("Quote characters out of place", ex.Message);
    }

    [AvaloniaFact]
    public void Action_XmlEndToEnd_EventAction_ActionTargetSetAfterLoad_InvokesMethodOnEvent()
    {
        // The ActionBase constructor resolves the target through GetValue + GetObservable on both
        // the subject and the backup root object. This e2e pins that path: the XAML is loaded
        // first (no ActionTarget anywhere), and the target is only set on the root window
        // afterwards — the inherited-property change must reach the button's EventAction.
        const string xaml = """
            <Window xmlns="https://github.com/avaloniaui"
                    xmlns:s="clr-namespace:Sonata.Avalonia.Xaml;assembly=Sonata.Avalonia">
                <Button Click="{s:Action RecordClick}" />
            </Window>
            """;
        var window = (Window)global::Avalonia.Markup.Xaml.AvaloniaRuntimeXamlLoader.Load(xaml);
        var vm = new ShellViewModelWithParameters();
        var button = (Button)window.Content!;

        // Act — the ActionTarget is set AFTER the load
        View.SetActionTarget(window, vm);

        button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));

        Assert.Equal(1, vm.ClickCount);
    }

    [AvaloniaFact]
    public void Action_XmlEndToEnd_DataTemplate_DataContextParameter_InvokesParentDelete()
    {
        // Spec §2 flagship scenario: ItemsControl + DataTemplate, ActionTarget = parent ViewModel
        // (inherited from the window), DataContext of each templated button = the current item.
        // ParentViewModel.Delete(item) must be invoked with the button's DataContext.
        // The headless test app loads no theme, so the ItemsControl template (which the theme
        // normally provides) is declared inline; showing the window runs the layout pass that
        // materializes the containers and instantiates the DataTemplate.
        const string xaml = """
            <Window xmlns="https://github.com/avaloniaui"
                    xmlns:s="clr-namespace:Sonata.Avalonia.Xaml;assembly=Sonata.Avalonia">
                <ItemsControl ItemsSource="{Binding Items}">
                    <ItemsControl.Template>
                        <ControlTemplate>
                            <ItemsPresenter />
                        </ControlTemplate>
                    </ItemsControl.Template>
                    <ItemsControl.ItemTemplate>
                        <DataTemplate>
                            <Button Command="{s:Action Delete}">
                                <s:Action.Parameters>
                                    <s:DataContextParameter />
                                </s:Action.Parameters>
                            </Button>
                        </DataTemplate>
                    </ItemsControl.ItemTemplate>
                </ItemsControl>
            </Window>
            """;
        var window = (Window)global::Avalonia.Markup.Xaml.AvaloniaRuntimeXamlLoader.Load(xaml);
        var parentVm = new ParameterizedParentViewModel();
        window.DataContext = parentVm;

        // Act — realize the items, then set the ActionTarget AFTER the template buttons exist
        window.Show();
        Dispatcher.UIThread.RunJobs();
        View.SetActionTarget(window, parentVm);

        var buttons = window.GetVisualDescendants().OfType<Button>().ToList();
        Assert.Equal(3, buttons.Count);

        var button = buttons[1];
        var item = Assert.IsType<Widget>(button.DataContext);

        button.Command!.Execute(null);

        Assert.Same(item, parentVm.DeletedItem);
    }

    [AvaloniaFact]
    public void Action_XmlEndToEnd_SemicolonMultiArgs_InvokesMethod()
    {
        const string xaml = """
            <Button xmlns="https://github.com/avaloniaui"
                    xmlns:s="clr-namespace:Sonata.Avalonia.Xaml;assembly=Sonata.Avalonia"
                    Command="{s:Action Add(40;2)}" />
            """;
        var button = (Button)global::Avalonia.Markup.Xaml.AvaloniaRuntimeXamlLoader.Load(xaml);
        var vm = new ShellViewModelWithParameters();
        View.SetActionTarget(button, vm);

        button.Command!.Execute(null);

        Assert.Equal(42, vm.LastSum);
    }

    [AvaloniaFact]
    public void Action_XmlEndToEnd_EscapedQuoteAndSemicolon_InvokesMethod()
    {
        // \' is cleaned by the XamlX tokenizer to ', so ParseMethod sees Greet('Alice';3).
        const string xaml = """
            <Button xmlns="https://github.com/avaloniaui"
                    xmlns:s="clr-namespace:Sonata.Avalonia.Xaml;assembly=Sonata.Avalonia"
                    Command="{s:Action Greet(\'Alice\';3)}" />
            """;
        var button = (Button)global::Avalonia.Markup.Xaml.AvaloniaRuntimeXamlLoader.Load(xaml);
        var vm = new ShellViewModelWithParameters();
        View.SetActionTarget(button, vm);

        button.Command!.Execute(null);

        Assert.Equal("Alicex3", vm.LastGreeting);
    }

    [AvaloniaFact]
    public void Action_CompiledXaml_SemicolonMultiArgs_InvokesMethod()
    {
        // Compiled path (XamlIl at build time): the compiled view from the Task-1 spike,
        // executed end-to-end — ActionTarget set on the root, button command executed.
        var view = new ActionArgumentsCompiledView();
        var vm = new ShellViewModelWithParameters();
        View.SetActionTarget(view, vm);

        var button = view.FindControl<Button>("MultiArgButton");
        Assert.NotNull(button);
        button.Command!.Execute(null);

        Assert.Equal(42, vm.LastSum);
    }

    [AvaloniaFact]
    public void Action_XmlEndToEnd_EscapedComma_InvokesMethod()
    {
        // \, is cleaned by the XamlX tokenizer to ',' — the parser then splits on it.
        const string xaml = """
            <Button xmlns="https://github.com/avaloniaui"
                    xmlns:s="clr-namespace:Sonata.Avalonia.Xaml;assembly=Sonata.Avalonia"
                    Command="{s:Action Add(40\,2)}" />
            """;
        var button = (Button)global::Avalonia.Markup.Xaml.AvaloniaRuntimeXamlLoader.Load(xaml);
        var vm = new ShellViewModelWithParameters();
        View.SetActionTarget(button, vm);

        button.Command!.Execute(null);

        Assert.Equal(42, vm.LastSum);
    }

    [AvaloniaFact]
    public void Action_XmlEndToEnd_IntThenStringArgs_InvokesMethod()
    {
        // Spec §5.1 grammar row 5: Save(42;\'Alice Smith\') → Save(42, "Alice Smith").
        const string xaml = """
            <Button xmlns="https://github.com/avaloniaui"
                    xmlns:s="clr-namespace:Sonata.Avalonia.Xaml;assembly=Sonata.Avalonia"
                    Command="{s:Action Record(42;\'Alice Smith\')}" />
            """;
        var button = (Button)global::Avalonia.Markup.Xaml.AvaloniaRuntimeXamlLoader.Load(xaml);
        var vm = new ShellViewModelWithParameters();
        View.SetActionTarget(button, vm);

        button.Command!.Execute(null);

        Assert.Equal("42:Alice Smith", vm.LastRecord);
    }

    [AvaloniaFact]
    public void Action_XmlEndToEnd_EmptyToken_ThrowsAtLoad()
    {
        // Pins the error-matrix row "empty token → parse error at XAML load".
        const string xaml = """
            <Button xmlns="https://github.com/avaloniaui"
                    xmlns:s="clr-namespace:Sonata.Avalonia.Xaml;assembly=Sonata.Avalonia"
                    Command="{s:Action Save(42;)}" />
            """;

        var ex = Assert.ThrowsAny<Exception>(() => global::Avalonia.Markup.Xaml.AvaloniaRuntimeXamlLoader.Load(xaml));
        // The parse error may surface directly or wrapped by the loader — walk the message chain only.
        var message = ex.Message;
        for (var inner = ex.InnerException; inner != null; inner = inner.InnerException)
            message += " " + inner.Message;
        Assert.Contains("empty argument token", message);
    }
}
