using Avalonia.Controls;
using Avalonia.Headless.XUnit;
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
}
