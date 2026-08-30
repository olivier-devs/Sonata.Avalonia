# Validation

Integrates property-level validation with `INotifyDataErrorInfo` and async validation adapters. Validates on property change (auto or manual) and exposes errors to XAML binding.

> **SP-2 breaking change:** Synchronous validation facades were removed. Validation is async-only. `ValidateProperty` and `ValidateAllProperties` (sync) no longer exist.

## Key types

| Type | Role | Package |
|------|------|---------|
| `ValidatingModelBase` | Base class with `INotifyDataErrorInfo` implementation | `Sonata.Avalonia` |
| `IModelValidator` | Interface for validation logic | `Sonata.Avalonia` |
| `IModelValidator<T>` | Generic version for IoC generic bindings | `Sonata.Avalonia` |
| `HasErrors` | `true` if any property has validation errors | `ValidatingModelBase` |
| `ErrorsChanged` | Event raised when errors for a property change | `ValidatingModelBase` |
| `GetErrors(propertyName)` | Returns validation errors for a property | `ValidatingModelBase` |
| `ValidatePropertyAsync` | Validate a single property by expression | `ValidatingModelBase` |
| `ValidateAllPropertiesAsync` | Validate all properties at once | `ValidatingModelBase` |
| `RecordPropertyError` | Manually add/clear errors without a validator | `ValidatingModelBase` |
| `RethrowingBinding` | Binding that rethrows source exceptions on the UI thread | `Sonata.Avalonia.Xaml` |

## Use cases

### Implementing IModelValidator and wiring to Screen

`IModelValidator` is the bridge between your validation library and `ValidatingModelBase`:

```csharp
// From tests/Sonata.Avalonia.Tests/TestFakes.cs
public class TestValidator : IModelValidator
{
    public Dictionary<string, string[]> Errors { get; } = new();
    public string LastValidatedProperty { get; private set; }

    public void Initialize(object subject) { } // Called once with the view model

    public Task<IEnumerable<string>> ValidatePropertyAsync(string propertyName)
    {
        LastValidatedProperty = propertyName;
        return Task.FromResult<IEnumerable<string>>(
            Errors.TryGetValue(propertyName, out var e) ? e : null);
    }

    public Task<Dictionary<string, IEnumerable<string>>> ValidateAllPropertiesAsync()
    {
        return Task.FromResult(Errors.ToDictionary(k => k.Key, v => (IEnumerable<string>)v.Value));
    }
}
```

Wire to a `Screen` or `ValidatingModelBase` via constructor injection:

```csharp
public class OrderViewModel : Screen
{
    public OrderViewModel(IModelValidator validator) : base(validator) { }

    private string _customerName;
    public string CustomerName
    {
        get => _customerName;
        set
        {
            if (SetAndNotify(ref _customerName, value))
            {
                // AutoValidate is true by default → ValidatePropertyAsync is called
            }
        }
    }
}
```

`ValidatingModelBase` has two constructors (line 40-55): parameterless (no auto-validation) and `(IModelValidator?)` which calls `validator.Initialize(this)`.

### INotifyDataErrorInfo surface

`ValidatingModelBase` implements `INotifyDataErrorInfo` (line 7). The surface:

```csharp
// HasErrors — true if any property has non-null, non-empty error array
public virtual bool HasErrors
{
    get { return propertyErrors.Values.Any(x => x != null && x.Length > 0); }
}

// ErrorsChanged — raised on UI thread via UiThreadDispatch.PostToUIThread
// line 270-274:
protected virtual void RaiseErrorsChanged(string propertyName)
{
    var handler = ErrorsChanged;
    if (handler != null)
        UiThreadDispatch.PostToUIThread(() =>
            handler(this, new DataErrorsChangedEventArgs(propertyName)));
}

// GetErrors — returns string[] for a property, or all entity-level errors
// line 282-302: returns Array.Empty<string>() for non-existent keys (SP-5 change)
public virtual IEnumerable GetErrors(string? propertyName)
{
    // ...
    return errors ?? Array.Empty<string>();
}
```

### Async validation API

All validation is async. Methods:

```csharp
// Validate a single property (expression overload):
var isValid = await ValidatePropertyAsync(x => x.CustomerName);

// Validate a single property by name:
var isValid = await ValidatePropertyAsync("CustomerName");

// Validate all properties:
var allValid = await ValidateAsync(); // returns !HasErrors
```

`OnPropertyChanged` in `ValidatingModelBase` (line 243-251) auto-triggers `ValidatePropertyAsync` when `AutoValidate = true` and the changed property isn't `HasErrors`. It uses `FireAndForget.Run` to avoid blocking the property setter:

```csharp
protected override void OnPropertyChanged(string propertyName)
{
    base.OnPropertyChanged(propertyName);
    if (Validator != null && AutoValidate && propertyName != "HasErrors")
        FireAndForget.Run(
            ValidatePropertyAsync(propertyName),
            SonataLogManager.GetLogger(GetType()));
}
```

### Manual error recording

Errors can be recorded independently of the validator:

```csharp
// Record errors for a property
RecordPropertyError(x => x.CustomerName, new[] { "Name is required" });

// Clear errors for a property
RecordPropertyError(x => x.CustomerName, null);

// Clear all errors
ClearAllPropertyErrors();
```

These fire `ErrorsChanged` and call `OnValidationStateChanged`, which raises `PropertyChanged("HasErrors")`.

## See also

- [`src/Sonata.Avalonia/ValidatingModelBase.cs`](../../src/Sonata.Avalonia/ValidatingModelBase.cs) — full `INotifyDataErrorInfo` implementation
- [`src/Sonata.Avalonia/IValidationAdapter.cs`](../../src/Sonata.Avalonia/IValidationAdapter.cs) — `IModelValidator` / `IModelValidator<T>`
- [`src/Sonata.Avalonia/Xaml/RethrowingBinding.cs`](../../src/Sonata.Avalonia/Xaml/RethrowingBinding.cs) — binding that rethrows exceptions
- [`tests/Sonata.Avalonia.Tests/TestFakes.cs`](../../tests/Sonata.Avalonia.Tests/TestFakes.cs) — `TestValidator` minimal example
- [Screen Lifecycle](./screen-lifecycle.md) — `Screen` base class
