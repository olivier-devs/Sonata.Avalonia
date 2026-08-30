using Avalonia.Controls;
using Microsoft.Extensions.Logging;
using Sonata.Avalonia;
using System.Reflection;

namespace Sonata.Samples.OverridingViewManager;

[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
sealed class ViewModelAttribute : Attribute
{
    public Type ViewModel { get; }

    public ViewModelAttribute(Type viewModel)
    {
        ViewModel = viewModel;
    }
}

public class CustomViewManager : ViewManager
{
    private readonly Dictionary<Type, Type> _viewModelToViewMapping;

    public CustomViewManager(ViewManagerConfig config, ILogger<ViewManager> logger)
        : base(config, logger)
    {
        var mappings = from type in ViewAssemblies.SelectMany(x => x.GetExportedTypes())
            let attribute = type.GetCustomAttribute<ViewModelAttribute>()
            where attribute != null && typeof(Control).IsAssignableFrom(type)
            select new { View = type, ViewModel = attribute.ViewModel };

        _viewModelToViewMapping = mappings.ToDictionary(x => x.ViewModel, x => x.View);
    }

    protected override Type LocateViewForModel(Type modelType)
    {
        return _viewModelToViewMapping.TryGetValue(modelType, out var view) ? view : base.LocateViewForModel(modelType);
    }
}
