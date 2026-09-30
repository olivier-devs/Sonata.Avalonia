using System.Reactive.Subjects;

namespace Sonata.Avalonia.Xaml;

/// <summary>
/// A parameter that resolves to the value of an <see cref="AvaloniaProperty"/> on a named element
/// (an <c>x:Name</c>d control in the same name scope as the action's subject). Produced exclusively
/// by the compact syntax <c>Save(NameTextBox.Text)</c>; the declarative equivalent is a plain
/// <c>&lt;s:Parameter Value="{Binding #NameTextBox.Text}" /&gt;</c>, which remains the canonical form.
/// </summary>
public class NamedElementParameter : ActionParameter
{
    private static ILogger Logger => SonataLogManager.GetLogger(typeof(NamedElementParameter));
    private readonly Subject<object?> _changes = new();
    private IDisposable? _elementSubscription;
    private bool _elementSubscriptionCreated;
    private bool _missingElementWarningLogged;

    /// <summary>The name of the element (its <c>x:Name</c>) to read the property from. Required.</summary>
    public required string Name { get; set; }

    /// <summary>The name of the <see cref="AvaloniaProperty"/> to read on the element. Required.</summary>
    public required string Path { get; set; }

    /// <inheritdoc />
    public override IObservable<object?> GetChanges() => _changes;

    /// <inheritdoc />
    public override object? GetValue(ActionExecutionContext context)
        => Resolve(context, out var value) ? value : null;

    /// <summary>
    /// Hard variant of <see cref="GetValue"/>: throws when the named element cannot be resolved.
    /// Used on the Execute path, where the view has finished loading and a missing element is a
    /// deterministic typo rather than a timing issue.
    /// </summary>
    internal object? GetValueStrict(ActionExecutionContext context)
        => Resolve(context, out var value)
            ? value
            : throw new InvalidOperationException(
                string.Format("Named element '{0}' referenced by an action parameter could not be found in the name scope (path '{1}').", Name, Path));

    private bool Resolve(ActionExecutionContext context, out object? value)
    {
        value = null;

        var element = FindElement(context.Source);
        if (element == null)
        {
            if (!_missingElementWarningLogged)
            {
                _missingElementWarningLogged = true;
                Logger.LogWarning("Named element '{Name}' referenced by an action parameter was not found in the name scope; treating its value as null until the element is registered.", Name);
            }

            return false;
        }

        var property = ResolveProperty(element);
        EnsureElementSubscription(element, property);
        value = element.GetValue(property);
        return true;
    }

    private void EnsureElementSubscription(AvaloniaObject element, AvaloniaProperty property)
    {
        // First successful resolution wires the element's property observable into the change
        // subject, so a CommandAction re-evaluates its guard when the property changes.
        // The flag guards against re-entrancy: Avalonia's GetObservable emits the current value
        // synchronously on subscribe, and that emission must not recurse back into Resolve.
        if (_elementSubscriptionCreated)
            return;
        _elementSubscriptionCreated = true;
        _elementSubscription = element.GetObservable(property).Subscribe(_changes);
    }

    /// <summary>
    /// Resolves the named element from the subject's name scope, walking up the logical tree to the
    /// host scope when the subject itself carries none — the same mechanism Avalonia's
    /// <c>{Binding #Name}</c> uses.
    /// </summary>
    private AvaloniaObject? FindElement(AvaloniaObject? source)
    {
        for (StyledElement? current = source as StyledElement; current != null; current = current.Parent as StyledElement)
        {
            var scope = NameScope.GetNameScope(current);
            var found = scope?.Find(Name);
            if (found is AvaloniaObject avaloniaObject)
                return avaloniaObject;
        }

        return null;
    }

    private AvaloniaProperty ResolveProperty(AvaloniaObject element)
    {
        var property = AvaloniaPropertyRegistry.Instance.FindRegistered(element.GetType(), Path);
        if (property != null)
            return property;

        // Safety-net fallback (spec-required): the registry already walks the type hierarchy,
        // but resolve via the static 'XxxProperty' field if a property ever escapes it.
        var field = element.GetType().GetField(Path + "Property", BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy);
        if (field?.GetValue(null) is AvaloniaProperty fallback)
            return fallback;

        throw new InvalidOperationException(
            string.Format("Property '{0}' on named element '{1}' ({2}) is not a registered AvaloniaProperty.", Path, Name, element.GetType().Name));
    }
}
