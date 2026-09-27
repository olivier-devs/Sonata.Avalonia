using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Markup.Xaml;
using Sonata.Avalonia.Xaml;
using Xunit;

namespace Sonata.Avalonia.Headless.Tests;

public class ActionArgumentsTokenizerSpikeTests
{
    [AvaloniaFact]
    public void Tokenizer_SemicolonSeparator_Loads()
    {
        const string xaml = """
            <Button xmlns="https://github.com/avaloniaui"
                    xmlns:s="clr-namespace:Sonata.Avalonia.Xaml;assembly=Sonata.Avalonia"
                    Command="{s:Action Save(42;43)}" />
            """;

        var button = (Button)AvaloniaRuntimeXamlLoader.Load(xaml);
        var action = Assert.IsType<CommandAction>(button.Command);

        Assert.Equal("Save", action.MethodName);
    }

    [AvaloniaFact]
    public void Tokenizer_EscapedQuote_Loads()
    {
        const string xaml = """
            <Button xmlns="https://github.com/avaloniaui"
                    xmlns:s="clr-namespace:Sonata.Avalonia.Xaml;assembly=Sonata.Avalonia"
                    Command="{s:Action Save(\'Alice Smith\';42)}" />
            """;

        var button = (Button)AvaloniaRuntimeXamlLoader.Load(xaml);
        var action = Assert.IsType<CommandAction>(button.Command);

        Assert.Equal("Save", action.MethodName);
    }

    [AvaloniaFact]
    public void Tokenizer_EscapedComma_Loads()
    {
        const string xaml = """
            <Button xmlns="https://github.com/avaloniaui"
                    xmlns:s="clr-namespace:Sonata.Avalonia.Xaml;assembly=Sonata.Avalonia"
                    Command="{s:Action Save(42\,43)}" />
            """;

        var button = (Button)AvaloniaRuntimeXamlLoader.Load(xaml);
        var action = Assert.IsType<CommandAction>(button.Command);

        Assert.Equal("Save", action.MethodName);
    }

    [AvaloniaFact]
    public void Tokenizer_CompiledXaml_SemicolonSeparator_Loads()
    {
        // The compiled path runs the SAME MeScanner through XamlIl at build time: if this view
        // compiles, the compiled tokenizer accepts ';'. Instantiating it runs InitializeComponent.
        var view = new ActionArgumentsCompiledView();

        var button = view.FindControl<Button>("MultiArgButton");
        var action = Assert.IsType<CommandAction>(button!.Command);

        Assert.Equal("Add", action.MethodName);
    }
}
