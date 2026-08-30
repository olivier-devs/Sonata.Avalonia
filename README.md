# Sonata.Avalonia

A lightweight, powerful ViewModel-first MVVM framework for [Avalonia UI](https://avaloniaui.net/),
modernized from [Stylet.Avalonia](https://github.com/sealoyal2018/Stylet.Avalonia) (itself a port of
[Stylet](https://github.com/canton7/Stylet)) for .NET 8/9/10 and Avalonia 12.

**Status: 1.0.0 — stable.**

## Features

- **Screen/Conductor lifecycle (async)** — `Screen` with `OnActivateAsync`/`OnDeactivateAsync`/`OnCloseAsync`,
  `Conductor<T>` with serialized transitions, `IGuardClose` for close guards
- **Convention-based View location** — `IViewManager` resolves ViewModels to Views by naming convention,
  fully customizable via `ViewManagerConfig`
- **WindowManager** — `ShowWindow`, `ShowDialog<T>`, `ShowMessageBox` with owner resolution,
  `WindowConductor` lifecycle, and `MessageBoxViewModel`
- **XAML actions** — `{s:Action}` markup extension wires XAML events to ViewModel methods,
  supports async actions with logged exceptions
- **Async validation** — `ValidatingModelBase` with `INotifyDataErrorInfo`, `ValidatePropertyAsync`,
  `ValidateAllPropertiesAsync`; no sync-over-async `.Result` blocking
- **EventAggregator** — weak-reference subscriptions, channel-based publication, `PublishOnUIThread`
- **Microsoft.Extensions integration** — `SonataApplication<T>` uses `IServiceCollection` for DI;
  `ILogger<T>` injected throughout; optional `Sonata.Avalonia.Hosting` for Generic Host (`IHost`/`IHostedService`)

## Packages

| Package | Purpose | NuGet |
|---------|---------|-------|
| `Sonata.Avalonia` | Core framework — Screen, Conductors, ViewManager, WindowManager, actions, validation, dispatching | [`Sonata.Avalonia`](https://nuget.org/packages/Sonata.Avalonia) |
| `Sonata.Avalonia.StyletIoC` | Legacy StyletIoC container support; exposes `StyletApplication<T>` for minimal migration | [`Sonata.Avalonia.StyletIoC`](https://nuget.org/packages/Sonata.Avalonia.StyletIoC) |
| `Sonata.Avalonia.Hosting` | Generic Host integration — `IHost`/`IHostedService` alongside the Avalonia UI | [`Sonata.Avalonia.Hosting`](https://nuget.org/packages/Sonata.Avalonia.Hosting) |

## Quick start

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
        services.AddSingleton<IMyService, MyService>();
        // ShellViewModel is registered automatically by convention
    }
}
```

> **Other bootstrappers:** If you prefer the StyletIoC container, use `StyletApplication<T>`
> from `Sonata.Avalonia.StyletIoC` (see `samples/Sonata.Samples.Hello`). For DryIoc,
> see `samples/Sonata.Samples.DryIoC`. For Generic Host, see `samples/Sonata.Samples.Hosting`
> (package `Sonata.Avalonia.Hosting`).

## Samples

| Sample | Demonstrates | Path |
|--------|-------------|------|
| Hello | `StyletApplication<T>` with StyletIoC container | `samples/Sonata.Samples.Hello/` |
| MSIoC | `SonataApplication<T>` with Microsoft.Extensions.DI (recommended) | `samples/Sonata.Samples.MSIoC/` |
| DryIoC | Custom DryIoc container integration | `samples/Sonata.Samples.DryIoC/` |
| HelloDialog | `ShowDialog<T>`, dialog factories, `RequestClose` | `samples/Sonata.Samples.HelloDialog/` |
| MasterDetail | Master-detail conductor, `BindableCollection`, selection | `samples/Sonata.Samples.MasterDetail/` |
| TabNavigation | `Conductor<T>` + TabControl, `SonataConductorTabControl` | `samples/Sonata.Samples.TabNavigation/` |
| NavigationController | Custom navigation controller, delegate injection | `samples/Sonata.Samples.NavigationController/` |
| OverridingViewManager | Custom `IViewManager` (attribute-based mapping) | `samples/Sonata.Samples.OverridingViewManager/` |

## Documentation

- **[API Reference](docs/api/)** — `docs/api/README.md` links to ~11 domain pages: bootstrappers, screen lifecycle,
  conductors, bindables, view location, actions, window manager, validation, event aggregator, commands, dispatching
- **[Migration Guide](docs/MIGRATION.md)** — Stylet.Avalonia 0.5.1 → Sonata.Avalonia 1.0.0

## License

MIT — see [LICENSE.txt](LICENSE.txt). Includes copyright notices from
[Stylet](https://github.com/canton7/Stylet) (canton7) and
[Stylet.Avalonia](https://github.com/sealoyal2018/Stylet.Avalonia) (sealoyal), preserved under MIT license.
