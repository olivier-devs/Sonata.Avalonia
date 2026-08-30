using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Sonata.Avalonia;
using Sonata.Avalonia.Xaml;
using Xunit;

namespace Sonata.Avalonia.Headless.Tests;

public class ViewModelLocationTests
{
    /// <summary>
    /// Wires <see cref="IoC"/> (used by the View.Model attached property) to the given
    /// ViewManager for the duration of the test, restoring the previous delegate afterwards.
    /// </summary>
    private static void WithViewManagerIoC(ViewManager manager, Action test)
    {
        var original = IoC.GetInstance;
        IoC.GetInstance = (service, key) =>
        {
            if (service == typeof(IViewManager))
                return manager;
            throw new InvalidOperationException($"Unexpected IoC request for service '{service.Name}' during the test");
        };
        try
        {
            test();
        }
        finally
        {
            IoC.GetInstance = original;
        }
    }

    [AvaloniaFact]
    public void View_Model_Set_LocatesViewByConvention_AndSetsItAsContent()
    {
        // Arrange
        var manager = TestHost.CreateViewManager();
        WithViewManagerIoC(manager, () =>
        {
            var vm = new WidgetViewModel();
            var host = new ContentControl();

            // Act
            View.SetModel(host, vm);

            // Assert — the located View became the host's Content, fully bound to the ViewModel
            var view = Assert.IsType<WidgetView>(host.Content);
            Assert.Same(vm, view.DataContext);
            Assert.Same(view, vm.View);
        });
    }

    [AvaloniaFact]
    public void View_Model_ClearedToNull_ClearsContent()
    {
        // Arrange
        var manager = TestHost.CreateViewManager();
        WithViewManagerIoC(manager, () =>
        {
            var host = new ContentControl();
            View.SetModel(host, new WidgetViewModel());
            Assert.IsType<WidgetView>(host.Content);

            // Act — clearing View.Model to null clears the content (the parameter is
            // annotated non-nullable, but null is the framework's documented clear value)
            View.SetModel(host, null!);

            // Assert
            Assert.Null(host.Content);
        });
    }

    [AvaloniaFact]
    public void View_Model_WithCustomSuffixes_LocatesPageView()
    {
        // Arrange — 'SuffixVm' should be located as 'SuffixPage' with these suffixes
        var manager = TestHost.CreateViewManager(m =>
        {
            m.ViewModelNameSuffix = "Vm";
            m.ViewNameSuffix = "Page";
        });
        WithViewManagerIoC(manager, () =>
        {
            var vm = new SuffixVm();
            var host = new ContentControl();

            // Act
            View.SetModel(host, vm);

            // Assert
            var view = Assert.IsType<SuffixPage>(host.Content);
            Assert.Same(vm, view.DataContext);
        });
    }

    [AvaloniaFact]
    public void View_Model_WithNamespaceTransformation_LocatesViewInMappedNamespace()
    {
        // Arrange — ViewModels in 'VmLand' are looked up in 'ViewLand'
        var manager = TestHost.CreateViewManager(m =>
        {
            m.NamespaceTransformations = new Dictionary<string, string>
            {
                ["Sonata.Avalonia.Headless.Tests.VmLand"] = "Sonata.Avalonia.Headless.Tests.ViewLand",
            };
        });
        WithViewManagerIoC(manager, () =>
        {
            var vm = new VmLand.NestedViewModel();
            var host = new ContentControl();

            // Act
            View.SetModel(host, vm);

            // Assert
            var view = Assert.IsType<ViewLand.NestedView>(host.Content);
            Assert.Same(vm, view.DataContext);
        });
    }
}
