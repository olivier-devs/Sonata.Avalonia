using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Sonata.Avalonia;
using Sonata.Avalonia.Internal;
using Sonata.Avalonia.Primitive;
using Xunit;

namespace Sonata.Avalonia.Headless.Tests;

public class WindowManagerHeadlessTests
{
    private static WindowManager CreateManager(FakeWindowManagerConfig config) =>
        TestHost.CreateWindowManager(config, () => new MessageBoxViewModel());

    [AvaloniaFact]
    public void ShowWindow_LocatesViewByConvention_BindsTitleAndWiresConductor()
    {
        // Arrange
        var manager = CreateManager(new FakeWindowManagerConfig());
        var vm = new ShellViewModel { DisplayName = "Shell Title" };

        // Act
        manager.ShowWindow(vm);

        // Assert — the View was located by naming convention and shown
        var window = Assert.IsType<ShellView>(vm.View);
        Assert.True(window.IsVisible);
        Assert.Null(window.Owner);

        // Title is bound to the ViewModel's DisplayName
        Assert.Equal("Shell Title", window.Title);

        // The WindowConductor was wired as the ViewModel's parent, and activated it
        Assert.IsType<WindowConductor>(vm.Parent);
        Assert.True(vm.IsActive);

        // Requesting a close through the conductor closes the window
        vm.RequestClose();
        Assert.False(window.IsVisible);
    }

    [AvaloniaFact]
    public void ShowWindow_WithOwnerViewModel_ShowsOwnedWindow()
    {
        // Arrange
        var manager = CreateManager(new FakeWindowManagerConfig());
        var ownerVm = new ShellViewModel();
        manager.ShowWindow(ownerVm);
        var vm = new DialogViewModel();

        // Act
        manager.ShowWindow(vm, ownerVm);

        // Assert
        var window = Assert.IsType<DialogView>(vm.View);
        Assert.True(window.IsVisible);
        Assert.Same(ownerVm.View, window.Owner);
    }

    [AvaloniaFact]
    public void ShowWindow_WithNonWindowView_ThrowsInvalidViewType()
    {
        // Arrange — WidgetView is a UserControl, not a Window
        var manager = CreateManager(new FakeWindowManagerConfig());

        // Act / Assert
        var ex = Assert.Throws<SonataInvalidViewTypeException>(() => manager.ShowWindow(new WidgetViewModel()));
        Assert.Contains("Window", ex.Message);
    }

    [AvaloniaFact]
    public async Task ShowDialog_WithOwnerViewModel_ShowsDialogWhichCompletesOnClose()
    {
        // Arrange
        var manager = CreateManager(new FakeWindowManagerConfig());
        var ownerVm = new ShellViewModel();
        manager.ShowWindow(ownerVm);
        var vm = new DialogViewModel();

        // Act
        var dialogTask = manager.ShowDialog<object>(vm, ownerVm);

        // Assert — the dialog is shown, owned by the owner ViewModel's window
        var window = Assert.IsType<DialogView>(vm.View);
        Assert.True(window.IsVisible);
        Assert.Same(ownerVm.View, window.Owner);

        // Closing the ViewModel completes the dialog task
        vm.RequestClose();
        Assert.Null(await dialogTask);
    }

    [AvaloniaFact]
    public async Task ShowDialog_WithoutOwnerOrActiveWindow_Throws()
    {
        // Arrange — no ownerViewModel, and the config reports no active window
        var manager = CreateManager(new FakeWindowManagerConfig());
        var vm = new DialogViewModel();

        // Act / Assert — the owner check throws before the dialog is shown
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => manager.ShowDialog<object>(vm));
        Assert.Contains("owner", ex.Message);
    }

    [AvaloniaFact]
    public async Task ShowDialog_WithoutOwnerViewModel_InfersOwnerFromActiveWindow()
    {
        // Arrange — the config reports the shown owner window as active
        var config = new FakeWindowManagerConfig();
        var manager = CreateManager(config);
        var ownerVm = new ShellViewModel();
        manager.ShowWindow(ownerVm);
        config.ActiveWindow = (Window?)ownerVm.View;
        var vm = new DialogViewModel();

        // Act
        var dialogTask = manager.ShowDialog<object>(vm);

        // Assert — the owner was inferred from the active window
        var window = Assert.IsType<DialogView>(vm.View);
        Assert.True(window.IsVisible);
        Assert.Same(ownerVm.View, window.Owner);

        vm.RequestClose();
        await dialogTask;
    }
}
