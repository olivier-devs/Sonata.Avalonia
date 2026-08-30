# Dispatching

Abstraction for dispatching work to the UI thread, with `IDispatcher` interface and implementations for application and testing.

> **SP-3 deprecation:** `Execute` facade (`Execute.PostToUIThread`, `Execute.OnUIThread`) is obsolete. Inject `IDispatcher` instead.

## Key types

| Type | Role | Package |
|------|------|---------|
| `IDispatcher` | Interface with `Post` (async), `Send` (sync), `IsCurrent` | `Sonata.Avalonia` |
| `ApplicationDispatcher` | Dispatches via Avalonia `Dispatcher` | `Sonata.Avalonia` |
| `SynchronousDispatcher` | Synchronous dispatch — for unit tests | `Sonata.Avalonia` |
| `UiThreadDispatch` | Internal ambient dispatcher used by framework base classes | `Sonata.Avalonia.Internal` |

## Use cases

### Inject IDispatcher and post to UI thread

```csharp
public class OrderService
{
    private readonly IDispatcher _dispatcher;
    private readonly IWindowManager _windowManager;

    public OrderService(IDispatcher dispatcher, IWindowManager windowManager)
    {
        _dispatcher = dispatcher;
        _windowManager = windowManager;
    }

    public async Task PlaceOrderAsync(Order order)
    {
        await _repository.SaveAsync(order);

        // Post result back to UI thread
        _dispatcher.Post(() =>
        {
            await _windowManager.ShowMessageBox<MessageBoxResult>(
                "Order placed successfully", "Success");
        });
    }

    public bool IsOnUIThread => _dispatcher.IsCurrent;
}
```

`IDispatcher` is registered by `SonataServiceCollectionExtensions.AddSonata` as a singleton resolving to `ApplicationDispatcher`.

### Send (synchronous wait) vs Post (fire-and-forget)

```csharp
// Post — async, returns immediately
_dispatcher.Post(() => UpdateUI());

// Send — blocks until action completes
_dispatcher.Send(() => UpdateUI());

// IsCurrent — check before doing work
if (_dispatcher.IsCurrent)
    UpdateUI(); // already on UI thread
else
    _dispatcher.Send(() => UpdateUI()); // must dispatch
```

`ApplicationDispatcher.Send` uses `dispatcher.InvokeAsync` (line 61 in `IDispatcher.cs`) which returns a `Task` — the `Send` method is synchronous but internally awaits the `Task`.

### SynchronousDispatcher for unit tests

```csharp
// In tests, replace with synchronous dispatcher
var services = new ServiceCollection();
services.AddSingleton<IDispatcher>(SynchronousDispatcher.Instance);
```

`SynchronousDispatcher` (line 75-99 in `IDispatcher.cs`) executes `Post` and `Send` immediately on the calling thread and `IsCurrent` always returns `true`. This makes tests deterministic without needing to pump the UI thread.

### Migration from Execute facade (SP-3)

Old (deprecated):
```csharp
// Deprecated in SP-3
Execute.PostToUIThread(() => DoWork());
Execute.OnUIThread(() => DoWork());
var dispatcher = Execute.Dispatcher; // also deprecated
```

New:
```csharp
// Inject IDispatcher
public MyService(IDispatcher dispatcher)
{
    _dispatcher = dispatcher;
}

// Post to UI thread asynchronously
_dispatcher.Post(() => DoWork());

// Or send synchronously
_dispatcher.Send(() => DoWork());
```

The framework internally uses `UiThreadDispatch` (`src/Sonata.Avalonia/Internal/UiThreadDispatch.cs`) which holds a static `IDispatcher` set by the bootstrapper. Framework types (`Screen`, `PropertyChangedBase`, `BindableCollection`) cannot use constructor injection, so they use this ambient dispatcher instead.

## See also

- [`src/Sonata.Avalonia/IDispatcher.cs`](../../src/Sonata.Avalonia/IDispatcher.cs) — `IDispatcher`, `ApplicationDispatcher`, `SynchronousDispatcher`
- [`src/Sonata.Avalonia/Execute.cs`](../../src/Sonata.Avalonia/Execute.cs) — deprecated facade with `[Obsolete]` attributes
- [`src/Sonata.Avalonia/Internal/UiThreadDispatch.cs`](../../src/Sonata.Avalonia/Internal/UiThreadDispatch.cs) — ambient dispatcher used by framework
- [`tests/Sonata.Avalonia.Tests/TestFakes.cs`](../../tests/Sonata.Avalonia.Tests/TestFakes.cs) — `RecordingDispatcher` for test verification
