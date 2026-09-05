using Avalonia.Headless.XUnit;
using Sonata.Avalonia.Xaml;
using Xunit;

namespace Sonata.Avalonia.Headless.Tests;

public class ParameterizedGuardHeadlessTests
{
    public class Target
    {
        public bool CanSave(string name, int age) => !string.IsNullOrWhiteSpace(name) && age >= 18;

        public void Save(string name, int age) { }
    }

    [AvaloniaFact]
    public void CanExecuteChanged_Fires_WhenParameterValueChanges()
    {
        var target = new Target();
        var name = new Parameter { Value = "" };
        var age = new Parameter { Value = 20 };
        var action = new CommandAction(target, "Save", ActionUnavailableBehaviour.Throw, ActionUnavailableBehaviour.Throw,
            new ActionParameter[] { name, age });

        Assert.False(action.CanExecute(null));

        var fired = false;
        action.CanExecuteChanged += (_, _) => fired = true;

        name.Value = "Alice";

        Assert.True(fired);
        Assert.True(action.CanExecute(null));
    }
}
