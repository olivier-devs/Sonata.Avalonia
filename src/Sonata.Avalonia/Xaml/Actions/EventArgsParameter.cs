namespace Sonata.Avalonia.Xaml;

/// <summary>
/// A parameter that resolves to the event arguments of the event that triggered the action —
/// declared as <c>$eventArgs</c> in compact syntax or <c>&lt;s:EventArgsParameter /&gt;</c>.
/// Only valid on event actions: a command does not carry event arguments.
/// </summary>
public sealed class EventArgsParameter : ActionParameter
{
    /// <inheritdoc />
    public override object? GetValue(ActionExecutionContext context)
        => context.EventArgs ?? throw new InvalidOperationException(
            "The '$eventArgs' parameter is only valid on event actions: commands do not carry event arguments.");
}
