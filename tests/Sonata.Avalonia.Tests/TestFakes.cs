using Avalonia.Controls;
using Microsoft.Extensions.DependencyInjection;
using Sonata.Avalonia;

namespace Sonata.Avalonia.Tests;

public class TestRootViewModel : Screen
{
}

public class TestRootView : UserControl
{
}

public class TestApp : SonataApplication<TestRootViewModel>
{
    public List<string> CallOrder { get; } = new();

    public IServiceProvider PublicServices => Services;

    protected override void ConfigureServices(IServiceCollection services)
    {
        CallOrder.Add("ConfigureServices");
    }

    protected override void ConfigureSonataServices(IServiceCollection services)
    {
        CallOrder.Add("ConfigureSonataServices");
        base.ConfigureSonataServices(services);
    }
}

public class TestAppNoConvention : SonataApplication<TestRootViewModel>
{
    public IServiceProvider? PublicServices
    {
        get
        {
            try { return Services; }
            catch (InvalidOperationException) { return null; }
        }
    }

    protected override bool EnableConventionRegistration => false;
}
