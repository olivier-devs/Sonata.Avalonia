using Sonata.Avalonia.Xaml;
using Xunit;

namespace Sonata.Avalonia.Tests;

public class ParameterizedEventActionTests
{
    public class Target
    {
        public int LastValue;

        public void Open(int id) => LastValue = id;
    }

    [Fact]
    public void Invoke_WithParameters_UsesParameterValues()
    {
        var target = new Target();
        var action = new EventAction(
            target,
            typeof(EventHandler),
            "Open",
            ActionUnavailableBehaviour.Throw,
            ActionUnavailableBehaviour.Throw,
            new ActionParameter[] { new Parameter { Value = 99 } });

        var del = action.GetDelegate();
        ((EventHandler)del)(this, EventArgs.Empty);

        Assert.Equal(99, target.LastValue);
    }
}
