namespace Sonata.Avalonia.Xaml;

/// <summary>
/// A source of a single value passed to an action method. Each concrete type knows how to obtain
/// its value from an <see cref="ActionExecutionContext"/>. This is the extension point for future
/// parameter sources (named elements, event args, ...).
/// </summary>
public abstract class ActionParameter : AvaloniaObject
{
    /// <summary>
    /// Resolves the current value of this parameter for the given execution context.
    /// </summary>
    public abstract object? GetValue(ActionExecutionContext context);
}
