# Sonata.Avalonia API Reference

This section documents the key types and common use cases for Sonata.Avalonia. Signatures, default values, and behavioural details reflect the actual source code in `src/`.

> **Note:** Exact API signatures live in the XML documentation embedded in the source. These pages focus on **use cases** and **derived code examples** verified against the 8 samples and `src/` assemblies.

## Pages

| Page | Description |
|------|-------------|
| [bootstrappers](./bootstrappers.md) | Application bootstrappers: `SonataApplication<T>`, `StyletApplication<T>`, `SonataHostedApplication<T>`, startup sequence, convention registration |
| [screen-lifecycle](./screen-lifecycle.md) | Screen lifecycle: `Screen`, `IScreen`, `ScreenState`, async hooks (`OnActivateAsync`/`OnDeactivateAsync`/`OnCloseAsync`), close guard, `IViewAware` |
| [bindables](./bindables.md) | Observable objects: `PropertyChangedBase`, `SetAndNotify`, `BindableCollection<T>`, `INotifyCollectionChanging`, `LabelledValue<T>` |
| [conductors](./conductors.md) | Conductors: `Conductor<T>`, `Conductor<T>.Collection.OneActive`, `Conductor<T>.Collection.AllActive`, `Conductor<T>.StackNavigation`, item lifecycle |
| [view-location](./view-location.md) | View location: `IViewManager`, view-model to view resolution, naming conventions, `ViewManagerConfig` |
| [actions](./actions.md) | XAML actions: `IAction` via `<Button Action="">`, `ICommand` binding, `RoutedCommand` |
| [window-manager](./window-manager.md) | Window management: `IWindowManager`, `ShowWindowAsync`, `ShowDialogAsync`, `ShowMessageBox`, `WindowConductor` lifecycle |
| [validation](./validation.md) | Validation: `ValidatingModelBase`, `IModelValidator`, `IValidationAdapter`, integration with `DataAnnotations` |
| [event-aggregator](./event-aggregator.md) | Event aggregation: `IEventAggregator`, `Subscribe`, `Publish`, weak subscriptions |
| [commands](./commands.md) | Commands: `RelayCommand`, `IAction` (action callable from XAML), `ActionExtension` |
| [dispatching](./dispatching.md) | Dispatching: `IDispatcher`, `UiThreadDispatch`, fire-and-forget, `Execute.OnUiThread` |
