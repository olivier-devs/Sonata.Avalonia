namespace Sonata.Avalonia.Xaml;

/// <summary>
/// A parameter that resolves to the root object of the XAML file the action was declared in —
/// declared as <c>$view</c> in compact syntax or <c>&lt;s:ViewParameter /&gt;</c>.
/// </summary>
public sealed class ViewParameter : ActionParameter
{
    /// <inheritdoc />
    public override object? GetValue(ActionExecutionContext context)
        => context.View ?? throw new InvalidOperationException(
            "The '$view' parameter is unavailable: no XAML root object was captured when the action was created.");
}
