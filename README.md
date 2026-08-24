# Sonata.Avalonia

A lightweight, powerful ViewModel-first MVVM framework for [Avalonia UI](https://avaloniaui.net/),
modernized from [Stylet.Avalonia](https://github.com/sealoyal2018/Stylet.Avalonia) (itself a port of
[Stylet](https://github.com/canton7/Stylet)) for .NET 8/9/10 and Avalonia 12.

**Status: 1.0.0-preview.1 — modernization in progress.**

## Packages

| Package | Purpose |
|---------|---------|
| `Sonata.Avalonia` | Core framework — Screen, Conductors, ViewManager, WindowManager, actions, validation. DI via Microsoft.Extensions.DependencyInjection. |
| `Sonata.Avalonia.StyletIoC` | Legacy StyletIoC container support (original Stylet bootstrapper experience). |
| `Sonata.Avalonia.Hosting` | Generic Host (`IHost`/`IHostedService`) integration. |

## Quick start

```csharp
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

See the `samples/` directory for complete examples (MS.DI, StyletIoC, DryIoC).

## License

MIT — see [LICENSE.txt](LICENSE.txt). Includes copyright notices from Stylet and Stylet.Avalonia.