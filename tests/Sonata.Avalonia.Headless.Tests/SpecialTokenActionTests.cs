using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using Sonata.Avalonia.Xaml;
using System.Reflection;
using Xunit;

namespace Sonata.Avalonia.Headless.Tests;

public class SpecialTokenActionTests
{
    [AvaloniaFact]
    public void EventArgsToken_OnEventAction_PassesEventArguments()
    {
        const string xaml = """
            <Window xmlns="https://github.com/avaloniaui"
                    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
                    xmlns:s="clr-namespace:Sonata.Avalonia.Xaml;assembly=Sonata.Avalonia">
                <Button x:Name="B" Click="{s:Action OnArgs($eventArgs)}" />
            </Window>
            """;
        var window = (Window)AvaloniaRuntimeXamlLoader.Load(xaml);
        var vm = new SpecialTokenViewModel();
        View.SetActionTarget(window, vm);

        window.Show();
        Dispatcher.UIThread.RunJobs();

        var button = window.FindControl<Button>("B")!;
        var args = new RoutedEventArgs(Button.ClickEvent);
        button.RaiseEvent(args);

        Assert.Same(args, vm.ReceivedEventArgs);
    }

    [AvaloniaFact]
    public void EventArgsToken_OnCommandAction_Throws()
    {
        const string xaml = """
            <Window xmlns="https://github.com/avaloniaui"
                    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
                    xmlns:s="clr-namespace:Sonata.Avalonia.Xaml;assembly=Sonata.Avalonia">
                <Button x:Name="B" Command="{s:Action OnArgs($eventArgs)}" />
            </Window>
            """;
        var window = (Window)AvaloniaRuntimeXamlLoader.Load(xaml);
        var vm = new SpecialTokenViewModel();

        var ex = Assert.Throws<InvalidOperationException>(() => View.SetActionTarget(window, vm));
        Assert.Contains("$eventArgs", ex.Message);
    }

    [AvaloniaFact]
    public void SourceToken_PassesTriggeringControl()
    {
        const string xaml = """
            <Window xmlns="https://github.com/avaloniaui"
                    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
                    xmlns:s="clr-namespace:Sonata.Avalonia.Xaml;assembly=Sonata.Avalonia">
                <Button x:Name="B" Command="{s:Action OnSource($source)}" />
            </Window>
            """;
        var window = (Window)AvaloniaRuntimeXamlLoader.Load(xaml);
        var vm = new SpecialTokenViewModel();
        View.SetActionTarget(window, vm);

        window.Show();
        Dispatcher.UIThread.RunJobs();

        var button = window.FindControl<Button>("B")!;
        button.Command!.Execute(null);

        Assert.Same(button, vm.ReceivedSource);
    }

    [AvaloniaFact]
    public void ViewToken_PassesXamlRootObject()
    {
        const string xaml = """
            <Window xmlns="https://github.com/avaloniaui"
                    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
                    xmlns:s="clr-namespace:Sonata.Avalonia.Xaml;assembly=Sonata.Avalonia">
                <Button x:Name="B" Command="{s:Action OnView($view)}" />
            </Window>
            """;
        var window = (Window)AvaloniaRuntimeXamlLoader.Load(xaml);
        var vm = new SpecialTokenViewModel();
        View.SetActionTarget(window, vm);

        window.Show();
        Dispatcher.UIThread.RunJobs();

        var button = window.FindControl<Button>("B")!;
        button.Command!.Execute(null);

        Assert.Same(window, vm.ReceivedView);
    }

    [AvaloniaFact]
    public void DeclarativeSpecialParameters_LoadFromXaml()
    {
        const string xaml = """
            <Window xmlns="https://github.com/avaloniaui"
                    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
                    xmlns:s="clr-namespace:Sonata.Avalonia.Xaml;assembly=Sonata.Avalonia">
                <Button x:Name="B" Click="{s:Action OnArgs}">
                    <s:Action.Parameters>
                        <s:EventArgsParameter />
                    </s:Action.Parameters>
                </Button>
            </Window>
            """;
        var window = (Window)AvaloniaRuntimeXamlLoader.Load(xaml);
        var vm = new SpecialTokenViewModel();
        View.SetActionTarget(window, vm);

        window.Show();
        Dispatcher.UIThread.RunJobs();

        var button = window.FindControl<Button>("B")!;
        var parameters = Sonata.Avalonia.Xaml.Action.GetParameters(button);
        Assert.IsType<EventArgsParameter>(parameters[0]);
    }

    [AvaloniaFact]
    public void EventAction_NamedElementMissing_ThrowsAtInvoke()
    {
        // EventAction resolves arguments strictly at event-fire time: a missing named
        // element is a deterministic typo once the view has loaded.
        const string xaml = """
            <Window xmlns="https://github.com/avaloniaui"
                    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
                    xmlns:s="clr-namespace:Sonata.Avalonia.Xaml;assembly=Sonata.Avalonia">
                <Button x:Name="B" Click="{s:Action Save(Missing.Text)}" />
            </Window>
            """;
        var window = (Window)AvaloniaRuntimeXamlLoader.Load(xaml);
        var vm = new NamedElementViewModel();
        View.SetActionTarget(window, vm);

        window.Show();
        Dispatcher.UIThread.RunJobs();

        var button = window.FindControl<Button>("B")!;

        var ex = Assert.Throws<TargetInvocationException>(() => button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent)));
        Assert.IsType<InvalidOperationException>(ex.InnerException);
    }
}
