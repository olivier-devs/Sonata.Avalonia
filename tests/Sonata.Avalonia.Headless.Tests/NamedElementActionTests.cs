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
}
