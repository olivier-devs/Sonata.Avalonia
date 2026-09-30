using System.Reactive.Linq;

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

    /// <summary>
    /// Returns an observable that emits whenever this parameter's resolved value changes, so a
    /// <c>CommandAction</c> can re-evaluate its <c>CanExecute</c> state. The default is an empty
    /// observable (never emits): static sources such as <see cref="DataContextParameter"/> do not
    /// notify. Mutable sources (<see cref="Parameter"/>, <c>NamedElementParameter</c>) override this.
    /// </summary>
    public virtual IObservable<object?> GetChanges() => Observable.Empty<object?>();
}
