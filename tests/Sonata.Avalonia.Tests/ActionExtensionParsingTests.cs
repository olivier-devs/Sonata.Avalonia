using Sonata.Avalonia.Xaml;
using Xunit;

namespace Sonata.Avalonia.Tests;

public class ActionExtensionParsingTests
{
    private static (string, IReadOnlyList<ActionParameter>) Parse(string method)
        => ActionExtension.ParseMethod(method);

    [Fact]
    public void Parse_NoArguments_ReturnsEmptyParameters()
    {
        var (name, parameters) = Parse("Save");
        Assert.Equal("Save", name);
        Assert.Empty(parameters);
    }

    [Fact]
    public void Parse_Literals_TypesThem()
    {
        var (_, parameters) = Parse("Delete(42, 'Draft', true, null)");
        Assert.Equal(4, parameters.Count);
        Assert.Equal(42, Assert.IsType<Parameter>(parameters[0]).Value);
        Assert.Equal("Draft", Assert.IsType<Parameter>(parameters[1]).Value);
        Assert.Equal(true, Assert.IsType<Parameter>(parameters[2]).Value);
        Assert.Null(Assert.IsType<Parameter>(parameters[3]).Value);
    }

    [Fact]
    public void Parse_DataContext_ReturnsDataContextParameter()
    {
        var (_, parameters) = Parse("Delete($dataContext)");
        Assert.IsType<DataContextParameter>(parameters[0]);
    }

    [Fact]
    public void Parse_PropertyPath_Throws()
    {
        Assert.Throws<InvalidOperationException>(() => Parse("Save(NameTextBox.Text)"));
    }
}
