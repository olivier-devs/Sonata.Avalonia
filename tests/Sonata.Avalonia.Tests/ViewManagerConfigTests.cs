using Avalonia.Controls;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Sonata.Avalonia;
using Xunit;

namespace Sonata.Avalonia.Tests
{
    public class ViewManagerConfigTests
    {
        // ---------------------------------------------------------------------
        // Stubs for config-level tests: private nested types, only ever
        // referenced via typeof(...), never located by name.
        // Fixtures used for convention-based discovery (ViewManager resolves
        // views by transformed FullName via Assembly.GetType, and nested type
        // names contain '+') are declared at the bottom of this file, as
        // top-level types in real sub-namespaces.
        // ---------------------------------------------------------------------
        private class NotAControl { }
        private abstract class AbstractView : Control { }
        private class MainView : Control { }
        private class OldView : Control { }
        private class NewView : Control { }
        private class MainViewModel { }
        private class SomeViewModel { }

        private static ViewManager CreateManager(ViewManagerConfig config) =>
            new(Options.Create(config), NullLogger<ViewManager>.Instance);

        // --- AddViewAssembly ---

        [Fact]
        public void AddViewAssembly_Generic_AddsAssembly()
        {
            var config = new ViewManagerConfig();

            config.AddViewAssembly<MainViewModel>();

            var assembly = Assert.Single(config.ViewAssemblies);
            Assert.Equal(typeof(MainViewModel).Assembly, assembly);
        }

        [Fact]
        public void AddViewAssembly_Instance_AddsAssembly()
        {
            var config = new ViewManagerConfig();

            config.AddViewAssembly(typeof(MainViewModel).Assembly);

            var assembly = Assert.Single(config.ViewAssemblies);
            Assert.Equal(typeof(MainViewModel).Assembly, assembly);
        }

        [Fact]
        public void AddViewAssembly_IgnoresDuplicates()
        {
            var config = new ViewManagerConfig();

            config.AddViewAssembly<MainViewModel>();
            config.AddViewAssembly(typeof(MainViewModel).Assembly);
            config.AddViewAssembly<MainViewModel>();

            Assert.Single(config.ViewAssemblies);
        }

        [Fact]
        public void AddViewAssembly_Null_ThrowsArgumentNullException()
        {
            var config = new ViewManagerConfig();

            Assert.Throws<ArgumentNullException>(() => config.AddViewAssembly(null!));
        }

        // --- MapNamespace ---

        [Fact]
        public void MapNamespace_AddsMapping()
        {
            var config = new ViewManagerConfig();

            config.MapNamespace("VmLand", "ViewLand");

            Assert.Equal("ViewLand", config.NamespaceTransformations["VmLand"]);
            // The dictionary uses StringComparer.Ordinal: lookups are case-sensitive.
            Assert.Throws<KeyNotFoundException>(() => _ = config.NamespaceTransformations["vmland"]);
        }

        [Fact]
        public void MapNamespace_LastWins()
        {
            var config = new ViewManagerConfig();

            config.MapNamespace("VmLand", "FirstViewLand");
            config.MapNamespace("VmLand", "SecondViewLand");

            Assert.Equal("SecondViewLand", config.NamespaceTransformations["VmLand"]);
            Assert.Single(config.NamespaceTransformations);
        }

        [Fact]
        public void MapNamespace_NullViewModelNamespace_ThrowsArgumentNullException()
        {
            var config = new ViewManagerConfig();

            Assert.Throws<ArgumentNullException>(() => config.MapNamespace(null!, "ViewLand"));
        }

        [Fact]
        public void MapNamespace_EmptyViewModelNamespace_ThrowsArgumentException()
        {
            var config = new ViewManagerConfig();

            Assert.Throws<ArgumentException>(() => config.MapNamespace("", "ViewLand"));
        }

        [Fact]
        public void MapNamespace_WhitespaceViewModelNamespace_ThrowsArgumentException()
        {
            var config = new ViewManagerConfig();

            Assert.Throws<ArgumentException>(() => config.MapNamespace("   ", "ViewLand"));
        }

        [Fact]
        public void MapNamespace_NullViewNamespace_ThrowsArgumentNullException()
        {
            var config = new ViewManagerConfig();

            Assert.Throws<ArgumentNullException>(() => config.MapNamespace("VmLand", null!));
        }

        [Fact]
        public void MapNamespace_EmptyViewNamespace_ThrowsArgumentException()
        {
            var config = new ViewManagerConfig();

            Assert.Throws<ArgumentException>(() => config.MapNamespace("VmLand", ""));
        }

        [Fact]
        public void MapNamespace_WhitespaceViewNamespace_ThrowsArgumentException()
        {
            var config = new ViewManagerConfig();

            Assert.Throws<ArgumentException>(() => config.MapNamespace("VmLand", "   "));
        }

        [Fact]
        public void MapNamespace_UsedInDiscovery()
        {
            // Arrange — ViewModels.MappedViewModel must resolve to Views.MappedView
            var config = new ViewManagerConfig()
                .SetViewFactory(type => Activator.CreateInstance(type)!)
                .AddViewAssembly(typeof(ViewModels.MappedViewModel).Assembly)
                .MapNamespace("Sonata.Avalonia.Tests.ViewModels", "Sonata.Avalonia.Tests.Views");
            var manager = CreateManager(config);

            // Act
            var view = manager.CreateViewForModel(new ViewModels.MappedViewModel());

            // Assert
            Assert.IsType<Views.MappedView>(view);
            Assert.Single(manager.ViewTypeCache);
        }

        // --- AddView ---

        [Fact]
        public void AddView_Generic_RegistersMapping()
        {
            var config = new ViewManagerConfig();

            config.AddView<MainView, MainViewModel>();

            Assert.Equal(typeof(MainView), config.ExplicitViewMappings[typeof(MainViewModel)]);
            Assert.Single(config.ExplicitViewMappings);
        }

        [Fact]
        public void AddView_Instance_RegistersMapping()
        {
            var config = new ViewManagerConfig();

            config.AddView(typeof(MainView), typeof(MainViewModel));

            Assert.Equal(typeof(MainView), config.ExplicitViewMappings[typeof(MainViewModel)]);
            Assert.Single(config.ExplicitViewMappings);
        }

        [Fact]
        public void AddView_ExplicitTakesPriorityOverConvention()
        {
            // Arrange — ConventionMainViewModel has a conventional match
            // (ConventionMainView, same namespace + suffix convention) AND an
            // explicit mapping to a different view
            var config = new ViewManagerConfig()
                .SetViewFactory(type => Activator.CreateInstance(type)!)
                .AddViewAssembly(typeof(ConventionMainViewModel).Assembly)
                .AddView<ConventionOtherMainView, ConventionMainViewModel>();
            var manager = CreateManager(config);

            // Act
            var view = manager.CreateViewForModel(new ConventionMainViewModel());

            // Assert — the explicit mapping wins over the convention
            Assert.IsType<ConventionOtherMainView>(view);
            Assert.IsNotType<ConventionMainView>(view);
        }

        [Fact]
        public void AddView_LastWins()
        {
            // Arrange
            var config = new ViewManagerConfig()
                .SetViewFactory(type => Activator.CreateInstance(type)!)
                .AddView<OldView, MainViewModel>()
                .AddView<NewView, MainViewModel>();

            // Act
            var manager = CreateManager(config);
            var view = manager.CreateViewForModel(new MainViewModel());

            // Assert
            Assert.Equal(typeof(NewView), config.ExplicitViewMappings[typeof(MainViewModel)]);
            Assert.Single(config.ExplicitViewMappings);
            Assert.IsType<NewView>(view);
        }

        [Fact]
        public void AddView_NonControl_ThrowsArgumentException()
        {
            var config = new ViewManagerConfig();

            Assert.Throws<ArgumentException>(() => config.AddView<NotAControl, SomeViewModel>());
        }

        [Fact]
        public void AddView_NonControl_InstanceOverload_ThrowsArgumentException()
        {
            var config = new ViewManagerConfig();

            Assert.Throws<ArgumentException>(() => config.AddView(typeof(NotAControl), typeof(SomeViewModel)));
        }

        [Fact]
        public void AddView_AbstractView_ThrowsArgumentException()
        {
            var config = new ViewManagerConfig();

            Assert.Throws<ArgumentException>(() => config.AddView<AbstractView, SomeViewModel>());
        }

        [Fact]
        public void AddView_NullViewType_ThrowsArgumentNullException()
        {
            var config = new ViewManagerConfig();

            Assert.Throws<ArgumentNullException>(() => config.AddView(null!, typeof(SomeViewModel)));
        }

        [Fact]
        public void AddView_NullViewModelType_ThrowsArgumentNullException()
        {
            var config = new ViewManagerConfig();

            Assert.Throws<ArgumentNullException>(() => config.AddView(typeof(MainView), null!));
        }

        // --- RemoveView ---

        [Fact]
        public void RemoveView_Generic_RemovesExisting()
        {
            var config = new ViewManagerConfig();
            config.AddView<MainView, MainViewModel>();

            config.RemoveView<MainViewModel>();

            Assert.Empty(config.ExplicitViewMappings);
        }

        [Fact]
        public void RemoveView_Instance_RemovesExisting()
        {
            var config = new ViewManagerConfig();
            config.AddView(typeof(MainView), typeof(MainViewModel));

            config.RemoveView(typeof(MainViewModel));

            Assert.Empty(config.ExplicitViewMappings);
        }

        [Fact]
        public void RemoveView_Nonexistent_DoesNotThrow()
        {
            var config = new ViewManagerConfig();
            config.AddView<MainView, MainViewModel>();

            config.RemoveView<SomeViewModel>();
            config.RemoveView(typeof(SomeViewModel));

            // Removing an unmapped ViewModel is a no-op: existing mappings survive
            Assert.Equal(typeof(MainView), config.ExplicitViewMappings[typeof(MainViewModel)]);
        }

        [Fact]
        public void RemoveView_Null_ThrowsArgumentNullException()
        {
            var config = new ViewManagerConfig();

            Assert.Throws<ArgumentNullException>(() => config.RemoveView(null!));
        }

        // --- ViewFactory ---

        [Fact]
        public void SetViewFactory_Null_ThrowsArgumentNullException()
        {
            var config = new ViewManagerConfig();

            Assert.Throws<ArgumentNullException>(() => config.SetViewFactory(null!));
        }

        [Fact]
        public void SetViewFactory_Chains()
        {
            var config = new ViewManagerConfig();

            var result = config.SetViewFactory(_ => new object());

            Assert.Same(config, result);
            Assert.NotNull(config.ViewFactory);
        }

        // --- Dependency Injection ---

        [Fact]
        public void ConfigureViewManager_SingleCall_Configures()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act
            services.ConfigureViewManager(o => o.AddView<MainView, MainViewModel>());

            // Assert
            using var provider = services.BuildServiceProvider();
            var config = provider.GetRequiredService<IOptions<ViewManagerConfig>>().Value;
            Assert.Equal(typeof(MainView), config.ExplicitViewMappings[typeof(MainViewModel)]);
        }

        [Fact]
        public void ConfigureViewManager_MultipleCalls_Cumulative()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act
            services.ConfigureViewManager(o => o.AddViewAssembly<MainViewModel>());
            services.ConfigureViewManager(o => o.AddView<MainView, MainViewModel>());

            // Assert — both calls contributed to the resolved configuration
            using var provider = services.BuildServiceProvider();
            var config = provider.GetRequiredService<IOptions<ViewManagerConfig>>().Value;
            Assert.Contains(typeof(MainViewModel).Assembly, config.ViewAssemblies);
            Assert.Equal(typeof(MainView), config.ExplicitViewMappings[typeof(MainViewModel)]);
        }

        [Fact]
        public void AddSonata_ProvidesDefaultsWhenNotConfigured()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act
            services.AddSonata(Mock.Of<IWindowManagerConfig>(), new[] { typeof(ViewManagerConfigTests).Assembly });

            // Assert — PostConfigure filled in the factory and the assembly
            using var provider = services.BuildServiceProvider();
            var config = provider.GetRequiredService<IOptions<ViewManagerConfig>>().Value;
            Assert.NotNull(config.ViewFactory);
            Assert.Contains(typeof(ViewManagerConfigTests).Assembly, config.ViewAssemblies);
        }

        [Fact]
        public void AddSonata_DoesNotOverrideUserSetViewFactory()
        {
            // Arrange — an identifiable custom factory, configured before AddSonata
            Func<Type, object> customFactory = _ => throw new InvalidOperationException("custom");
            var services = new ServiceCollection();

            // Act
            services.ConfigureViewManager(o => o.SetViewFactory(customFactory));
            services.AddSonata(Mock.Of<IWindowManagerConfig>(), new[] { typeof(ViewManagerConfigTests).Assembly });

            // Assert — the user's factory survived AddSonata's PostConfigure defaults
            using var provider = services.BuildServiceProvider();
            var config = provider.GetRequiredService<IOptions<ViewManagerConfig>>().Value;
            Assert.Same(customFactory, config.ViewFactory);
        }

        [Fact]
        public void AddSonata_PropagatesViewModelNameSuffix_WhenNotExplicitlyConfigured()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act
            services.AddSonata(Mock.Of<IWindowManagerConfig>(), new[] { typeof(ViewManagerConfigTests).Assembly }, viewModelNameSuffix: "Vm");

            // Assert — the suffix passed to AddSonata reached the config
            using var provider = services.BuildServiceProvider();
            var config = provider.GetRequiredService<IOptions<ViewManagerConfig>>().Value;
            Assert.Equal("Vm", config.ViewModelNameSuffix);
        }

        [Fact]
        public void AddSonata_DoesNotOverrideUserSetViewModelNameSuffix()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act
            services.ConfigureViewManager(o => o.SetViewModelNameSuffix("CustomVm"));
            services.AddSonata(Mock.Of<IWindowManagerConfig>(), new[] { typeof(ViewManagerConfigTests).Assembly }, viewModelNameSuffix: "Vm");

            // Assert — the user's explicit suffix survived AddSonata's PostConfigure defaults
            using var provider = services.BuildServiceProvider();
            var config = provider.GetRequiredService<IOptions<ViewManagerConfig>>().Value;
            Assert.Equal("CustomVm", config.ViewModelNameSuffix);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void AddSonata_InvalidViewModelNameSuffix_ThrowsArgumentException(string? suffix)
        {
            var services = new ServiceCollection();
            var windowManagerConfig = Mock.Of<IWindowManagerConfig>();

            Assert.Throws<ArgumentException>(() =>
                services.AddSonata(windowManagerConfig, new[] { typeof(ViewManagerConfigTests).Assembly }, viewModelNameSuffix: suffix!));
        }

        [Fact]
        public void ViewManager_RequiresViewFactory()
        {
            var ex = Assert.Throws<InvalidOperationException>(
                () => new ViewManager(Options.Create(new ViewManagerConfig()), NullLogger<ViewManager>.Instance));

            Assert.Contains("ViewFactory", ex.Message);
        }

        // --- Cache ---

        [Fact]
        public void ExplicitMapping_IsCached()
        {
            // Arrange
            var config = new ViewManagerConfig()
                .SetViewFactory(type => Activator.CreateInstance(type)!)
                .AddView<MainView, MainViewModel>();
            var manager = CreateManager(config);

            // Act
            manager.CreateViewForModel(new MainViewModel());

            // Assert
            Assert.Single(manager.ViewTypeCache);
            Assert.Equal(typeof(MainView), manager.ViewTypeCache[typeof(MainViewModel)]);
        }

        [Fact]
        public void ExplicitMapping_PriorityIsRespected_WhenConventionAlsoExists()
        {
            // Arrange — a conventional view exists, but the explicit mapping must win
            var config = new ViewManagerConfig()
                .SetViewFactory(type => Activator.CreateInstance(type)!)
                .AddViewAssembly(typeof(ConventionMainViewModel).Assembly)
                .AddView<ConventionOtherMainView, ConventionMainViewModel>();
            var manager = CreateManager(config);

            // Act
            manager.CreateViewForModel(new ConventionMainViewModel());

            // Assert — the cached resolution is the explicit one
            var cached = Assert.Single(manager.ViewTypeCache);
            Assert.Equal(typeof(ConventionMainViewModel), cached.Key);
            Assert.Equal(typeof(ConventionOtherMainView), cached.Value);
        }
    }

    // ---------------------------------------------------------------------
    // Convention-discovery fixtures: top-level types in real sub-namespaces,
    // so that ViewManager's name-based lookup (Assembly.GetType on the
    // ViewModel's transformed FullName) can locate them.
    // ---------------------------------------------------------------------

    namespace ViewModels
    {
        internal class MappedViewModel { }
    }

    namespace Views
    {
        internal class MappedView : Control { }
    }

    // Conventional pair (ConventionMainViewModel -> ConventionMainView by
    // suffix convention), plus the explicit-mapping target that must win
    // over the convention in the priority tests.
    internal class ConventionMainViewModel { }
    internal class ConventionMainView : Control { }
    internal class ConventionOtherMainView : Control { }
}
