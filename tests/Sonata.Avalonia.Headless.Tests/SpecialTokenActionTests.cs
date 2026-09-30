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

        // Avalonia's event routing invokes handlers via DynamicInvoke, which wraps the
        // resolution error in TargetInvocationException — assert the inner error robustly
        // (works whether or not a wrapper is present).
        var ex = Assert.ThrowsAny<Exception>(() => button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent)));
        var invalidOp = ex is TargetInvocationException tie ? tie.InnerException : ex;
        var invalidOperationException = Assert.IsType<InvalidOperationException>(invalidOp);
        Assert.Contains("Named element 'Missing'", invalidOperationException.Message);
    }

    [AvaloniaFact]
    public void DeclarativeSourceAndViewParameters_LoadFromXaml()
    {
        // Spec §11 Wave 3: "les 3 éléments déclaratifs chargent depuis XAML" — SourceParameter
        // and ViewParameter complete the set (EventArgsParameter is pinned above). No
        // ActionTarget needed: the load assertion only reads the attached collection.
        const string xaml = """
            <Window xmlns="https://github.com/avaloniaui"
                    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
                    xmlns:s="clr-namespace:Sonata.Avalonia.Xaml;assembly=Sonata.Avalonia">
                <StackPanel>
                    <Button x:Name="SourceButton" Command="{s:Action OnSource}">
                        <s:Action.Parameters>
                            <s:SourceParameter />
                        </s:Action.Parameters>
                    </Button>
                    <Button x:Name="ViewButton" Command="{s:Action OnView}">
                        <s:Action.Parameters>
                            <s:ViewParameter />
                        </s:Action.Parameters>
                    </Button>
                </StackPanel>
            </Window>
            """;
        var window = (Window)AvaloniaRuntimeXamlLoader.Load(xaml);

        var sourceButton = window.FindControl<Button>("SourceButton")!;
        var viewButton = window.FindControl<Button>("ViewButton")!;

        Assert.IsType<SourceParameter>(Sonata.Avalonia.Xaml.Action.GetParameters(sourceButton)[0]);
        Assert.IsType<ViewParameter>(Sonata.Avalonia.Xaml.Action.GetParameters(viewButton)[0]);
    }
}
