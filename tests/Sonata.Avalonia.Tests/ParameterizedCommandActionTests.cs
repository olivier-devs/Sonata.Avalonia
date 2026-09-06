using Avalonia.Controls;
using Sonata.Avalonia.Xaml;
using Xunit;
using Action = Sonata.Avalonia.Xaml.Action;

namespace Sonata.Avalonia.Tests;

public class ParameterizedCommandActionTests
{
    public class Target
    {
        public int LastValue;

        public void Save(string name, int age) => LastValue = name.Length + age;

        public void Delete(int id) => LastValue = id;
    }

    private static readonly ActionParameter[] SaveParameters =
    {
        new Parameter { Value = "abc" },
        new Parameter { Value = 42 },
    };

    [Fact]
    public void Execute_WithParameters_InvokesMatchingOverload()
    {
        var target = new Target();
        var action = new CommandAction(target, "Save", ActionUnavailableBehaviour.Throw, ActionUnavailableBehaviour.Throw, SaveParameters);

        action.Execute(null);

        Assert.Equal(3 + 42, target.LastValue);
    }

    [Fact]
    public void Execute_WithAttachedParameters_InvokesMatchingOverload()
    {
        // Simulates the attached-collection path via the Subject control.
        var target = new Target();
        var button = new Button();
        Action.SetParameters(button, new ActionParameterCollection { new Parameter { Value = 7 } });
        View.SetActionTarget(button, target);

        var action = new CommandAction(button, null, "Delete", ActionUnavailableBehaviour.Throw, ActionUnavailableBehaviour.Throw);

        action.Execute(null);

        Assert.Equal(7, target.LastValue);
    }

    [Fact]
    public void Execute_WithParametersAndCommandParameter_Throws()
    {
        var target = new Target();
        var action = new CommandAction(target, "Save", ActionUnavailableBehaviour.Throw, ActionUnavailableBehaviour.Throw, SaveParameters);

        var ex = Assert.Throws<InvalidOperationException>(() => action.Execute("unexpected"));
        Assert.Contains("CommandParameter", ex.Message);
    }
}
