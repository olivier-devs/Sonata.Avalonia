using Sonata.Avalonia;
using Sonata.Avalonia.Internal;
using Xunit;

namespace Sonata.Avalonia.Tests;

/// <summary>
/// Serializes the test classes that share the ambient <see cref="UiThreadDispatch.Dispatcher"/>
/// static state (directly, or indirectly via <c>SonataApplicationBase.Initialize()</c> which
/// installs a dispatcher). xUnit runs test classes in parallel by default; classes in this
/// collection run one at a time and, because parallelization is disabled, never concurrently
/// with any other test in the assembly. Per-test try/finally guards remain as defense in depth.
/// </summary>
[CollectionDefinition("Ambient", DisableParallelization = true)]
public class AmbientCollection : ICollectionFixture<AmbientFixture>
{
}

/// <summary>
/// Installs a synchronous ambient dispatcher before the first class in the collection runs,
/// and resets it to the default after the last one finishes.
/// </summary>
public class AmbientFixture : IDisposable
{
    public AmbientFixture()
    {
        UiThreadDispatch.Dispatcher = SynchronousDispatcher.Instance;
    }

    public void Dispose()
    {
        UiThreadDispatch.Dispatcher = null; // reset to default
    }
}
