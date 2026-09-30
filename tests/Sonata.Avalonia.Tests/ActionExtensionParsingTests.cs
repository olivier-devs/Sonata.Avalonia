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
        // Property paths are now named-element references (single dot) — superseded by
        // Parse_NamedElement_* tests. What still throws is a multi-dot path.
        Assert.Throws<InvalidOperationException>(() => Parse("Save(NameTextBox.Text.Suffix)"));
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

    [Fact]
    public void Parse_NamedElement_SingleDot_ProducesNamedElementParameter()
    {
        var (_, parameters) = Parse("Save(NameTextBox.Text)");
        var p = Assert.IsType<NamedElementParameter>(Assert.Single(parameters));
        Assert.Equal("NameTextBox", p.Name);
        Assert.Equal("Text", p.Path);
    }

    [Fact]
    public void Parse_NamedElement_MultiDot_Throws()
    {
        Assert.Throws<InvalidOperationException>(() => Parse("Save(A.B.C)"));
    }

    [Fact]
    public void Parse_DollarTokenWithDot_Throws()
    {
        Assert.Throws<InvalidOperationException>(() => Parse("Save($x.y)"));
    }

    [Fact]
    public void Parse_EventArgsToken_ProducesEventArgsParameter()
    {
        var (_, parameters) = Parse("Save($eventArgs)");
        Assert.IsType<EventArgsParameter>(Assert.Single(parameters));
    }

    [Fact]
    public void Parse_SourceToken_ProducesSourceParameter()
    {
        var (_, parameters) = Parse("Save($source)");
        Assert.IsType<SourceParameter>(Assert.Single(parameters));
    }

    [Fact]
    public void Parse_ViewToken_ProducesViewParameter()
    {
        var (_, parameters) = Parse("Save($view)");
        Assert.IsType<ViewParameter>(Assert.Single(parameters));
    }

    [Fact]
    public void Parse_MixedArgsWithSpecialToken_Splits()
    {
        var (name, parameters) = Parse("Save(42;$eventArgs)");
        Assert.Equal("Save", name);
        Assert.Equal(2, parameters.Count);
        Assert.Equal(42, Assert.IsType<Parameter>(parameters[0]).Value);
        Assert.IsType<EventArgsParameter>(parameters[1]);
    }
}
