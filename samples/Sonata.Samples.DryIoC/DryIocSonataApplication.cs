using System.Reflection;
using Avalonia.Controls;
using DryIoc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Sonata.Avalonia;
using Sonata.Avalonia.Primitive;

namespace Sonata.Samples.DryIoC;

public class DryIocSonataApplication<T> : SonataApplicationBase<T> where T : class
{
    private readonly IContainer _container = new Container();

    protected sealed override void Configure()
    {
        ConfigureIoC(_container);
        AutoRegister();
    }

    protected override object GetInstance(Type type) => _container.Resolve(type);

    protected override object GetInstance(Type service, string? key) => _container.Resolve(service, serviceKey: key);

    protected override IEnumerable<object> GetInstances(Type service) => _container.ResolveMany(service);

    protected override ILoggerFactory? GetLoggerFactory() => _container.Resolve<ILoggerFactory>();

    protected virtual void ConfigureIoC(IContainer container)
    {
        var viewManagerConfig = new ViewManagerConfig
        {
            ViewFactory = GetInstance,
            ViewAssemblies = new List<Assembly> { GetType().Assembly }
        };
        container.RegisterInstance(viewManagerConfig);
        container.RegisterInstance<ILoggerFactory>(NullLoggerFactory.Instance);
        container.Register(typeof(ILogger<>), typeof(Logger<>));
        container.Register<IViewManager, ViewManager>();
        container.RegisterInstance<IWindowManagerConfig>(this);
        container.Register<IWindowManager, WindowManager>();
        container.Register<IEventAggregator, EventAggregator>();
        container.Register<IMessageBoxViewModel, MessageBoxViewModel>();
        container.Register<MessageBoxView>();
    }

    private void AutoRegister()
    {
        foreach (var type in GetType().Assembly.GetTypes())
        {
            if (type.IsAbstract || type.IsInterface)
                continue;

            if (typeof(Control).IsAssignableFrom(type) || type.Name.EndsWith("ViewModel"))
                _container.Register(type, type);
        }
    }

    public override void Dispose()
    {
        base.Dispose();
        _container.Dispose();
    }
}
