namespace Sonata.Avalonia.Xaml;

/// <summary>
/// Resolves a named method to a concrete overload based on the arguments to pass.
/// </summary>
public interface IActionMethodResolver
{
    /// <summary>
    /// Finds the best-matching overload of <paramref name="methodName"/>, or null if none match,
    /// or throws <see cref="AmbiguousActionMethodException"/> if several match equally well.
    /// </summary>
    MethodInfo? Resolve(Type targetType, string methodName, IReadOnlyList<object?> arguments, BindingFlags bindingFlags);
}

/// <summary>
/// Default <see cref="IActionMethodResolver"/>. Filters by parameter count, then by type
/// compatibility (exact, assignable, null-safe, or minimally coercible literal).
/// </summary>
public sealed class ActionMethodResolver : IActionMethodResolver
{
    /// <summary>Shared default instance.</summary>
    public static ActionMethodResolver Instance { get; } = new();

    /// <inheritdoc />
    public MethodInfo? Resolve(Type targetType, string methodName, IReadOnlyList<object?> arguments, BindingFlags bindingFlags)
    {
        var candidates = targetType
            .GetMethods(bindingFlags)
            .Where(m => m.Name == methodName && m.GetParameters().Length == arguments.Count)
            .Where(m => IsCompatible(m.GetParameters(), arguments))
            .ToList();

        if (candidates.Count == 0)
            return null;

        if (candidates.Count > 1)
        {
            var overloads = string.Join(", ", candidates.Select(c => c.ToString()));
            throw new AmbiguousActionMethodException(
                string.Format("Multiple overloads of '{0}' on {1} match the supplied arguments: {2}",
                    methodName, targetType.Name, overloads));
        }

        return candidates[0];
    }

    private static bool IsCompatible(ParameterInfo[] parameters, IReadOnlyList<object?> arguments)
    {
        for (var i = 0; i < parameters.Length; i++)
        {
            if (!IsCompatible(parameters[i].ParameterType, arguments[i]))
                return false;
        }

        return true;
    }

    private static bool IsCompatible(Type parameterType, object? argument)
    {
        if (argument == null)
            return !parameterType.IsValueType || Nullable.GetUnderlyingType(parameterType) != null;

        return parameterType.IsInstanceOfType(argument) || TryConvert(argument, parameterType, out _);
    }

    /// <summary>
    /// Attempts a minimal literal conversion: a string literal to a primitive, enum or decimal
    /// (invariant culture). Deliberately not a general conversion engine — only string→primitive is
    /// supported, so an <c>int</c> argument is never silently coerced to <c>string</c> (which would
    /// make overload resolution ambiguous).
    /// </summary>
    internal static bool TryConvert(object? value, Type targetType, out object? converted)
    {
        converted = null;
        if (value == null)
            return false;
        if (targetType.IsInstanceOfType(value))
        {
            converted = value;
            return true;
        }
        if (value is not string s)
            return false;
        if (!(targetType.IsPrimitive || targetType.IsEnum || targetType == typeof(decimal)))
            return false;

        try
        {
            converted = Convert.ChangeType(s, targetType, CultureInfo.InvariantCulture);
            return true;
        }
        catch (Exception e) when (e is FormatException or InvalidCastException or OverflowException)
        {
            return false;
        }
    }
}
