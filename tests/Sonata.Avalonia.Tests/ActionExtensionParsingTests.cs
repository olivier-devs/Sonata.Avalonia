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

    [Fact]
    public void Parse_SemicolonSeparator_SplitsMultipleArgs()
    {
        var (name, parameters) = Parse("Save(42;43)");
        Assert.Equal("Save", name);
        Assert.Equal(2, parameters.Count);
        Assert.Equal(42, Assert.IsType<Parameter>(parameters[0]).Value);
        Assert.Equal(43, Assert.IsType<Parameter>(parameters[1]).Value);
    }

    [Fact]
    public void Parse_MixedSeparators_SplitsAll()
    {
        var (_, parameters) = Parse("Save(42;43,44)");
        Assert.Equal(3, parameters.Count);
        Assert.Equal(42, Assert.IsType<Parameter>(parameters[0]).Value);
        Assert.Equal(43, Assert.IsType<Parameter>(parameters[1]).Value);
        Assert.Equal(44, Assert.IsType<Parameter>(parameters[2]).Value);
    }

    [Fact]
    public void Parse_QuotedString_WithCommaInside_PreservesIt()
    {
        // Pins the latent V1 bug: ParseMethod("Save('a,b')") used to split inside the string.
        var (_, parameters) = Parse("Save('a,b')");
        var p = Assert.IsType<Parameter>(Assert.Single(parameters));
        Assert.Equal("a,b", p.Value);
    }

    [Fact]
    public void Parse_QuotedString_WithSemicolonInside_PreservesIt()
    {
        var (_, parameters) = Parse("Save('a;b')");
        var p = Assert.IsType<Parameter>(Assert.Single(parameters));
        Assert.Equal("a;b", p.Value);
    }

    [Fact]
    public void Parse_QuotedString_MixedWithOtherArgs()
    {
        var (_, parameters) = Parse("Save('Alice Smith';42)");
        Assert.Equal(2, parameters.Count);
        Assert.Equal("Alice Smith", Assert.IsType<Parameter>(parameters[0]).Value);
        Assert.Equal(42, Assert.IsType<Parameter>(parameters[1]).Value);
    }

    [Fact]
    public void Parse_TrailingSeparator_Throws()
    {
        Assert.Throws<InvalidOperationException>(() => Parse("Save(42;)"));
    }

    [Fact]
    public void Parse_LeadingSeparator_Throws()
    {
        Assert.Throws<InvalidOperationException>(() => Parse("Save(;42)"));
    }

    [Fact]
    public void Parse_DoubleSeparator_Throws()
    {
        Assert.Throws<InvalidOperationException>(() => Parse("Save(42; ;43)"));
    }

    [Fact]
    public void Parse_UnknownDollarToken_Throws()
    {
        Assert.Throws<InvalidOperationException>(() => Parse("Save($datacontext)"));
        Assert.Throws<InvalidOperationException>(() => Parse("Save($foo)"));
    }

    [Fact]
    public void Parse_UnterminatedQuote_Throws()
    {
        // Fail-fast on malformed input, consistent with the empty-token and unknown-$ errors.
        Assert.Throws<InvalidOperationException>(() => Parse("Save('a,b)"));
    }

    [Fact]
    public void Parse_EmptyParentheses_ReturnsEmptyParameters()
    {
        var (name, parameters) = Parse("Save()");
        Assert.Equal("Save", name);
        Assert.Empty(parameters);
    }
}
