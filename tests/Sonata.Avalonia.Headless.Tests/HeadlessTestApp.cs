using Avalonia;
using Avalonia.Headless;
using Avalonia.Skia;

[assembly: AvaloniaTestApplication(typeof(Sonata.Avalonia.Headless.Tests.HeadlessTestApp))]

namespace Sonata.Avalonia.Headless.Tests;

public class HeadlessTestApp
{
    public static AppBuilder BuildAvaloniaApp() => AppBuilder
        .Configure<App>()
        .UseSkia()
        .UseHeadless(new AvaloniaHeadlessPlatformOptions
        {
            UseHeadlessDrawing = false,
        });
}

public class App : Application
{
}
