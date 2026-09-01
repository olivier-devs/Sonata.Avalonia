# Migration Guide

**Stylet.Avalonia 0.5.1 → Sonata.Avalonia 1.0.0**

This guide covers every breaking change introduced during the SP-1 → SP-5 modernization effort.
The vast majority of migrations are mechanical (namespace find/replace) with a small number of API-level adaptations.

---

## Overview

The project has been split into three packages:

| Old | New | Notes |
|-----|-----|-------|
| `Stylet.Avalonia` (single package) | `Sonata.Avalonia` | Core framework |
| *(bundled)* | `Sonata.Avalonia.StyletIoC` | StyletIoC container, for minimal migration |
| *(absent)* | `Sonata.Avalonia.Hosting` | Generic Host integration |

---

## Namespace changes (mechanical)

### Primary namespace

```diff
- using Stylet.Avalonia;
+ using Sonata.Avalonia;
```

A global find/replace of `Stylet.Avalonia` → `Sonata.Avalonia` handles the majority of migration.

### Type renames

| Old name | New name |
|----------|----------|
| `StyletApplicationBase` | `SonataApplicationBase` |
| `StyletViewLocationException` | `SonataViewLocationException` |
| `StyletInvalidViewTypeException` | `SonataInvalidViewTypeException` |
| `StyletConductorExtensions` | `SonataConductorExtensions` |

All are covered by the same find/replace on the prefix.

### XAML prefix

```xml
<!-- Unchanged — the `s:` prefix is preserved -->
xmlns:s="using:Sonata.Avalonia"
```

The only XAML change is the URL in any documentation comments, which becomes
`https://github.com/sonata-avalonia/sonata.avalonia`.

### NuGet package reference

```diff
- <PackageReference Include="Stylet.Avalonia" Version="0.5.1" />
+ <PackageReference Include="Sonata.Avalonia" Version="1.0.0" />
```

If you were using the StyletIoC container, also add:

```xml
<PackageReference Include="Sonata.Avalonia.StyletIoC" Version="1.0.0" />
```

---

## Breaking changes by sub-project

### SP-1 — Foundations

| Before | After | Migration |
|--------|-------|-----------|
| `Stylet.Avalonia.Logging.ILogger`, `LogManager`, `TraceLogger`, `NullLogger` | Removed | Inject `ILogger<T>` from Microsoft.Extensions.Logging |
| `IoC` (static service locator) | `internal` | Use constructor injection; only the XAML `View.Model` bridge retains a static delegate |
| Single `Stylet.Avalonia` package | Three packages | Add `Sonata.Avalonia.StyletIoC` if using StyletIoC; `Sonata.Avalonia.Hosting` if using Generic Host |


---

### SP-2 — Async lifecycle

| Before | After | Migration |
|--------|-------|-----------|
| `void Activate()` / `Deactivate()` / `Close()` | `Task ActivateAsync(ct)` / `DeactivateAsync(ct)` / `CloseAsync(ct)` | Add `Async` suffix + `return Task.CompletedTask`; add `CancellationToken ct = default` parameter |
| `protected override void OnActivate()` | `protected override Task OnActivateAsync(CancellationToken ct)` | Same — mechanical rename + return statement |
| `Task<bool> CanCloseAsync()` | `Task<bool> CanCloseAsync(CancellationToken ct)` | Add `ct` parameter |
| `void ActivateItem(T)` / `CloseItem(T)` | `Task ActivateItemAsync(T, ct)` / `CloseItemAsync(T, ct)` | Await the returned task |
| `void ChangeActiveItem(T, bool)` | `Task ChangeActiveItemAsync(T, bool, ct)` | Await the returned task |
| `bool Validate()` / `ValidateProperty()` | `Task<bool> ValidateAsync(ct)` / `ValidatePropertyAsync(ct)` | `await ValidateAsync()` — no more `.Result` blocking |
| `TryActivate` / `TryDeactivate` / `TryClose` | `TryActivateAsync` / `TryDeactivateAsync` / `TryCloseAsync` | Await the returned task |
| `GoBack()` / `Clear()` (StackNavigation) | `GoBackAsync()` / `ClearAsync()` | Await the returned task |
| `[Obsolete]` members (`State`, `CanClose()`, `TryClose()`) | Removed | Already migrated away in Stylet — remove any remaining usages |


---

### SP-3 — UI layer

| Before | After | Migration |
|--------|-------|-----------|
| `Execute.OnUIThread(...)` | `[Obsolete]` — still works | Inject `IDispatcher` and use it; the facade remains functional during the deprecation period |
| `Execute.OnUIThreadSync` / `PostToUIThreadAsync` / `OnUIThreadAsync` | **Removed** | Use `IDispatcher.Post` + `await` if needed |
| `Execute.DefaultPropertyChangedDispatcher` | **Removed** | `PropertyChangedBase` always dispatches via Post |
| `PropertyChangedBase.PropertyChangedDispatcher` | **Removed** | Notifications always Post to UI thread (non-blocking) |
| `INotifyPropertyChangedDispatcher` | **Removed** | `PropertyChangedBase` no longer implements this |
| `ViewManager.InitializeView` (virtual no-op) | **Removed** | Avalonia views self-initialize via `InitializeComponent()` in constructors |
| `ViewManagerConfig { get; set; }` | `required init` properties | Config is passed at registration; no mutation after creation |
| `WindowManager` constructor (3 params) | `+ Func<IMessageBoxViewModel>` (4 params) | MessageBox factory now injected — `ShowMessageBox` no longer uses `IoC.Get` internally |

**Note on `PropertyChanged` dispatch:** Previously, `OnPropertyChanged` used a blocking `Dispatcher.UIThread.Invoke`.
It now uses a non-blocking `Post`. Handlers that depended on the calling thread being blocked
by the UI dispatch need to be reviewed — the notification order (FIFO) is preserved.


---

### SP-4 — Consolidation

| Before | After | Migration |
|--------|-------|-----------|
| `ActionBase.InvokeTargetMethod`: async action exceptions **swallowed silently** | Exceptions **logged** via `ILogger` | No API change — behavior improved; any handler relying on silent swallowing will now produce error logs |
| `CommandAction.UpdateCanExecute`: `Dispatcher.UIThread.Invoke` (blocking) | `UiThreadDispatch.OnUIThread` (non-blocking Post if off the UI thread) | Same — no API change; now non-blocking when called from a background thread |
| `EventAggregator.PublishOnUIThread`: `Dispatcher.UIThread.Invoke` | `UiThreadDispatch.OnUIThread` | Same behavior on UI thread; off-UI-thread calls are now non-blocking |
| `RethinkingBinding`: `Dispatcher.UIThread.Invoke` | `UiThreadDispatch.OnUIThread` | Same; constructors cleaned of dead WPF code |


---

### SP-5 — Release

| Before | After | Migration |
|--------|-------|-----------|
| MessageBox with no caption: title falls back to `"提示"` | Title falls back to **empty string** | Any UI test asserting `"提示"` as the default caption needs updating |
| `ApplicationDispatcher.Send` | Fixed to be **truly synchronous** | This was a bug fix — the behavior change only affects code that was relying on the incorrect async-over-sync implementation |


---

## Bootstrapper migration

### Before (Stylet.Avalonia)

```csharp
// App.axaml.cs — StyletApplication with ConfigureIoC
using Avalonia.Markup.Xaml;
using Stylet.Avalonia;
using Stylet.Avalonia.StyletIoC;

namespace Stylet.Samples.HelloDialog;

public partial class App : StyletApplication<ShellViewModel>
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
        base.Initialize();
    }

    protected override void ConfigureIoC(IStyletIoCBuilder builder)
    {
        base.ConfigureIoC(builder);
        builder.Bind<IDialogFactory>().ToAbstractFactory();
    }
}
```

### After (Sonata.Avalonia)

```csharp
// App.axaml.cs — SonataApplication with ConfigureServices
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using Sonata.Avalonia;

namespace Sonata.Samples.HelloDialog;

public partial class App : SonataApplication<ShellViewModel>
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
        base.Initialize(); // required — builds the service provider
    }

    protected override void ConfigureServices(IServiceCollection services)
    {
        services.AddTransient<Dialog1ViewModel>();
        services.AddTransient<Func<Dialog1ViewModel>>(
            sp => () => sp.GetRequiredService<Dialog1ViewModel>());
    }
}
```

> **Minimal migration path:** If you prefer to keep the StyletIoC container and
> `ConfigureIoC` style, use `StyletApplication<T>` from `Sonata.Avalonia.StyletIoC`
> (see `samples/Sonata.Samples.Hello`). The namespace stays `Sonata.Avalonia.StyletIoC`
> but the class name is unchanged, minimizing diff.

**Source:** `samples/Sonata.Samples.HelloDialog/App.axaml.cs` (after);
`/mnt/datas/dev/pro/Stylet.Avalonia/Samples/Stylet.Samples.HelloDialog/App.axaml.cs` (before)

---

## Logging migration

### Before (Stylet.Avalonia)

```csharp
// Using Stylet.Avalonia.Logging
using Stylet.Avalonia.Logging;

var logger = LogManager.GetLogger<MyService>();
logger.Info("Service initialized");
logger.Error(ex, "Something went wrong");
```

### After (Sonata.Avalonia)

```csharp
// Using Microsoft.Extensions.Logging — inject ILogger<T>
public class MyService
{
    private readonly ILogger<MyService> _logger;

    public MyService(ILogger<MyService> logger)
    {
        _logger = logger;
    }

    public void DoSomething()
    {
        _logger.LogInformation("Service initialized");
        _logger.LogError(ex, "Something went wrong");
    }
}
```

The framework itself (`ViewManager`, `WindowManager`, `EventAggregator`, etc.) now uses
`ILogger<T>` internally. A no-op logger factory is registered by default — add an
`ILoggerProvider` via `ConfigureServices` to capture framework logs.


---

## Validation migration

### Before (Stylet.Avalonia)

```csharp
// Sync-over-async — risk of deadlock
public void Save()
{
    if (Validate())  // .Result blocking call
    {
        // ...
    }
}

public void ValidateProperty(string propertyName)
{
    ValidateProperty(propertyName);  // .Result — blocking
}
```

### After (Sonata.Avalonia)

```csharp
// Fully async — no blocking
public async Task SaveAsync()
{
    if (await ValidateAsync())  // properly awaited
    {
        // ...
    }
}

public async Task ValidatePropertyAsync(string propertyName, CancellationToken ct = default)
{
    await ValidatePropertyAsync(propertyName, ct);  // properly awaited
}
```

`Validate()` and `ValidateProperty()` (sync facades) have been removed.
`ValidateAsync(ct)` and `ValidatePropertyAsync(propertyName, ct)` are the only entry points.


---

## ViewManager configuration migration

### Before (Sonata.Avalonia 1.0.0)

```csharp
// Mutating the ViewManager after registration
services.AddSonata(windowManagerConfig, new[] { typeof(App).Assembly });

// Later, resolving and mutating the registered instance
var manager = provider.GetRequiredService<IViewManager>() as ViewManager;
manager!.ViewAssemblies = new[] { typeof(App).Assembly };
manager.NamespaceTransformations = new Dictionary<string, string>
{
    ["MyApp.ViewModels"] = "MyApp.Views",
};
manager.ViewNameSuffix = "Screen";
```

### After (centralized configuration)

```csharp
// All configuration up-front through the options pattern
services.ConfigureViewManager(options =>
{
    options
        .AddViewAssembly<App>()
        .MapNamespace("MyApp.ViewModels", "MyApp.Views")
        .SetViewNameSuffix("Screen")
        .AddView<CustomEditorControl, LegacyEditorViewModel>(); // explicit mapping
});

services.AddSonata(windowManagerConfig, new[] { typeof(App).Assembly });
```

> **Note:** `ConfigureViewManager` and `AddSonata` can be called in any order.
> User configuration registered via `ConfigureViewManager` always takes precedence —
> `AddSonata` uses `PostConfigure`, which runs after the user's configuration.

### Key changes

| Before | After |
|--------|-------|
| `new ViewManagerConfig { ViewFactory = ..., ViewAssemblies = ... }` | `ConfigureViewManager(options => { options.SetViewFactory(...).AddViewAssembly<...>(); })` |
| `NamespaceTransformations = new Dictionary<...>` | `MapNamespace("FromNs", "ToNs")` |
| `ViewManager` properties were public and mutable | Configuration lives in `ViewManagerConfig`; `ViewManager` exposes read-only accessors |
| Explicit mappings required a custom `ViewManager` subclass | `AddView<TView, TViewModel>()` handles this declaratively |
| Convention-only resolution | Explicit mappings (`AddView`) take priority over convention-based discovery |

### ViewFactory is configured automatically

`AddSonata` automatically sets `ViewFactory` via `PostConfigure` if it is still `null`.
This means for most applications, `SetViewFactory` is only needed when replacing the default
service-resolution behaviour with a custom factory (e.g., XAML loading):

```csharp
services.ConfigureViewManager(options =>
{
    options.SetViewFactory(type => AvaloniaXamlLoader.Load(type));
});
```

---

## See also

- [API Reference](docs/api/README.md) — full API documentation by domain
- [CHANGELOG](CHANGELOG.md) — version history
