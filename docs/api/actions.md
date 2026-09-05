# Actions

Binds methods on view models to XAML controls via the `{s:Action}` markup extension. Supports commands (`ICommand`) and event handlers, single-parameter passing through `CommandParameter`, multiple parameters through `s:Action.Parameters`, and parameterized guard methods for can-execute logic. Handles async method observation.

## Key types

| Type | Role | Package |
|------|------|---------|
| `ActionExtension` | Markup extension `{s:Action MethodName}` returning `ICommand` or event handler; parses the compact single-argument syntax `Method(arg)` | `Sonata.Avalonia.Xaml` |
| `CommandAction` | `ICommand` implementation calling a method on `View.ActionTarget`, with parameterized Execute/CanExecute | `Sonata.Avalonia.Xaml` |
| `EventAction` | Event handler delegating to a method on `View.ActionTarget`, with parameterized invocation | `Sonata.Avalonia.Xaml` |
| `ActionUnavailableBehaviour` | Enum: `Default`, `Enable`, `Disable`, `Throw` | `Sonata.Avalonia.Xaml` |
| `ActionTargetNullException` | Thrown when `View.ActionTarget` is null and behaviour is `Throw` | `Sonata.Avalonia.Xaml` |
| `ActionNotFoundException` | Thrown when method not found on target and behaviour is `Throw` | `Sonata.Avalonia.Xaml` |
| `ActionNotSetException` | Thrown when `View.ActionTarget` not inherited (e.g., in ContextMenu/Popup) | `Sonata.Avalonia.Xaml` |
| `ActionSignatureInvalidException` | Thrown when method signature doesn't match expected pattern | `Sonata.Avalonia.Xaml` |
| `ActionEventSignatureInvalidException` | Thrown when event handler signature is invalid for `EventAction` | `Sonata.Avalonia.Xaml` |
| `ActionParameter` | Abstract base for value sources passed to action methods; extension point for future sources | `Sonata.Avalonia.Xaml` |
| `Parameter` | `<s:Parameter Value="..."/>` — bindable value source (binding or literal) | `Sonata.Avalonia.Xaml` |
| `DataContextParameter` | `<s:DataContextParameter />` — resolves to the trigger control's `DataContext` | `Sonata.Avalonia.Xaml` |
| `ActionParameterCollection` | `AvaloniaList<ActionParameter>` attached to a control via `s:Action.Parameters` | `Sonata.Avalonia.Xaml` |
| `Action` | Static class hosting the `s:Action.Parameters` attached property | `Sonata.Avalonia.Xaml` |
| `ActionExecutionContext` | Immutable invocation snapshot (`Target`, `Source`, `DataContext`, `EventArgs`); parameter sources read from it | `Sonata.Avalonia.Xaml` |
| `IActionMethodResolver` | Resolves a named method to an overload based on the supplied arguments | `Sonata.Avalonia.Xaml` |
| `ActionMethodResolver` | Default `IActionMethodResolver` implementation (count filter + type compatibility + minimal string→primitive coercion, invariant culture) | `Sonata.Avalonia.Xaml` |
| `AmbiguousActionMethodException` | Thrown when more than one overload matches the supplied arguments | `Sonata.Avalonia.Xaml` |

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

### Multiple parameters

Actions progress from 0 to N parameters without breaking the existing single-parameter path:

```xml
<!-- 0 parameters — unchanged, zero regression -->
<Button Command="{s:Action Save}" />

<!-- 1 parameter — unchanged, CommandParameter as today -->
<Button Command="{s:Action Delete}" CommandParameter="{Binding}" />

<!-- 2+ parameters — s:Action.Parameters attached collection (canonical API) -->
<Button Command="{s:Action Save}">
    <s:Action.Parameters>
        <s:Parameter Value="{Binding Name}" />
        <s:Parameter Value="{Binding Age}" />
    </s:Action.Parameters>
</Button>
```

The attached collection lives under `s:Action.Parameters` and is lazy-created on first access (same pattern as `Interaction.Behaviors` from Avalonia.Xaml.Behaviors). It is populated by the XAML compiler when the control is loaded.

#### Value sources

Each entry in `<s:Action.Parameters>` is an `ActionParameter` — a self-contained source of one value:

```xml
<!-- Binding (typical) -->
<s:Parameter Value="{Binding Name}" />

<!-- Literal — typed by attribute value -->
<s:Parameter Value="42" />
<s:Parameter Value="null" />

<!-- Special value — resolves to the trigger control's DataContext -->
<s:DataContextParameter />
```

`<s:Parameter>` exposes a bindable `Value` styled property — Avalonia's binding engine writes the resolved value into it, so the action reads the live value at invocation time. `<s:DataContextParameter>` carries no value: at execute time it returns `context.DataContext` (the trigger control's `DataContext`, typically the current item of an `ItemsControl`). This distinction matters in a `DataTemplate`:

```xml
<!-- Inside an ItemTemplate, ActionTarget is the parent view-model,
     DataContext is the row. Delete(row) — call a method on the parent,
     pass the row. -->
<ListBox ItemsSource="{Binding Customers}">
    <ListBox.ItemTemplate>
        <DataTemplate>
            <Button Command="{s:Action Delete}">
                <s:Action.Parameters>
                    <s:DataContextParameter />
                </s:Action.Parameters>
            </Button>
        </DataTemplate>
    </ListBox.ItemTemplate>
</ListBox>
```

```csharp
// ShellViewModel.cs — the parent that owns the collection
public void Delete(Customer customer) { /* ... */ }
```

#### Compact syntax (single argument, bare tokens only)

`ActionExtension` also accepts a compact inline form `Method(arg)` as a sugar for the common single-argument case:

```xml
<Button Command="{s:Action Save}" />
<Button Command="{s:Action Delete(42)}" />
<Button Command="{s:Action Delete($dataContext)}" />
```

The XAML surface for the compact form is **intentionally narrow**: Avalonia's XamlX markup extension tokenizer does not support quotes or commas inside a single markup extension argument. The following therefore fail to load with `XamlX.XamlParseException: Quote characters out of place`:

- `{s:Action Save('Draft')}` — quoted strings are not expressible in compact XAML.
- `{s:Action Save('Alice', 42)}` — multi-argument compact syntax is not expressible in compact XAML.

Two headless tests pin this platform limitation: `Action_XmlEndToEnd_CompactSyntax_QuotedString_ThrowsAtLoad` and `Action_XmlEndToEnd_CompactSyntax_MultipleArguments_ThrowsAtLoad`. The internal `ParseMethod` / `ParseToken` helpers still accept quoted strings and multiple arguments when invoked programmatically (e.g. from `ParseMethod` unit tests); only the XAML surface is restricted.

Tokens recognised in compact XAML:

| Token | Resolves to |
|-------|-------------|
| `$dataContext` | `DataContextParameter` (trigger control's `DataContext`) |
| `42`, `3.14` | `Parameter` whose `Value` is `int` / `double` (invariant culture) |
| `true` / `false` | `Parameter` whose `Value` is `bool` |
| `null` | `Parameter` whose `Value` is `null` |
| Any bare word (no dot, no quotes, no comma) | `Parameter` whose `Value` is the word as a `string` |

A token containing a dot (`Foo.Bar`) is rejected with `InvalidOperationException` at load time — property paths are reserved for named elements in a later release. To pass a string literal that contains spaces or punctuation, or to pass more than one argument, declare the parameters declaratively with `<s:Action.Parameters>`.

To pass the literal string `"$dataContext"`, also use the declarative form — the `$` prefix is only meaningful as a bare token in compact syntax.

#### Conflicts (fail at Execute)

Both of these combinations are rejected at `Execute` time with `InvalidOperationException` (the XAML compiler processes `Command` before the property element, so the check cannot happen at load time):

- **`CommandParameter` + `s:Action.Parameters`** — pick one path. The 1-parameter path uses `CommandParameter`; the multi-parameter path uses the attached collection.
- **Compact inline + attached `<s:Action.Parameters>`** — pick one path. If you wrote `{s:Action Delete(42)}` and also declared `<s:Action.Parameters>`, this combination throws.

### Parameterized guards

When the action method takes parameters, the matching guard is a `Can<MethodName>` **method** with the same parameters (and same order), returning `bool` synchronously:

```csharp
public void Save(string name, int age) { /* ... */ }

public bool CanSave(string name, int age)
{
    return !string.IsNullOrWhiteSpace(name) && age >= 18;
}
```

```xml
<Button Command="{s:Action Save}">
    <s:Action.Parameters>
        <s:Parameter Value="{Binding Name}" />
        <s:Parameter Value="{Binding Age}" />
    </s:Action.Parameters>
</Button>
```

Resolution order (for a method with N parameters):

1. **`Can<Method>` method** with N parameters → used as the guard.
2. **Otherwise** `Can<Method>` property (existing behaviour) → used as the guard.
3. **Otherwise** no guard → always executable.

If **both** a parameterized method and a property are present, the method wins and a warning is logged (the property is treated as redundant). The same `Can<Method>` form applies to the 1-parameter `CommandParameter` path: `CanDelete(object value)` is consulted identically to a `<s:Action.Parameters>` collection with one entry.

The guard must return `bool` synchronously — a `Can*` method returning `Task` is not supported (`ActionSignatureInvalidException`).

#### Re-evaluation chain

```text
Binding change (Name, Age, ...)
    ↓
Parameter.ValueProperty change (StyledProperty — the binding writes into it)
    ↓
CommandAction subscription fires
    ↓
Re-resolve guard with current argument values
    ↓
CanExecuteChanged (dispatched to the UI thread, existing mechanism)
    ↓
Button.IsEnabled updated
```

The argument values used by the guard are the same descriptors as the action itself, resolved at the same moment — the guard and the method can never observe different values. Re-evaluation also fires when `View.ActionTarget` changes (existing behaviour).

## See also

- [`src/Sonata.Avalonia/Xaml/ActionExtension.cs`](../../src/Sonata.Avalonia/Xaml/ActionExtension.cs) — markup extension, `ActionUnavailableBehaviour`, compact-syntax parsing (`ParseMethod` / `ParseToken`)
- [`src/Sonata.Avalonia/Xaml/CommandAction.cs`](../../src/Sonata.Avalonia/Xaml/CommandAction.cs) — `ICommand` with guard observation and parameterized Execute/CanExecute
- [`src/Sonata.Avalonia/Xaml/EventAction.cs`](../../src/Sonata.Avalonia/Xaml/EventAction.cs) — event handler delegate with parameterized invocation
- [`src/Sonata.Avalonia/Xaml/ActionBase.cs`](../../src/Sonata.Avalonia/Xaml/ActionBase.cs) — `InvokeTargetMethod` with `FireAndForget` task observation, parameter plumbing, conflict checks
- [`src/Sonata.Avalonia/Xaml/Actions/Action.cs`](../../src/Sonata.Avalonia/Xaml/Actions/Action.cs) — `s:Action.Parameters` attached property
- [`src/Sonata.Avalonia/Xaml/Actions/ActionParameter.cs`](../../src/Sonata.Avalonia/Xaml/Actions/ActionParameter.cs) — abstract value-source base
- [`src/Sonata.Avalonia/Xaml/Actions/Parameter.cs`](../../src/Sonata.Avalonia/Xaml/Actions/Parameter.cs) — bindable value parameter
- [`src/Sonata.Avalonia/Xaml/Actions/DataContextParameter.cs`](../../src/Sonata.Avalonia/Xaml/Actions/DataContextParameter.cs) — `DataContext` source
- [`src/Sonata.Avalonia/Xaml/Actions/ActionParameterCollection.cs`](../../src/Sonata.Avalonia/Xaml/Actions/ActionParameterCollection.cs) — attached collection
- [`src/Sonata.Avalonia/Xaml/Actions/ActionExecutionContext.cs`](../../src/Sonata.Avalonia/Xaml/Actions/ActionExecutionContext.cs) — invocation snapshot
- [`src/Sonata.Avalonia/Xaml/Actions/ActionMethodResolver.cs`](../../src/Sonata.Avalonia/Xaml/Actions/ActionMethodResolver.cs) — overload resolver (count + type compatibility + minimal string→primitive coercion)
- [`src/Sonata.Avalonia/Xaml/Actions/AmbiguousActionMethodException.cs`](../../src/Sonata.Avalonia/Xaml/Actions/AmbiguousActionMethodException.cs) — ambiguous-match exception
- [`src/Sonata.Avalonia/Internal/FireAndForget.cs`](../../src/Sonata.Avalonia/Internal/FireAndForget.cs) — task observation with exception logging
- [`src/Sonata.Avalonia/Xaml/DebugConverter.cs`](../../src/Sonata.Avalonia/Xaml/DebugConverter.cs) — binding debug logger
- [`src/Sonata.Avalonia/Xaml/EqualityConverter.cs`](../../src/Sonata.Avalonia/Xaml/EqualityConverter.cs) — multi-value equality check
- [`samples/Sonata.Samples.HelloDialog/ShellView.axaml`](../../samples/Sonata.Samples.HelloDialog/ShellView.axaml) — `{s:Action}` on button
- [Window Manager](./window-manager.md) — dialogs opened via `IWindowManager.ShowDialog`
