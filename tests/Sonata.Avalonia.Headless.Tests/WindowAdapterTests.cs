using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Sonata.Avalonia.Internal;
using Xunit;

namespace Sonata.Avalonia.Headless.Tests;

public class WindowAdapterTests
{
    [AvaloniaFact]
    public void Closing_IsForwarded()
    {
        var window = new Window();
        window.Show();
        using var adapter = new WindowAdapter(window);
        var raised = false;
        adapter.Closing += (s, e) => raised = true;

        window.Close();

        Assert.True(raised);
    }

    [AvaloniaFact]
    public void Closed_IsForwarded()
    {
        var window = new Window();
        window.Show();
        using var adapter = new WindowAdapter(window);
        var raised = false;
        adapter.Closed += (s, e) => raised = true;

        window.Close();

        Assert.True(raised);
    }

    [AvaloniaFact]
    public void StateChanged_IsForwarded_OnWindowStateChange()
    {
        var window = new Window();
        using var adapter = new WindowAdapter(window);
        var raised = false;
        adapter.StateChanged += (s, e) => raised = true;

        window.WindowState = WindowState.Minimized;

        Assert.True(raised);
    }

    [AvaloniaFact]
    public void Close_PassesDialogResult_ToRealWindow()
    {
        var window = new Window();
        window.Show();
        var result = new object();
        using var adapter = new WindowAdapter(window);

        adapter.Close(result);

        Assert.False(window.IsVisible);
    }

    [AvaloniaFact]
    public void Dispose_StopsForwarding()
    {
        var window = new Window();
        window.Show();
        var adapter = new WindowAdapter(window);
        var count = 0;
        adapter.Closed += (s, e) => count++;
        adapter.Dispose();

        window.Close();

        Assert.Equal(0, count);
    }
}
