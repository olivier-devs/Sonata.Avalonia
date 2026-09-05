using System.ComponentModel;
using Sonata.Avalonia.Xaml;
using Xunit;

namespace Sonata.Avalonia.Tests;

public class ParameterizedGuardTests
{
    public class Target : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        public bool CanSave(string name, int age) => !string.IsNullOrWhiteSpace(name) && age >= 18;

        public void Save(string name, int age) { }
    }

    [Fact]
    public void CanExecute_UsesParameterizedGuard()
    {
        var target = new Target();
        var name = new Parameter { Value = "" };
        var age = new Parameter { Value = 20 };
        var action = new CommandAction(target, "Save", ActionUnavailableBehaviour.Throw, ActionUnavailableBehaviour.Throw,
            new ActionParameter[] { name, age });

        Assert.False(action.CanExecute(null));

        name.Value = "Alice";
        Assert.True(action.CanExecute(null));
    }
}
