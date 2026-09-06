using System.Reflection;
using Sonata.Avalonia.Xaml;
using Xunit;

namespace Sonata.Avalonia.Tests;

public class ActionMethodResolverTests
{
    public class Target
    {
        public void Save() { }
        public void Save(string name) { }
        public void Save(string name, int age) { }
        public void Delete(int id) { }
        public void Delete(string id) { }
        public void Nullable(int? value) { }
        public void Coerce(int value) { }
    }

    private static readonly BindingFlags InstanceFlags = BindingFlags.Public | BindingFlags.Instance;

    private static MethodInfo Resolve(string name, params object?[] args)
        => ActionMethodResolver.Instance.Resolve(typeof(Target), name, args, InstanceFlags)!;

    [Fact]
    public void Resolve_FiltersByArgumentCount()
    {
        var method = Resolve("Save", "name", 42);
        Assert.Equal(2, method.GetParameters().Length);
    }

    [Fact]
    public void Resolve_ReturnsNull_WhenNotFound()
    {
        Assert.Null(ActionMethodResolver.Instance.Resolve(typeof(Target), "Nope", Array.Empty<object?>(), InstanceFlags));
    }

    [Fact]
    public void Resolve_Throws_WhenAmbiguous()
    {
        Assert.Throws<AmbiguousActionMethodException>(() => Resolve("Delete", "42"));
    }

    [Fact]
    public void Resolve_ExactTypeMatch_Wins()
    {
        var method = Resolve("Delete", 42);
        Assert.Equal(typeof(int), method.GetParameters()[0].ParameterType);
    }

    [Fact]
    public void Resolve_CoercesStringLiteral_ToInt()
    {
        // 'Coerce' has only an int overload, so the string literal "42" must be coerced.
        var method = Resolve("Coerce", "42");
        Assert.Equal(typeof(int), method.GetParameters()[0].ParameterType);
    }

    [Fact]
    public void Resolve_NullArgument_MatchesNullable()
    {
        var method = Resolve("Nullable", (object?)null);
        Assert.Equal(typeof(int?), method.GetParameters()[0].ParameterType);
    }
}
