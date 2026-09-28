using Avalonia.Controls;
using Sonata.Avalonia.Xaml;
using System.Reactive.Linq;
using Xunit;

namespace Sonata.Avalonia.Tests;

public class ActionParameterTests
{
    [Fact]
    public void Parameter_GetValue_ReturnsValue()
    {
        var parameter = new Parameter { Value = 42 };
        var context = new ActionExecutionContext { Target = new object(), Source = null };

        var value = parameter.GetValue(context);

        Assert.Equal(42, value);
    }

    [Fact]
    public void DataContextParameter_GetValue_ReturnsContextDataContext()
    {
        var dataContext = new object();
        var context = new ActionExecutionContext { Target = new object(), Source = null, DataContext = dataContext };

        var value = new DataContextParameter().GetValue(context);

        Assert.Same(dataContext, value);
    }

    [Fact]
    public void Parameter_GetChanges_EmitsOnValueChange()
    {
        var parameter = new Parameter { Value = 1 };
        object? received = null;
        parameter.GetChanges().Subscribe(v => received = v);

        parameter.Value = 2;

        Assert.Equal(2, received);
    }

    [Fact]
    public void DataContextParameter_GetChanges_NeverEmits()
    {
        var parameter = new DataContextParameter();
        var emitted = false;
        parameter.GetChanges().Subscribe(_ => emitted = true);

        Assert.False(emitted);
    }

    /// <summary>Test parameter source that resolves the context's View — exercises the root capture.</summary>
    private sealed class ViewCapturingParameter : ActionParameter
    {
        public override object? GetValue(ActionExecutionContext context) => context.View;
    }

    private class ViewCaptureViewModel
    {
        public object? ReceivedView { get; private set; }

        public void Capture(object? view) => ReceivedView = view;
    }

    [Fact]
    public void CreateExecutionContext_PopulatesView_FromCapturedRoot()
    {
        // ActionExtension passes IRootObjectProvider.RootObject as the backupSubject — the
        // subject-based constructor must capture it and expose it as context.View.
        var subject = new Button();
        var root = new UserControl();
        var vm = new ViewCaptureViewModel();
        View.SetActionTarget(root, vm);

        var action = new CommandAction(subject, root, "Capture", ActionUnavailableBehaviour.Throw, ActionUnavailableBehaviour.Throw,
            new ActionParameter[] { new ViewCapturingParameter() });

        action.Execute(null);

        Assert.Same(root, vm.ReceivedView);
    }

    [Fact]
    public void CreateExecutionContext_ExplicitTarget_ViewIsNull()
    {
        // The explicit-target constructor has no root object — context.View stays null
        // (the $view token will throw on resolution in a later task; here it resolves to null).
        var vm = new ViewCaptureViewModel();

        var action = new CommandAction(vm, "Capture", ActionUnavailableBehaviour.Throw, ActionUnavailableBehaviour.Throw,
            new ActionParameter[] { new ViewCapturingParameter() });

        action.Execute(null);

        Assert.Null(vm.ReceivedView);
    }
}
