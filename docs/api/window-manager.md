# Window Manager

Shows views as windows or dialogs, with automatic view model binding, display name → title wiring, and lifecycle conductor integration.

## Key types

| Type | Role | Package |
|------|------|---------|
| `IWindowManager` | Interface for showing windows and dialogs | `Sonata.Avalonia` |
| `WindowManager` | Default implementation using `IViewManager` | `Sonata.Avalonia` |
| `IWindowManagerConfig` | Abstraction for `GetActiveWindow()` | `Sonata.Avalonia` |
| `ShowWindow(viewModel)` | Show a view model's view as a non-modal window | `IWindowManager` |
| `ShowWindow(viewModel, ownerViewModel)` | Show with explicit owner | `IWindowManager` |
| `ShowDialog<T>(viewModel)` | Show as dialog, infer owner from active window | `IWindowManager` |
| `ShowDialog<T>(viewModel, ownerViewModel)` | Show as dialog with explicit owner | `IWindowManager` |
| `ShowMessageBox<T>(...)` | Display a message box with buttons, icon, result | `IWindowManager` |
| `MessageBoxViewModel` | Default `IMessageBoxViewModel` implementation | `Sonata.Avalonia.Primitive` |
| `IMessageBoxViewModel` | Interface for custom message box view models | `Sonata.Avalonia.Primitive` |
| `MessageBoxButton` | Enum: `OK`, `OKCancel`, `YesNo`, `YesNoCancel` | `Sonata.Avalonia.Primitive` |
| `MessageBoxImage` | Enum: `None`, `Error`, `Question`, `Warning`, `Information` | `Sonata.Avalonia.Primitive` |
| `MessageBoxResult` | Enum: `None`, `OK`, `Cancel`, `Yes`, `No` | `Sonata.Avalonia.Primitive` |

## Use cases

### ShowDialog with factory and async/await

`samples/Sonata.Samples.HelloDialog/ShellViewModel.cs` demonstrates dialog usage:

```csharp
public class ShellViewModel : Screen
{
    private readonly IWindowManager _windowManager;
    private readonly Func<Dialog1ViewModel> _dialogFactory;

    public ShellViewModel(IWindowManager windowManager, Func<Dialog1ViewModel> dialogFactory)
    {
        DisplayName = "Hello Dialog";
        _windowManager = windowManager;
        _dialogFactory = dialogFactory;
    }

    public async Task ShowDialog()
    {
        var dialogVm = _dialogFactory();
        var result = await _windowManager.ShowDialog<bool>(dialogVm);
        NameString = result
            ? $"Your name is {dialogVm.Name}"
            : "Dialog cancelled";
    }
}
```

Owner inference: `ShowDialog<T>(viewModel)` without an explicit owner calls `InferOwnerOf` (line 225-229 in `WindowManager.cs`) which calls `IWindowManagerConfig.GetActiveWindow()`. If no active window exists, it throws `InvalidOperationException` (line 137-140):

> "ShowDialog requires an owner window: no ownerViewModel was provided and no active window could be inferred. Provide a ViewModel whose View is a shown Window, or call ShowDialog while a window is active."

### ShowDialog with explicit owner

```csharp
// If you have a reference to the owner's ViewModel:
await _windowManager.ShowDialog<bool>(dialogVm, this); // 'this' is IViewAware

// Internally: ownerViewModel?.View as Window sets the dialog's owner
// If ownerViewModel is null or has no Window.View, owner is omitted
```

### ShowMessageBox with all options

```csharp
var result = await _windowManager.ShowMessageBox<MessageBoxResult>(
    messageBoxText: "Do you want to save before closing?",
    caption: "Confirm",                      // null → empty string title (SP-5)
    buttons: MessageBoxButton.YesNoCancel,
    icon: MessageBoxImage.Question,
    defaultResult: MessageBoxResult.Yes,
    cancelResult: MessageBoxResult.Cancel
);
```

`MessageBoxViewModel` extends `Screen` and calls `RequestClose` when a button is clicked (line 207-211). `ClickedButton` is set before requesting close, and `RequestClose(true)` is called for `OK` and `Yes`, `RequestClose(false)` for `Cancel` and `No`.

### Automatic DisplayName → Window.Title binding

`WindowManager.CreateWindow` (line 193-201) automatically wires `IHaveDisplayName.DisplayName` to the window's `Title` property if the window title is still the type name or empty:

```csharp
if (viewModel is IHaveDisplayName haveDisplayName &&
    (string.IsNullOrEmpty(window.Title) || window.Title == view.GetType().Name))
{
    var binding = new Binding(nameof(IHaveDisplayName.DisplayName))
    {
        Source = haveDisplayName,
        Mode = BindingMode.TwoWay
    };
    window.Bind(Window.TitleProperty, binding);
}
```

All `Screen` subclasses implement `IHaveDisplayName` via `DisplayName` property, so window titles update automatically when the view model's `DisplayName` changes.

### WindowConductor lifecycle management

`WindowConductor` (`src/Sonata.Avalonia/Internal/WindowConductor.cs`) orchestrates the window–viewmodel relationship:

- **Activation on minimize/restore:** `WindowStateChanged` handler (line 85-100) calls `TryActivateAsync` on maximize/normal and `TryDeactivateAsync` on minimize
- **Close guard:** `IGuardClose.CanCloseAsync` is called before closing; if it returns false, the close is cancelled
- **Fire-and-forget:** All lifecycle calls use `FireAndForget.Run` to avoid async void — exceptions are logged, not swallowed

## See also

- [`src/Sonata.Avalonia/WindowManager.cs`](../../src/Sonata.Avalonia/WindowManager.cs) — `ShowDialog` with owner inference
- [`src/Sonata.Avalonia/Primitive/MessageBoxViewModel.cs`](../../src/Sonata.Avalonia/Primitive/MessageBoxViewModel.cs) — `Setup` method and button mapping
- [`src/Sonata.Avalonia/Internal/WindowConductor.cs`](../../src/Sonata.Avalonia/Internal/WindowConductor.cs) — activation, state changes, close guard
- [`samples/Sonata.Samples.HelloDialog/ShellViewModel.cs`](../../samples/Sonata.Samples.HelloDialog/ShellViewModel.cs) — `ShowDialog<bool>` with factory
- [Screen Lifecycle](./screen-lifecycle.md) — `Screen`, `IGuardClose`, `IScreenState`
