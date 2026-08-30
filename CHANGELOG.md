# Changelog

All notable changes to this project are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.0.0] - 2026-08-30

> First stable release of Sonata.Avalonia — a modernized, async-first MVVM framework
> for Avalonia UI, derived from Stylet.Avalonia 0.5.1.

### Added

- Three-package layout: `Sonata.Avalonia` (core), `Sonata.Avalonia.StyletIoC` (backward-compatibility IoC), `Sonata.Avalonia.Hosting` (Generic Host integration) *(SP-1 §2, §3)*
- `SonataApplication<T>` bootstrapper with Microsoft.Extensions.DependencyInjection and convention-based auto-registration (`ViewModel` → singleton, `View` → transient) *(SP-1 §5)*
- Async Screen/Conductor lifecycle: `OnActivateAsync`, `ActivateItemAsync`, serialized transitions via `SemaphoreSlim(1,1)`, `IAsyncDisposable` on `Screen` *(SP-2 §3, §4.1, §4.2)*
- `IDispatcher` abstraction — injectable via DI; `SynchronousDispatcher` for headless tests; `ApplicationDispatcher` installed by the bootstrapper *(SP-3 §3)*
- `SonataHostedApplication<T>` Generic Host integration (`IHost` start/stop/dispose lifecycle via internal `HostRunner`) *(SP-1 §8)*
- Headless test suite (`Avalonia.Headless.XUnit`) + **107 unit/headless tests** across two test projects *(SP-4 §3, §6)*
- 8 samples: 3 DI samples (Hello/StyletIoC, MSIoC/MS.DI, DryIoC/custom) + 5 feature samples (HelloDialog, MasterDetail, TabNavigation, NavigationController, OverridingViewManager) *(SP-5 §4.2)*
- API reference (`docs/api/`, 11 pages) covering bootstrappers, screen lifecycle, conductors, bindables, view location, actions, window manager, validation, event aggregator, commands, and dispatching *(SP-5 §5.3)*
- Migration guide (`docs/MIGRATION.md`) documenting all breaking changes from Stylet.Avalonia 0.5.1 *(SP-5 §6.1)*

### Changed

- Renamed from **Stylet.Avalonia to Sonata.Avalonia**: namespaces (`Stylet.Avalonia` → `Sonata.Avalonia`), types (`Stylet*` → `Sonata*`), NuGet package IDs; XAML prefix `s:` is preserved (`xmlns:s="using:Sonata.Avalonia"`) *(SP-1 §1, §6)*
- Logging migrated to **Microsoft.Extensions.Logging** — custom `Stylet.Avalonia.Logging.*` types (`ILogger`, `LogManager`, `TraceLogger`, `NullLogger`) removed from public API *(SP-1 §6)*
- Validation is **async-only**: `ValidateAsync(ct)` / `ValidatePropertyAsync(propertyName, ct)` are the only public entry points; synchronous facades (`.Result`) removed *(SP-2 §5)*
- `PropertyChanged` and UI dispatch are **non-blocking off the UI thread** — `PropertyChangedBase.OnPropertyChanged` uses `Post` instead of blocking `Invoke` *(SP-3 §4)*
- MessageBox caption fallback changed from Chinese `"提示"` to **empty string** (`""`) *(SP-5 H5)*
- `ApplicationDispatcher.Send` is now truly synchronous (was fire-and-forget, violating the `IDispatcher` contract) *(SP-3 §3.3, SP-3 §2)*

### Deprecated

- `Execute` static facade — inject `IDispatcher` instead; the facade is kept functional but marked `[Obsolete]` with a warning *(SP-3 §3.2)*

### Removed

- Public `IoC` service locator (now `internal`; exposed only for the XAML `View.Model` bridge); use constructor injection or the StyletIoC package for IoC access *(SP-1 §7)*
- Synchronous validation facades (`Validate()`, `ValidateProperty()`) — async-only since 1.0.0 *(SP-2 §5)*
- `Stylet.Avalonia.Logging.*` public types — migrated to `ILogger<T>` MEL *(SP-1 §6)*
- Dead `Execute` members with zero usage: `OnUIThreadSync`, `PostToUIThreadAsync`, `OnUIThreadAsync`, `DefaultPropertyChangedDispatcher` *(SP-3 §3.2)*
- `PropertyChangedDispatcher`, `INotifyPropertyChangedDispatcher`, `ViewManager.InitializeView` (no-op) *(SP-3 §3.2, §5)*

### Fixed

- ViewModel closure is now guaranteed on all window close paths — `WindowConductor.WindowClosed` moves `Dispose` into a try block and calls `TryCloseAsync` in `finally`; adapter failures can no longer prevent ViewModel cleanup or crash handlers *(SP-5 H1)*
- Exceptions from async XAML actions (`ActionBase.InvokeTargetMethod`) are now logged instead of silently swallowed via `FireAndForget.Run` helper *(SP-4 §4)*
- `View.Model` with Avalonia 12 binding values — `BindingValue` is unwrapped correctly when setting the attached property *(SP-4 §3.2)*
- `MessageBoxViewModel` honors explicit `cancelResult` set via `ButtonClicked` even when the dialog is cancelled programmatically *(SP-5 H5)*
- `ISingleViewApplicationLifetime.MainView` receives the **created View** (was always `null` because the bootstrapper assigned the ViewModel instead of the view) *(SP-1 §9, Bug #1)*
- Startup-location defaulting is reachable; `ShowDialog` owner passed through all overload chains *(SP-1 §9, Bug #2; SP-3 §6.2)*

### Security

- **Close guard hardened**: `e.Cancel = true` is set as the **first instruction** in `WindowClosing` (before any user code or logging), preventing a malicious or misbehaving logger from allowing the window to close prematurely *(SP-5 H1, SP-4 §5.3 S1)*
- **Window adapter `Dispose` failures cannot crash handlers or leave ViewModels open**: `Dispose` is inside the try block, `TryCloseAsync` runs in `finally`; all exceptions in the close path are caught and logged *(SP-5 H1, SP-4 §5.3 S2)*
- `FireAndForget.Run` wraps the logger call itself in a try/catch — a throwing logger cannot propagate an exception onto the thread pool *(SP-4 §5.3)*
