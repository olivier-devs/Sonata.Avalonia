using Sonata.Avalonia.Primitive;
using Xunit;

namespace Sonata.Avalonia.Tests;

[Collection("Ambient")]
public class MessageBoxViewModelTests
{
    [Fact]
    public void Setup_NullCaption_DisplayNameIsEmpty()
    {
        var vm = new MessageBoxViewModel();

        vm.Setup("text", caption: null);

        Assert.Equal(string.Empty, vm.DisplayName);
    }
}
