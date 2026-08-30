using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using Sonata.Avalonia;
using Xunit;

namespace Sonata.Avalonia.Headless.Tests;

public class DispatcherHeadlessTests
{
    [AvaloniaFact]
    public void Send_ExecutesSynchronously_OnUIThread()
    {
        var dispatcher = new ApplicationDispatcher(Dispatcher.UIThread);

        var executed = false;
        dispatcher.Send(() => executed = true);

        Assert.True(executed);
    }

    [AvaloniaFact]
    public void Post_ExecutesInline_WhenPumped()
    {
        var dispatcher = new ApplicationDispatcher(Dispatcher.UIThread);

        var executed = false;
        dispatcher.Post(() => executed = true);

        Dispatcher.UIThread.RunJobs();

        Assert.True(executed);
    }
}
