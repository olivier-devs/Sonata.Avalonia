# Actions

Binds methods on view models to XAML controls via the `{s:Action}` markup extension. Supports commands (`ICommand`) and event handlers. Handles guard methods for can-execute logic and async method observation.

## Key types

| Type | Role | Package |
|------|------|---------|
| `ActionExtension` | Markup extension `{s:Action MethodName}` returning `ICommand` or event handler | `Sonata.Avalonia.Xaml` |
| `CommandAction` | `ICommand` implementation calling a method on `View.ActionTarget` | `Sonata.Avalonia.Xaml` |
| `EventAction` | Event handler delegating to a method on `View.ActionTarget` | `Sonata.Avalonia.Xaml` |
| `ActionUnavailableBehaviour` | Enum: `Default`, `Enable`, `Disable`, `Throw` | `Sonata.Avalonia.Xaml` |
| `ActionTargetNullException` | Thrown when `View.ActionTarget` is null and behaviour is `Throw` | `Sonata.Avalonia.Xaml` |
| `ActionNotFoundException` | Thrown when method not found on target and behaviour is `Throw` | `Sonata.Avalonia.Xaml` |
| `ActionNotSetException` | Thrown when `View.ActionTarget` not inherited (e.g., in ContextMenu/Popup) | `Sonata.Avalonia.Xaml` |
| `ActionSignatureInvalidException` | Thrown when method signature doesn't match expected pattern | `Sonata.Avalonia.Xaml` |
| `ActionEventSignatureInvalidException` | Thrown when event handler signature is invalid for `EventAction` | `Sonata.Avalonia.Xaml` |

## Use cases

### Button command with guard method

`CommandAction` implements `ICommand`. If the view model has a `Can<MethodName>` bool property, it is observed and controls `CanExecute`:

```xml
<!-- ShellView.axaml (from samples/Sonata.Samples.HelloDialog) -->
<Button Command="{s:Action ShowDialog}">Show Dialog</Button>
```

```csharp
// ShellViewModel.cs
public class ShellViewModel : Screen
{
    private readonly IWindowManager _windowManager;
    private string _nameString = "Click the button to show the dialog";
    public string NameString
    {
        get => _nameString;
        set => SetAndNotify(ref _nameString, value);
    }

    // Guard for CanShowDialog — button enabled only when this returns true
    public bool CanShowDialog => !string.IsNullOrEmpty(NameString);

    public async Task ShowDialog()
    {
        // ...
    }
}
```

`CommandAction` watches `View.ActionTarget` (inherited from parent). If the method has a parameter, `CommandParameter` is passed. CanExecuteChanged is dispatched to the UI thread via `UiThreadDispatch.OnUIThread` (line 117 in `CommandAction.cs`).

### Event handler binding

`EventAction` returns a delegate suitable for attaching to events. Method signatures supported:

- `Method()` — no parameters
- `Method(EventArgs e)` — event args only
- `Method(object sender, EventArgs e)` — sender and event args

```xml
<ListBox DoubleClicked="{s:Action OpenItem}">
    <TextBlock PointerPressed="{s:Action PointerDown}">Press me</TextBlock>
</ListBox>
```

```csharp
public void OpenItem()
{
    // Handle double-click
}

public void PointerDown(PointerPressedEventArgs e)
{
    // e is the Avalonia event args
}
```

> `Disable` behaviour is invalid for events (line 51-56 in `EventAction.cs` throws `ArgumentException`).

### ActionTarget null and missing action behaviour

`ActionExtension` exposes `NullTarget` and `ActionNotFound` properties controlling what happens when `View.ActionTarget` is null or the method doesn't exist:

```xml
<!-- Default for commands: Disable if the target is null, Throw if the method is not found -->
<Button Command="{s:Action DoSomething}">Do</Button>

<!-- Explicit: throw if View.ActionTarget is null -->
<Button Command="{s:Action DoSomething, NullTarget=Throw}">Do</Button>

<!-- Explicit: throw if method not found -->
<Button Command="{s:Action DoSomething, ActionNotFound=Throw}">Do</Button>
```

`ActionNotSetException` is thrown when a control is in a `ContextMenu` or `Popup` and has not inherited `View.ActionTarget`. The error message (line 208-210 in `ActionBase.cs`) explains this explicitly:

> "View.ActionTarget not set on control {x} (method {y}). This probably means the control hasn't inherited it from a parent, e.g. because a ContextMenu or Popup sits in the visual tree. You will need to set 's:View.ActionTarget' explicitly."

Fix by setting `s:View.ActionTarget` on the popup/menu or its direct children.

### Async method fire-and-forget observation

When a bound method returns `Task`, the task is observed via `FireAndForget.Run` (line 239-242 in `ActionBase.cs`):

```csharp
// ActionBase.InvokeTargetMethod line 237-242:
var result = TargetMethodInfo.Invoke(target, parameters);
if (result is Task task)
{
    FireAndForget.Run(task, _logger);
}
```

`FireAndForget` (`src/Sonata.Avalonia/Internal/FireAndForget.cs`) logs unhandled exceptions rather than swallowing them silently. Exceptions in fire-and-forget tasks are captured via `TaskScheduler.Default` continuation.

### DebugConverter and EqualityConverter

These converters ship in the same namespace for convenience:

```xml
<!-- DebugConverter logs every binding value with Debug.WriteLine -->
<TextBlock Text="{Binding Value, Converter={x:Static s:DebugConverter.Instance}}" />

<!-- EqualityConverter: enables a button when two values match -->
<Button Content="Apply"
        Command="{s:Action Apply}"
        IsEnabled="{Binding SelectedItem, Converter={x:Static s:EqualityConverter.Instance}}" />
```

## See also

- [`src/Sonata.Avalonia/Xaml/ActionExtension.cs`](../../src/Sonata.Avalonia/Xaml/ActionExtension.cs) — markup extension and `ActionUnavailableBehaviour`
- [`src/Sonata.Avalonia/Xaml/CommandAction.cs`](../../src/Sonata.Avalonia/Xaml/CommandAction.cs) — `ICommand` with guard observation
- [`src/Sonata.Avalonia/Xaml/EventAction.cs`](../../src/Sonata.Avalonia/Xaml/EventAction.cs) — event handler delegate
- [`src/Sonata.Avalonia/Xaml/ActionBase.cs`](../../src/Sonata.Avalonia/Xaml/ActionBase.cs) — `InvokeTargetMethod` with `FireAndForget` task observation
- [`src/Sonata.Avalonia/Internal/FireAndForget.cs`](../../src/Sonata.Avalonia/Internal/FireAndForget.cs) — task observation with exception logging
- [`src/Sonata.Avalonia/Xaml/DebugConverter.cs`](../../src/Sonata.Avalonia/Xaml/DebugConverter.cs) — binding debug logger
- [`src/Sonata.Avalonia/Xaml/EqualityConverter.cs`](../../src/Sonata.Avalonia/Xaml/EqualityConverter.cs) — multi-value equality check
- [`samples/Sonata.Samples.HelloDialog/ShellView.axaml`](../../samples/Sonata.Samples.HelloDialog/ShellView.axaml) — `{s:Action}` on button
- [Window Manager](./window-manager.md) — dialogs opened via `IWindowManager.ShowDialog`
