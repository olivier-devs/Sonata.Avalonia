# Conductors

Parent containers that manage the lifecycle of child screens.

## Key types

| Type | Role |
|------|------|
| `ConductorBase<T>` | Abstract base; owns `ExecuteTransitionAsync` (serialised transitions via `SemaphoreSlim`) |
| `ConductorBaseWithActiveItem<T>` | Adds `ActiveItem` property and `ChangeActiveItemAsync` |
| `Conductor<T>` | Single-item conductor; replacing `ActiveItem` closes the previous item |
| `Conductor<T>.Collection.OneActive` | Many items, only one active; others are deactivated but kept in `Items` |
| `Conductor<T>.Collection.AllActive` | Many items, all active simultaneously |
| `Conductor<T>.StackNavigation` | Single active item with a history stack (`GoBackAsync`) |
| `IConductor<T>` | Interface: `ActivateItemAsync`, `DeactivateItemAsync`, `CloseItemAsync`, `DisposeChildren` |
| `IHaveActiveItem<T>` | `ActiveItem { get; set; }` |
| `IParent<T>` | `GetChildren()` — returns items owned by the conductor |
| `IChildDelegate` | `CloseItemAsync(object? item, bool? dialogResult, CancellationToken)` — child initiates close |

## Transition serialisation

All conductors serialise their state transitions through `ConductorBase.ExecuteTransitionAsync` (SP-2). A synchronous re-entrant call (e.g., calling `ActivateItemAsync` from inside an `On*Async` hook of the same conductor) throws `InvalidOperationException`.

`ActiveItemTransition` exposes the in-flight transition `Task`.

## Use cases

### Single-item conductor with ActivateItemAsync

```csharp
// samples/Sonata.Samples.NavigationController/Pages/ShellViewModel.cs
public class ShellViewModel : Conductor<IScreen>, INavigationControllerDelegate
{
    public HeaderViewModel HeaderViewModel { get; }

    public ShellViewModel(HeaderViewModel headerViewModel)
    {
        HeaderViewModel = headerViewModel ?? throw new ArgumentNullException(nameof(headerViewModel));
    }

    public void NavigateTo(IScreen screen) => _ = ActivateItemAsync(screen);
}
```

`Conductor<T>.ActivateItemAsync` checks `CanCloseItem` on the previous `ActiveItem` before replacing it. See `src/Sonata.Avalonia/Conductor.cs` line 25.

### Conductor.Collection.OneActive for tabs

```csharp
// samples/Sonata.Samples.TabNavigation/ShellViewModel.cs
public class ShellViewModel : Conductor<IScreen>.Collection.OneActive
{
    public ShellViewModel(Page1ViewModel page1, Page2ViewModel page2)
    {
        Items.Add(page1);
        Items.Add(page2);
        ActiveItem = page1;
    }
}
```

`OneActive` keeps all items in `Items`; only one is `ActiveItem`. When `ActiveItem` is removed from `Items`, the conductor automatically selects the next item to activate. `INotifyCollectionChanging` is used internally to handle `Reset` operations gracefully.

See `src/Sonata.Avalonia/ConductorOneActive.cs` lines 10–213.

### Closing a child via IChildDelegate

A child ViewModel calls `RequestClose(dialogResult)`, which delegates to its `Parent` conductor via `IChildDelegate.CloseItemAsync`. `WindowConductor` implements `IChildDelegate` to bridge ViewModels shown in windows:

```csharp
// src/Sonata.Avalonia/Internal/WindowConductor.cs lines 163–204
async Task IChildDelegate.CloseItemAsync(object? item, bool? dialogResult, CancellationToken ct)
{
    if (item != _viewModel) return;

    if (_viewModel is IGuardClose guardClose && !await guardClose.CanCloseAsync(ct))
        return; // close blocked

    UnsubscribeFromWindowEvents();
    await ScreenExtensions.TryCloseAsync(_viewModel, ct);
    _window.Close(dialogResult);
}
```

`Screen.RequestClose` (line 288 in `src/Sonata.Avalonia/Screen.cs`) fires `IChildDelegate.CloseItemAsync` via `FireAndForget.Run`.

### ActiveItem setter semantics

The `ActiveItem` setter on `ConductorBaseWithActiveItem` is fire-and-forget — it invokes `FireAndForget.Run(ActivateItemAsync(value), logger)`. Exceptions during activation are logged but not thrown:

```csharp
// src/Sonata.Avalonia/ConductorBaseWithActiveItem.cs line 18
public T? ActiveItem
{
    get => _activeItem;
    set => FireAndForget.Run(ActivateItemAsync(value), SonataLogManager.GetLogger(GetType()));
}
```

If you need to await activation, call `ActivateItemAsync` directly instead of using the setter.

## See also

- [`src/Sonata.Avalonia/ConductorBase.cs`](../../src/Sonata.Avalonia/ConductorBase.cs) — base class, `ExecuteTransitionAsync`, transition serialisation
- [`src/Sonata.Avalonia/ConductorBaseWithActiveItem.cs`](../../src/Sonata.Avalonia/ConductorBaseWithActiveItem.cs) — `ActiveItem` property, `ChangeActiveItemAsync`
- [`src/Sonata.Avalonia/Conductor.cs`](../../src/Sonata.Avalonia/Conductor.cs) — single-item conductor
- [`src/Sonata.Avalonia/ConductorOneActive.cs`](../../src/Sonata.Avalonia/ConductorOneActive.cs) — `OneActive` (tabs)
- [`src/Sonata.Avalonia/ConductorAllActive.cs`](../../src/Sonata.Avalonia/ConductorAllActive.cs) — `AllActive`
- [`src/Sonata.Avalonia/ConductorNavigating.cs`](../../src/Sonata.Avalonia/ConductorNavigating.cs) — `StackNavigation`
- [`src/Sonata.Avalonia/IConductor.cs`](../../src/Sonata.Avalonia/IConductor.cs) — interfaces
- [`src/Sonata.Avalonia/Internal/WindowConductor.cs`](../../src/Sonata.Avalonia/Internal/WindowConductor.cs) — `IChildDelegate` for window-hosts
- [`samples/Sonata.Samples.NavigationController/Pages/ShellViewModel.cs`](../../samples/Sonata.Samples.NavigationController/Pages/ShellViewModel.cs) — `Conductor<IScreen>`
- [`samples/Sonata.Samples.TabNavigation/ShellViewModel.cs`](../../samples/Sonata.Samples.TabNavigation/ShellViewModel.cs) — `OneActive` tabs
- [Screen lifecycle](./screen-lifecycle.md) — `Screen` hooks and `RequestClose` mechanics
