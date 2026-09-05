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
    public void Action_XmlEndToEnd_CompactSyntax_MultipleArguments_InvokesMethod()
    {
        // Validates that Avalonia's markup extension parser keeps 'Save('Alice', 42)' as ONE
        // constructor argument (nested parens/quotes) — critical for the compact syntax viability.
        const string xaml = """
            <Button xmlns="https://github.com/avaloniaui"
                    xmlns:s="clr-namespace:Sonata.Avalonia.Xaml;assembly=Sonata.Avalonia"
                    Command="{s:Action Save('Alice', 42)}" />
            """;
        var button = (Button)global::Avalonia.Markup.Xaml.AvaloniaRuntimeXamlLoader.Load(xaml);
        var vm = new ShellViewModelWithParameters();
        View.SetActionTarget(button, vm);

        button.Command!.Execute(null);

        Assert.Equal("Alice:42", vm.LastSave);
    }
}
