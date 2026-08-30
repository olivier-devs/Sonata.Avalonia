# Bindables

Observable properties and collections for MVVM data binding.

## Key types

| Type | Role |
|------|------|
| `PropertyChangedBase` | Base class for ViewModels; implements `INotifyPropertyChanged` with thread-safe dispatch |
| `SetAndNotify` | `ref` field setter that compares equality and fires `PropertyChanged` only on change |
| `NotifyOfPropertyChange` | Raise `PropertyChanged` by name or from an `Expression<Func<T>>` |
| `BindableCollection<T>` | `ObservableCollection<T>` with `AddRange`/`RemoveRange`, UI-thread dispatch, `INotifyCollectionChanging` |
| `IObservableCollection<T>` | Extends `IList<T>` + `INotifyPropertyChanged` + `INotifyCollectionChanged` with range operations |
| `IReadOnlyObservableCollection<T>` | `IReadOnlyList<T>` + `INotifyCollectionChanged` + `INotifyCollectionChanging` |
| `INotifyCollectionChanging` | Pre-change event (`CollectionChanging`) enabling cancellation before item add/remove |
| `PropertyChangedExtensions` | Strong bindings to `PropertyChanged` with `Bind`/`BindAndInvoke` returning `IEventBinding` |
| `PropertyChangedExtendedEventArgs<TProperty>` | `PropertyChangedEventArgs` with `NewValue` |
| `LabelledValue<T>` | Key-value pair (`Label` + `Value`) implementing `IEquatable`; used by `MessageBoxViewModel.ButtonList` |

## Thread safety

All `PropertyChanged` and collection notifications are dispatched to the UI thread via `UiThreadDispatch.PostToUIThread`. This is SP-3 behaviour. `BindableCollection` operations are also UI-threaded internally.

## Use cases

### Notifying property with SetAndNotify

```csharp
// EmployeeModel.cs — derived from samples/Sonata.Samples.MasterDetail/ShellViewModel.cs
public class EmployeeModel : PropertyChangedBase
{
    private string _name = string.Empty;
    public string Name
    {
        get => _name;
        set => SetAndNotify(ref _name, value);
    }
}
```

`SetAndNotify<T>(ref T field, T value, [CallerMemberName] string propertyName = "")` compares using `EqualityComparer<T>.Default.Equals`, updates the field, and fires `PropertyChanged` only when the value actually changes.

### BindableCollection with INotifyCollectionChanging

Subscribe to `CollectionChanging` to react before items are added/removed. `Conductor<T>.Collection.OneActive` uses this to track items before a reset:

```csharp
// Excerpt from src/Sonata.Avalonia/ConductorOneActive.cs
public OneActive()
{
    Items.CollectionChanging += ItemsCollectionChanging;
    Items.CollectionChanged += ItemsCollectionChanged;
}

private void ItemsCollectionChanging(object? sender, NotifyCollectionChangedEventArgs e)
{
    if (e.Action == NotifyCollectionChangedAction.Reset)
        itemsBeforeReset = items.ToList(); // snapshot before reset
}
```

`INotifyCollectionChanging` fires **before** the mutation, unlike `INotifyCollectionChanged` which fires after. Use it to save state or validate pending changes.

### LabelledValue for MessageBox buttons

`LabelledValue<T>` attaches a display label to a value, useful for selectable options in a view:

```csharp
// Excerpt from src/Sonata.Avalonia/Primitive/MessageBoxViewModel.cs
public BindableCollection<LabelledValue<MessageBoxResult>> ButtonList { get; protected set; } =
    new BindableCollection<LabelledValue<MessageBoxResult>>();

// Construction:
var lbv = new LabelledValue<MessageBoxResult>("OK", MessageBoxResult.OK);
// or
LabelledValue<MessageBoxResult>.Create("OK", MessageBoxResult.OK);
```

`MessageBoxViewModel.Setup` (line 168) populates `ButtonList` using `LabelledValue` — each button gets a human-readable label and its enum value.

### Strong-binding PropertyChanged subscription

```csharp
// Subscribe to a single property with a strong reference that can be explicitly unbound
IEventBinding binding = someViewModel.Bind(
    x => x.IsBusy,
    (sender, args) => ProgressIndicator.IsVisible = args.NewValue);

// Later, to unsubscribe:
binding.Unbind();
```

`BindAndInvoke` additionally calls the handler immediately with the current value.

## See also

- [`src/Sonata.Avalonia/PropertyChangedBase.cs`](../../src/Sonata.Avalonia/PropertyChangedBase.cs) — `SetAndNotify` / `NotifyOfPropertyChange`
- [`src/Sonata.Avalonia/BindableCollection.cs`](../../src/Sonata.Avalonia/BindableCollection.cs) — range ops and `CollectionChanging`
- [`src/Sonata.Avalonia/INotifyCollectionChanging.cs`](../../src/Sonata.Avalonia/INotifyCollectionChanging.cs) — interface definition
- [`src/Sonata.Avalonia/LabelledValue.cs`](../../src/Sonata.Avalonia/LabelledValue.cs) — `LabelledValue<T>`
- [`src/Sonata.Avalonia/PropertyChangedExtensions.cs`](../../src/Sonata.Avalonia/PropertyChangedExtensions.cs) — `Bind`/`BindAndInvoke`
- [`src/Sonata.Avalonia/Primitive/MessageBoxViewModel.cs`](../../src/Sonata.Avalonia/Primitive/MessageBoxViewModel.cs) — live `LabelledValue` usage
- [`samples/Sonata.Samples.MasterDetail/ShellViewModel.cs`](../../samples/Sonata.Samples.MasterDetail/ShellViewModel.cs) — `SetAndNotify` + `BindableCollection` live example
- [Conductors](./conductors.md) — `Conductor<T>.Collection.OneActive` uses `INotifyCollectionChanging` internally
