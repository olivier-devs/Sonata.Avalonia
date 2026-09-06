namespace Sonata.Avalonia.Xaml;

/// <summary>
/// A parameter that resolves to the <c>DataContext</c> of the control that triggered the action.
/// Lets an action call a method on the <c>View.ActionTarget</c> while passing the local
/// <c>DataContext</c> (e.g. the current item of an <c>ItemsControl</c>) as an argument.
/// </summary>
public sealed class DataContextParameter : ActionParameter
{
    /// <inheritdoc />
    public override object? GetValue(ActionExecutionContext context) => context.DataContext;
}
