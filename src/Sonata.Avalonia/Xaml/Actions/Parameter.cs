namespace Sonata.Avalonia.Xaml;

/// <summary>
/// A parameter whose value comes from a bindable <see cref="Value"/> property. Bind it in XAML and
/// the bound value is passed to the action method.
/// </summary>
public class Parameter : ActionParameter
{
    /// <summary>Defines the <see cref="Value"/> property.</summary>
    public static readonly StyledProperty<object?> ValueProperty =
        AvaloniaProperty.Register<Parameter, object?>(nameof(Value));

    /// <summary>Gets or sets the value to pass to the action method (usually via a XAML binding).</summary>
    public object? Value
    {
        get => GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    /// <inheritdoc />
    public override object? GetValue(ActionExecutionContext context) => Value;
}
