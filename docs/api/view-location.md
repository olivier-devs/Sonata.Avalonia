# View Location

Locates views for view models using naming conventions and namespace transformations. Handles view instantiation, binding, and the `s:View.Model` attached property.

## Key types

| Type | Role | Package |
|------|------|---------|
| `IViewManager` | Interface for locating and creating views | `Sonata.Avalonia` |
| `ViewManager` | Default implementation with caching and convention-based resolution | `Sonata.Avalonia` |
| `ViewManagerConfig` | Configuration for `ViewManager` — requires `ViewFactory` and `ViewAssemblies` | `Sonata.Avalonia` |
| `NamespaceTransformations` | Dictionary mapping namespace prefixes to replace | `ViewManager` |
| `SonataViewLocationException` | Thrown when a view cannot be located | `Sonata.Avalonia` |
| `SonataInvalidViewTypeException` | Thrown when located view is a `Window` (expected `UserControl`) | `Sonata.Avalonia` |
| `View.Model` | Attached property identifying the view model for a container | `Sonata.Avalonia.Xaml` |
| `View.ActionTarget` | Attached property inheritable down the visual tree | `Sonata.Avalonia.Xaml` |

## Use cases

### Default convention: ViewModel → View name transformation

`ViewManager` resolves view types by transforming the view model's full type name:

1. Apply `NamespaceTransformations` (prefix replacement)
2. Strip `ViewModelNameSuffix` ("ViewModel" by default), append `ViewNameSuffix` ("View")

```csharp
// Given: MyApp.ViewModels.Customers.CustomerListViewModel
// With defaults: MyApp.Views.Customers.CustomerListView

// Transform happens in ViewManager.ViewTypeNameForModelTypeName (line 240-258)
// NamespaceTransformations are applied first (prefix matching, line 244-250)
// Then suffix replacement via regex (line 253-255):
//   "(?<=.)ViewModel(?=s?.)|ViewModel$" → "View"
```

Results are cached per view model type in `ViewManager.ViewTypeCache`. Configure all conventions **before** the first lookup — the cache is never invalidated.

### Custom namespace transformations

```csharp
// App.axaml.cs
protected override void ConfigureSonataServices(IServiceCollection services)
{
    var viewManager = new ViewManager(new ViewManagerConfig
    {
        ViewFactory = type => AvaloniaXamlLoader.Load(type),
        ViewAssemblies = new List<Assembly> { GetType().Assembly }
    })
    {
        NamespaceTransformations = new Dictionary<string, string>
        {
            ["MyApp.Features.Customers"] = "MyApp.UI.Customers",
            ["MyApp.Domain"] = "MyApp.Presentation",
        },
        ViewNameSuffix = "Screen",
        ViewModelNameSuffix = "ViewModel"
    };
    services.AddSingleton<IViewManager>(viewManager);
}
```

### Custom ViewManager with attribute-based mapping

`samples/Sonata.Samples.OverridingViewManager/CustomViewManager.cs` overrides `LocateViewForModel` to use a `[ViewModel]` attribute:

```csharp
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
sealed class ViewModelAttribute : Attribute
{
    public Type ViewModel { get; }
    public ViewModelAttribute(Type viewModel) => ViewModel = viewModel;
}

public class CustomViewManager : ViewManager
{
    private readonly Dictionary<Type, Type> _viewModelToViewMapping;

    public CustomViewManager(ViewManagerConfig config, ILogger<ViewManager> logger)
        : base(config, logger)
    {
        var mappings = from type in ViewAssemblies.SelectMany(x => x.GetExportedTypes())
            let attribute = type.GetCustomAttribute<ViewModelAttribute>()
            where attribute != null && typeof(Control).IsAssignableFrom(type)
            select new { View = type, ViewModel = attribute.ViewModel };

        _viewModelToViewMapping = mappings.ToDictionary(x => x.ViewModel, x => x.View);
    }

    protected override Type LocateViewForModel(Type modelType)
    {
        return _viewModelToViewMapping.TryGetValue(modelType, out var view)
            ? view
            : base.LocateViewForModel(modelType);
    }
}

// Usage on a View:
[ViewModel(typeof(CustomerListViewModel))]
public partial class CustomerListView : UserControl { }
```

Register with `services.AddSingleton<IViewManager, CustomViewManager>()` in `ConfigureServices`. Fallback to base convention for unmapped types.

### View composition with s:View.Model

`samples/Sonata.Samples.NavigationController/Pages/ShellView.axaml` demonstrates `s:View.Model` for view composition:

```xml
<DockPanel>
    <!-- Header switches based on HeaderViewModel type -->
    <ContentControl DockPanel.Dock="Top" s:View.Model="{Binding HeaderViewModel}" />
    <!-- ActiveItem switches based on conductor's current item -->
    <ContentControl s:View.Model="{Binding ActiveItem}" />
</DockPanel>
```

When `View.Model` changes, `ViewManager.OnModelChanged` (line 165-187) resolves and instantiates the view, then sets the container's `Content`. The view's `DataContext` is set to the view model and `View.ActionTarget` is assigned for action resolution.

> **Important:** `s:View.Model` cannot target a `Window` — it throws `SonataInvalidViewTypeException`. Use `IWindowManager.ShowWindow` for windows.

## See also

- [`src/Sonata.Avalonia/ViewManager.cs`](../../src/Sonata.Avalonia/ViewManager.cs) — `ViewManager` with `ViewTypeNameForModelTypeName` convention logic
- [`src/Sonata.Avalonia/Xaml/View.cs`](../../src/Sonata.Avalonia/Xaml/View.cs) — `View.Model` and `View.ActionTarget` attached properties
- [`samples/Sonata.Samples.OverridingViewManager/CustomViewManager.cs`](../../samples/Sonata.Samples.OverridingViewManager/CustomViewManager.cs) — attribute-based custom `ViewManager`
- [`samples/Sonata.Samples.NavigationController/Pages/ShellView.axaml`](../../samples/Sonata.Samples.NavigationController/Pages/ShellView.axaml) — `s:View.Model` composition
- [Bootstrappers](./bootstrappers.md) — wiring a custom `IViewManager` via `ConfigureServices`
