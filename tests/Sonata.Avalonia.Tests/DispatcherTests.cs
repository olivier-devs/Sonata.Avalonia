using Sonata.Avalonia;
using Xunit;

namespace Sonata.Avalonia.Tests;

public class DispatcherTests
{
    [Fact]
    public void SynchronousDispatcher_ExecutesActionsInline()
    {
        var dispatcher = SynchronousDispatcher.Instance;

        var executed = false;
        dispatcher.Post(() => executed = true);

        Assert.True(executed);
        Assert.True(dispatcher.IsCurrent);
    }
}
