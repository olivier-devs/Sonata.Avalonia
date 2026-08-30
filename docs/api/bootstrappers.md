# Bootstrappers

Handles application startup, IoC container configuration, and convention-based service registration.

## Key types

| Type | Role | Package |
|------|------|---------|
| `SonataApplication<T>` | MS.DI-based bootstrapper; extend `App : SonataApplication<T>` | `Sonata.Avalonia` |
| `SonataApplicationBase<T>` | Abstract base; implement this if using a custom IoC | `Sonata.Avalonia` |
| `SonataServiceCollectionExtensions.AddSonata` | Registers all Sonata defaults (TryAdd semantics) | `Sonata.Avalonia` |
| `StyletApplication<T>` | StyletIoC bootstrapper; use when you prefer that container | `Sonata.Avalonia.StyletIoC` |
| `SonataHostedApplication<T>` | Hosts a `Microsoft.Extensions.Hosting` `IHost` alongside the UI | `Sonata.Avalonia.Hosting` |

## Startup sequence

```
App.Initialize()
  └─ base.Initialize()
       ├─ IoC.GetInstance = GetInstance       // set static delegate
       ├─ Configure()                          // virtual: ConfigureServices → ConfigureSonataServices → BuildServiceProvider
       ├─ SonataLogManager.SetFactory(...)
       └─ UiThreadDispatch.Dispatcher = ...

OnFrameworkInitializationCompleted()
  ├─ OnStart()
  ├─ vm = IoC.Get<T>()                        // resolve root ViewModel
  ├─ view = IViewManager.CreateAndBindViewForModelIfNecessary(vm)
  ├─ desktop.MainWindow = view as Window
  └─ OnFrameworkInitialized()                   // fire IApplicationLifetimeParticipant
```

## Use cases

### Minimal MS.DI application

`SonataApplication<T>` is the recommended bootstrapper for new projects using Microsoft.Extensions.DependencyInjection.

```csharp
// App.axaml.cs
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using Sonata.Avalonia;

public partial class App : SonataApplication<ShellViewModel>
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
        base.Initialize(); // required — builds the service provider
    }

    protected override void ConfigureServices(IServiceCollection services)
    {
        // Register application services
        services.AddTransient<Dialog1ViewModel>();
        services.AddTransient<Func<Dialog1ViewModel>>(
            sp => () => sp.GetRequiredService<Dialog1ViewModel>());
    }
}
```

`samples/Sonata.Samples.MSIoC/App.axaml.cs` and `samples/Sonata.Samples.HelloDialog/App.axaml.cs` follow this pattern.

### Replacing a default service

All Sonata defaults are registered with `TryAdd` semantics. User registrations in `ConfigureServices` take precedence:

```csharp
// App.axaml.cs
protected override void ConfigureServices(IServiceCollection services)
{
    // Replace IViewManager with our custom implementation.
    // Sonata's AddSonata registers IViewManager with TryAdd,
    // so this singleton registration wins.
    services.AddSingleton<IViewManager, CustomViewManager>();
}
```

See `samples/Sonata.Samples.OverridingViewManager/App.axaml.cs`.

### Disabling convention registration

```csharp
protected override bool EnableConventionRegistration => false;
```

When disabled, Views and ViewModels are **not** auto-registered. You must register every service explicitly in `ConfigureServices`.

### Custom ViewAssemblies

```csharp
// Scan additional assemblies for Views and ViewModels
protected override Assembly[] ViewAssemblies => new[]
{
    GetType().Assembly,
    typeof(SomeLibrary.Views.MyView).Assembly,
};
```

Defaults to `{ GetType().Assembly }` — the assembly containing your `App` class.

### Convention registration behaviour

When `EnableConventionRegistration = true` (default), `AddSonata` scans `ViewAssemblies` and registers:

| Pattern | Lifetime | Example |
|---------|----------|---------|
| `Control`-derived type | Transient | `MainWindow` → `services.TryAddTransient<MainWindow>()` |
| Type name ending with `"ViewModel"` | Singleton | `ShellViewModel` → `services.TryAddSingleton<ShellViewModel>()` |

Registration uses `TryAdd`, so explicit registrations always win.

## See also

- [`samples/Sonata.Samples.MSIoC/App.axaml.cs`](../../samples/Sonata.Samples.MSIoC/App.axaml.cs) — MS.DI bootstrapper
- [`samples/Sonata.Samples.HelloDialog/App.axaml.cs`](../../samples/Sonata.Samples.HelloDialog/App.axaml.cs) — service registration with `Func<T>`
- [`samples/Sonata.Samples.OverridingViewManager/App.axaml.cs`](../../samples/Sonata.Samples.OverridingViewManager/App.axaml.cs) — overriding defaults
- [`src/Sonata.Avalonia/SonataApplication.cs`](../../src/Sonata.Avalonia/SonataApplication.cs) — `SonataApplication<T>` source
- [`src/Sonata.Avalonia/SonataServiceCollectionExtensions.cs`](../../src/Sonata.Avalonia/SonataServiceCollectionExtensions.cs) — `AddSonata` source
- [StyletIoC bootstrapper](./bootstrappers.md) — `StyletApplication<T>` when not using MS.DI
- [Hosting bootstrapper](./bootstrappers.md) — `SonataHostedApplication<T>` for background services
