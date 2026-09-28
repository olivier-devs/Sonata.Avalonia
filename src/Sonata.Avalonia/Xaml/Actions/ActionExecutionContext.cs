namespace Sonata.Avalonia.Xaml;

/// <summary>
/// Immutable snapshot of the context in which an action is invoked. Exposed to parameter sources
/// (<see cref="ActionParameter"/>) so they resolve their value without reaching into the view.
/// </summary>
public sealed class ActionExecutionContext
{
    /// <summary>The resolved <c>View.ActionTarget</c> the action will be invoked on.</summary>
    public required object? Target { get; init; }

    /// <summary>The control that triggered the action, if any.</summary>
    public required AvaloniaObject? Source { get; init; }

    /// <summary>The <c>DataContext</c> of <see cref="Source"/>, when the source is a control.</summary>
    public object? DataContext { get; init; }

    /// <summary>Event arguments when the action was raised by an event; otherwise null.</summary>
    public object? EventArgs { get; init; }

    /// <summary>
    /// The root object of the XAML file the action was declared in, when it could be captured at
    /// construction; otherwise null. Optional (not <c>required</c>) to stay source-compatible.
    /// </summary>
    public object? View { get; init; }
}
