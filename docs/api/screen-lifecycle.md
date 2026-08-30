# Screen Lifecycle

Manages activation, deactivation, and closure of screens and dialogue ViewModels.

## Key types

| Type | Role |
|------|------|
| `Screen` | Base class for ViewModels; implements all lifecycle interfaces |
| `IScreen` | Composes `IViewAware` + `IHaveDisplayName` + `IScreenState` + `IChild` + `IGuardClose` + `IRequestClose` |
| `ScreenState` | Enum: `Active`, `Deactivated`, `Closed` (and obsolete `Initial`) |
| `IScreenState` | Exposes `ActivateAsync`/`DeactivateAsync`/`CloseAsync` + events |
| `IChild` / `Parent` | Links a screen to its conductor; used by `RequestClose` |
| `IGuardClose` | Override `CanCloseAsync` to veto closure |
| `IRequestClose` | Call `RequestClose(dialogResult?)` to close from ViewModel side |
| `IViewAware` | `AttachView`/`View`; `OnViewLoaded` hook for post-binding setup |
| `IHaveDisplayName` | `DisplayName` bound to window title or tab header |

### Event args types

- `ActivationEventArgs` — `IsInitialActivate` (bool) + `PreviousState`
- `DeactivationEventArgs` — `PreviousState`
- `CloseEventArgs` — `PreviousState`
- `ScreenStateChangedEventArgs` — `NewState` + `PreviousState`

## ScreenState transitions

```
Deactivated ──ActivateAsync()──> Active
     ^                              │
     │                              │
  DeactivateAsync()         CloseAsync()
     │                              │
     │                              v
     └─────────── Closed <──────────┘
```

- Transitions are **serialised** via `SemaphoreSlim` in `ConductorBase.ExecuteTransitionAsync`
- Attempting a synchronous re-entrant call throws `InvalidOperationException`
- `DisposeAsync()` closes the screen if still open; idempotent

## Use cases

### Async lifecycle hooks with CancellationToken

Override the protected virtual methods to run async logic on state transitions. All accept a `CancellationToken`.

```csharp
public class MyViewModel : Screen
{
    protected override Task OnInitialActivateAsync(CancellationToken ct)
    {
        // Fires once, the very first time the screen is activated
        return LoadDataAsync(ct);
    }

    protected override Task OnActivateAsync(CancellationToken ct)
    {
        // Fires every time the screen transitions to Active
        return RefreshAsync(ct);
    }

    protected override Task OnDeactivateAsync(CancellationToken ct)
    {
        // Fires every time the screen leaves the Active state
        return SaveStateAsync(ct);
    }

    protected override Task OnCloseAsync(CancellationToken ct)
    {
        // Fires when the screen transitions to Closed
        return ReleaseResourcesAsync(ct);
    }

    protected override Task OnStateChangedAsync(ScreenState previousState, ScreenState newState, CancellationToken ct)
    {
        // Fires on any transition — useful for logging or cross-cutting concerns
        Logger.LogInformation("Screen {Name} transitioned {From} → {To}", DisplayName, previousState, newState);
        return Task.CompletedTask;
    }
}
```

`src/Sonata.Avalonia/Screen.cs` lines 97–119 define these hooks. CancellationToken propagation follows SP-2 semantics.

### Closing a dialogue with a result

```csharp
public class Dialog1ViewModel : Screen
{
    public Dialog1ViewModel()
    {
        DisplayName = "I'm Dialog 1";
    }

    public void Close() => RequestClose(null);
    public void Save() => RequestClose(true); // dialogResult = true
}
```

`RequestClose(bool?)` delegates to the conductor via `IChildDelegate.CloseItemAsync`. If the screen has no conductor parent (e.g., not shown via `WindowManager`), it throws `InvalidOperationException`.

`samples/Sonata.Samples.HelloDialog/Dialog1ViewModel.cs` is the live example.

### Guarding close with CanCloseAsync

```csharp
public class DocumentEditorViewModel : Screen
{
    private bool _hasUnsavedChanges;

    public override Task<bool> CanCloseAsync(CancellationToken ct = default)
    {
        if (_hasUnsavedChanges)
        {
            // Could show a confirmation dialog here via IWindowManager
            Logger.LogInformation("Close blocked by unsaved changes");
            return Task.FromResult(false);
        }
        return Task.FromResult(true);
    }
}
```

`Screen.CanCloseAsync` (line 275 in `src/Sonata.Avalonia/Screen.cs`) returns `Task.FromResult(true)` by default.

### DisplayName bound to window title

`IHaveDisplayName.DisplayName` is bound automatically by `WindowManager` when showing a window or dialog. Set it in the constructor or mutate it at runtime:

```csharp
public ShellViewModel()
{
    DisplayName = "Master-Detail";
}
```

## Semantics notes

- **Transition serialisation:** `ConductorBase.ExecuteTransitionAsync` holds a `SemaphoreSlim(1,1)`. A synchronous re-entrant call (e.g., calling `ActivateItemAsync` from inside `OnActivateAsync`) throws `InvalidOperationException`. SP-2.
- **`IViewAware.OnViewLoaded`:** fires once when the attached view's `Loaded` event fires. After that the handler is self-unsubscribed (one-shot).
- **`DisposeAsync`:** idempotent — closing an already-closed screen is a no-op.

## See also

- [`src/Sonata.Avalonia/Screen.cs`](../../src/Sonata.Avalonia/Screen.cs) — `Screen` implementation
- [`src/Sonata.Avalonia/IScreen.cs`](../../src/Sonata.Avalonia/IScreen.cs) — all lifecycle interfaces and event args
- [`src/Sonata.Avalonia/Internal/WindowConductor.cs`](../../src/Sonata.Avalonia/Internal/WindowConductor.cs) — how `RequestClose` delegates to the window
- [`samples/Sonata.Samples.HelloDialog/Dialog1ViewModel.cs`](../../samples/Sonata.Samples.HelloDialog/Dialog1ViewModel.cs) — `RequestClose` usage
- [Conductors](./conductors.md) — parent containers that manage screen lifecycle
- [Window manager](./window-manager.md) — `IWindowManager.ShowDialogAsync` for dialogue hosting
