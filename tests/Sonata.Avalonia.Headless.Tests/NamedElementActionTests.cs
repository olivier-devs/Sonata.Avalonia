using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using Sonata.Avalonia.Xaml;
using Xunit;

namespace Sonata.Avalonia.Headless.Tests;

public class NamedElementActionTests
{
    [AvaloniaFact]
    public void NamedElement_GuardReevaluatesWhenElementPropertyChanges()
    {
        const string xaml = """
            <Window xmlns="https://github.com/avaloniaui"
                    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
                    xmlns:s="clr-namespace:Sonata.Avalonia.Xaml;assembly=Sonata.Avalonia">
                <StackPanel>
                    <TextBox x:Name="NameTextBox" />
                    <Button x:Name="SaveButton" Command="{s:Action Save(NameTextBox.Text)}" />
                </StackPanel>
            </Window>
            """;
        var window = (Window)AvaloniaRuntimeXamlLoader.Load(xaml);
        var vm = new NamedElementViewModel();
        View.SetActionTarget(window, vm);

        window.Show();
        Dispatcher.UIThread.RunJobs();

        var button = window.FindControl<Button>("SaveButton")!;
        var textBox = window.FindControl<TextBox>("NameTextBox")!;
        var command = Assert.IsType<CommandAction>(button.Command);

        // Empty text → guard false.
        Assert.False(command.CanExecute(null));

        // The flagship: changing the element's property must fire CanExecuteChanged
        // (Text change → GetChanges → UpdateCanExecute → CanExecuteChanged) — with no
        // manual CanExecute call in between, only the observation chain can fire it.
        var canExecuteChangedFired = false;
        command.CanExecuteChanged += (_, _) => canExecuteChangedFired = true;

        textBox.Text = "hello";
        Dispatcher.UIThread.RunJobs();

        Assert.True(canExecuteChangedFired);
        Assert.True(command.CanExecute(null));

        command.Execute(null);
        Assert.Equal("hello", vm.LastSaved);
    }

    [AvaloniaFact]
    public void NamedElement_MissingElement_SoftAtCanExecute_HardAtExecute()
    {
        // "Save(Missing.Text)" — the element is never registered in the name scope.
        const string xaml = """
            <Window xmlns="https://github.com/avaloniaui"
                    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
                    xmlns:s="clr-namespace:Sonata.Avalonia.Xaml;assembly=Sonata.Avalonia">
                <Button x:Name="SaveButton" Command="{s:Action Save(Missing.Text)}" />
            </Window>
            """;
        var window = (Window)AvaloniaRuntimeXamlLoader.Load(xaml);
        var vm = new NamedElementViewModel();
        View.SetActionTarget(window, vm);

        window.Show();
        Dispatcher.UIThread.RunJobs();

        var button = window.FindControl<Button>("SaveButton")!;
        var command = Assert.IsType<CommandAction>(button.Command);

        // Soft: CanExecute yields false (guard sees null) and does NOT throw.
        Assert.False(command.CanExecute(null));

        // Hard: Execute throws a deterministic InvalidOperationException.
        Assert.Throws<InvalidOperationException>(() => command.Execute(null));
    }

    [AvaloniaFact]
    public void NamedElement_PropertyNotFound_ThrowsAtResolution()
    {
        // Element found, property name wrong → deterministic error. It surfaces at target-set:
        // OnTargetChanged → UpdateCanExecute → CanExecuteChanged → the Button re-queries
        // CanExecute → resolution throws. Fail-fast at startup — the element exists, so there
        // is no timing excuse; the soft/hard asymmetry applies only to a MISSING element.
        const string xaml = """
            <Window xmlns="https://github.com/avaloniaui"
                    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
                    xmlns:s="clr-namespace:Sonata.Avalonia.Xaml;assembly=Sonata.Avalonia">
                <StackPanel>
                    <TextBox x:Name="NameTextBox" />
                    <Button x:Name="SaveButton" Command="{s:Action Save(NameTextBox.NonExistent)}" />
                </StackPanel>
            </Window>
            """;
        var window = (Window)AvaloniaRuntimeXamlLoader.Load(xaml);
        var vm = new NamedElementViewModel();
        var button = window.FindControl<Button>("SaveButton")!;
        var command = Assert.IsType<CommandAction>(button.Command);

        Assert.Throws<InvalidOperationException>(() => View.SetActionTarget(window, vm));

        // Every subsequent resolution throws identically.
        Assert.Throws<InvalidOperationException>(() => command.CanExecute(null));
        Assert.Throws<InvalidOperationException>(() => command.Execute(null));
    }

    [AvaloniaFact]
    public void NamedElement_DeclarativeBindingPattern_IsPinned()
    {
        // The canonical declarative equivalent of Save(NameTextBox.Text): a plain Parameter
        // bound via {Binding #NameTextBox.Text} (spec §6.4 — documentation-only, zero new
        // declarative type; this test pins the documented pattern end-to-end).
        const string xaml = """
            <Window xmlns="https://github.com/avaloniaui"
                    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
                    xmlns:s="clr-namespace:Sonata.Avalonia.Xaml;assembly=Sonata.Avalonia">
                <StackPanel>
                    <TextBox x:Name="NameTextBox" Text="seed" />
                    <Button x:Name="SaveButton" Command="{s:Action Save}">
                        <s:Action.Parameters>
                            <s:Parameter Value="{Binding #NameTextBox.Text}" />
                        </s:Action.Parameters>
                    </Button>
                </StackPanel>
            </Window>
            """;
        var window = (Window)AvaloniaRuntimeXamlLoader.Load(xaml);
        var vm = new NamedElementViewModel();
        View.SetActionTarget(window, vm);

        window.Show();
        Dispatcher.UIThread.RunJobs();

        var button = window.FindControl<Button>("SaveButton")!;

        button.Command!.Execute(null);

        Assert.Equal("seed", vm.LastSaved);
    }
}
