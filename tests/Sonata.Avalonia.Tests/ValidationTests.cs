using Sonata.Avalonia;
using Sonata.Avalonia.Internal;
using Xunit;

namespace Sonata.Avalonia.Tests;

[Collection("Ambient")]
public class ValidationTests
{
    public ValidationTests()
    {
        // Ensure validation property-change notifications run synchronously in unit tests.
        UiThreadDispatch.Dispatcher = SynchronousDispatcher.Instance;
    }

    [Fact]
    public async Task ValidateAsync_RecordsErrors_RaisesErrorsChanged()
    {
        var validator = new TestValidator();
        validator.Errors["Name"] = new[] { "Required" };
        var vm = new ValidatingScreen(validator);
        var errorsChanged = 0;
        vm.ErrorsChanged += (o, e) => errorsChanged++;

        var result = await vm.ValidateAsync();

        Assert.False(result);
        Assert.True(vm.HasErrors);
        Assert.True(errorsChanged > 0);
    }

    [Fact]
    public void AutoValidate_PropertyChange_TriggersValidation()
    {
        var validator = new TestValidator();
        validator.Errors["Name"] = new[] { "Required" };
        var vm = new ValidatingScreen(validator);

        vm.Name = "x";

        Assert.Equal("Name", validator.LastValidatedProperty);
        Assert.True(vm.HasErrors);
    }

    [Fact]
    public void RecordPropertyError_And_ClearAllPropertyErrors()
    {
        var vm = new ValidatingScreen(null);

        vm.RecordError("Name", new[] { "Required" });
        Assert.True(vm.HasErrors);

        vm.ClearErrors();
        Assert.False(vm.HasErrors);
    }
}
