namespace Sonata.Avalonia.Xaml;

/// <summary>
/// A parameter that resolves to the control that triggered the action — declared as <c>$source</c>
/// in compact syntax or <c>&lt;s:SourceParameter /&gt;</c>.
/// </summary>
public sealed class SourceParameter : ActionParameter
{
    /// <inheritdoc />
    public override object? GetValue(ActionExecutionContext context) => context.Source;
}
