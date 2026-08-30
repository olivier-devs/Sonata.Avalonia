using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Sonata.Avalonia;
using Sonata.Avalonia.Primitive;
using Xunit;

namespace Sonata.Avalonia.Headless.Tests;

public class MessageBoxHeadlessTests
{
    /// <summary>IChildDelegate fake recording close requests made through RequestClose.</summary>
    private sealed class RecordingCloseDelegate : IChildDelegate
    {
        public List<(object Item, bool? DialogResult)> CloseRequests { get; } = new();

        public Task CloseItemAsync(object item, bool? dialogResult = null, CancellationToken ct = default)
        {
            CloseRequests.Add((item, dialogResult));
            return Task.CompletedTask;
        }
    }

    [AvaloniaFact]
    public void Setup_WithOkCancel_ConfiguresTextCaptionButtonsAndDefaults()
    {
        // Arrange
        var vm = new MessageBoxViewModel();

        // Act
        vm.Setup("Body", "Caption", MessageBoxButton.OKCancel,
            defaultResult: MessageBoxResult.OK, cancelResult: MessageBoxResult.Cancel);

        // Assert
        Assert.Equal("Body", vm.Text);
        Assert.Equal("Caption", vm.DisplayName);
        Assert.Equal([MessageBoxResult.OK, MessageBoxResult.Cancel], vm.ButtonList.Select(x => x.Value));
        Assert.Equal(["OK", "Cancel"], vm.ButtonList.Select(x => x.Label));
        Assert.Equal(MessageBoxResult.OK, vm.DefaultButton);
        Assert.Equal(MessageBoxResult.Cancel, vm.CancelButton);
    }

    [AvaloniaFact]
    public void ButtonClicked_RecordsClickedButton_AndRequestsClose()
    {
        // Arrange
        var vm = new MessageBoxViewModel();
        vm.Setup("Body", "Caption", MessageBoxButton.OK);
        var conductor = new RecordingCloseDelegate();
        vm.Parent = conductor;

        // Act — simulate the OK button being clicked
        vm.ButtonClicked(MessageBoxResult.OK);

        // Assert
        Assert.Equal(MessageBoxResult.OK, vm.ClickedButton);
        var request = Assert.Single(conductor.CloseRequests);
        Assert.Same(vm, request.Item);
        Assert.True(request.DialogResult);   // OK maps to a positive dialog result
    }

    [AvaloniaFact]
    public async Task ShowMessageBox_ShowsMessageBoxView_AndReturnsClickedButtonAsResult()
    {
        // Arrange — a shown owner window the dialog can be inferred from
        var config = new FakeWindowManagerConfig();
        MessageBoxViewModel? vm = null;
        var factoryCalls = 0;
        var manager = TestHost.CreateWindowManager(config, () =>
        {
            factoryCalls++;
            vm = new MessageBoxViewModel();
            return vm;
        });
        var ownerVm = new ShellViewModel();
        manager.ShowWindow(ownerVm);
        config.ActiveWindow = (Window?)ownerVm.View;

        // Act — show a message box and click OK
        var okTask = manager.ShowMessageBox<bool>("Body", "Caption");
        Assert.NotNull(vm);
        var firstVm = vm;

        // Assert — the real MessageBoxView was located and shown for the factory-created ViewModel
        var view = Assert.IsType<MessageBoxView>(vm.View);
        Assert.True(view.IsVisible);
        Assert.Same(vm, view.DataContext);

        vm.ButtonClicked(MessageBoxResult.OK);
        Assert.True(await okTask);

        // Act — a second message box gets a fresh ViewModel from the factory
        var cancelTask = manager.ShowMessageBox<bool>("Body 2", "Caption 2", MessageBoxButton.OKCancel);
        Assert.Equal(2, factoryCalls);
        Assert.NotSame(firstVm, vm);

        // Assert — clicking Cancel completes the dialog with a negative result
        vm.ButtonClicked(MessageBoxResult.Cancel);
        Assert.False(await cancelTask);
    }
}
