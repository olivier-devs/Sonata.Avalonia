using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Xunit;

namespace Sonata.Avalonia.Headless.Tests;

public class HeadlessSmokeTests
{
    [AvaloniaFact]
    public void Window_CanBeCreatedAndShown()
    {
        var window = new Window();
        window.Show();

        Assert.True(window.IsVisible);
        window.Close();
    }
}
