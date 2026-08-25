using System.Reflection;
using Microsoft.Extensions.Logging.Abstractions;
using Sonata.Avalonia;
using Xunit;

namespace Sonata.Avalonia.Tests;

public class ViewManagerTests
{
    private class NoViewViewModel { }

    private static ViewManager CreateManager(List<Assembly> assemblies) =>
        new(new ViewManagerConfig
        {
            ViewFactory = type => Activator.CreateInstance(type)!,
            ViewAssemblies = assemblies,
        }, NullLogger<ViewManager>.Instance);

    [Fact]
    public void CreateViewForModel_LocatesAndInstantiatesView()
    {
        var manager = CreateManager(new List<Assembly> { typeof(TestRootViewModel).Assembly });

        var view = manager.CreateViewForModel(new TestRootViewModel());

        Assert.IsType<TestRootView>(view);
    }

    [Fact]
    public void CreateViewForModel_CachesViewType()
    {
        var manager = CreateManager(new List<Assembly> { typeof(TestRootViewModel).Assembly });

        var v1 = manager.CreateViewForModel(new TestRootViewModel());
        var v2 = manager.CreateViewForModel(new TestRootViewModel());

        Assert.NotSame(v1, v2);                       // two distinct views
        Assert.Single(manager.ViewTypeCache);         // but a single lookup
        Assert.Equal(typeof(TestRootView), manager.ViewTypeCache[typeof(TestRootViewModel)]);
    }

    [Fact]
    public void CreateViewForModel_MissingView_Throws()
    {
        var manager = CreateManager(new List<Assembly> { typeof(NoViewViewModel).Assembly });

        Assert.Throws<SonataViewLocationException>(() => manager.CreateViewForModel(new NoViewViewModel()));
    }
}
