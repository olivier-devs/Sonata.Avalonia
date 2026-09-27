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
}
