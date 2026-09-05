namespace Sonata.Avalonia.Xaml;

/// <summary>
/// Hosts the <c>s:Action.Parameters</c> attached property, allowing multiple parameters to be
/// declared on a control alongside <c>{s:Action Method}</c>. Follows the same pattern as
/// Avalonia.Xaml.Behaviors' <c>Interaction.Behaviors</c>.
/// </summary>
public static class Action
{
    /// <summary>Defines the <c>Parameters</c> attached property.</summary>
    public static readonly AttachedProperty<ActionParameterCollection?> ParametersProperty =
        AvaloniaProperty.RegisterAttached<Control, ActionParameterCollection?>(
            "Parameters", typeof(Action), default(ActionParameterCollection?), inherits: false);

    /// <summary>
    /// Gets the <see cref="ActionParameterCollection"/> associated with <paramref name="control"/>,
    /// lazily creating and attaching an empty collection on first access. Never returns null:
    /// the XAML compiler populates attached collections through this getter (same contract as
    /// Avalonia.Xaml.Behaviors' <c>Interaction.Behaviors</c>).
    /// </summary>
    public static ActionParameterCollection GetParameters(Control control)
    {
        var collection = control.GetValue(ParametersProperty);
        if (collection is null)
        {
            collection = new ActionParameterCollection();
            control.SetValue(ParametersProperty, collection);
        }
        return collection;
    }

    /// <summary>Sets the <see cref="ActionParameterCollection"/> associated with <paramref name="control"/>.</summary>
    public static void SetParameters(Control control, ActionParameterCollection? value)
        => control.SetValue(ParametersProperty, value);
}
