namespace Sonata.Avalonia;

/// <summary>
/// Responsible for managing views. Locates the correct view, instantiates it, attaches it to its ViewModel correctly, and handles the View.Model attached property
/// </summary>
public interface IViewManager
{
    /// <summary>
    /// Called by View whenever its current View.Model changes. Will locate and instantiate the correct view, and set it as the target's Content
    /// </summary>
    /// <param name="targetLocation">Thing which View.Model was changed on. Will have its Content set</param>
    /// <param name="oldValue">Previous value of View.Model</param>
    /// <param name="newValue">New value of View.Model</param>
    void OnModelChanged(AvaloniaObject targetLocation, object oldValue, object newValue);

    /// <summary>
    /// Given a ViewModel instance, locate its View type (using LocateViewForModel), and instantiates it
    /// </summary>
    /// <param name="model">ViewModel to locate and instantiate the View for</param>
    /// <returns>Instantiated and setup view</returns>
    Control CreateViewForModel(object model);

    /// <summary>
    /// Given an instance of a ViewModel and an instance of its View, bind the two together
    /// </summary>
    /// <param name="view">View to bind to the ViewModel</param>
    /// <param name="viewModel">ViewModel to bind the View to</param>
    void BindViewToModel(Control view, object viewModel);

    /// <summary>
    /// Create a View for the given ViewModel, and bind the two together, if the model doesn't already have a view
    /// </summary>
    /// <param name="model">ViewModel to create a Veiw for</param>
    /// <returns>Newly created View, bound to the given ViewModel</returns>
    Control CreateAndBindViewForModelIfNecessary(object model);
}

/// <summary>
/// Configuration object consumed by <see cref="ViewManager"/>
/// </summary>
public class ViewManagerConfig
{
    /// <summary>
    /// Gets the ViewFactory used to instantiate views.
    /// </summary>
    public required Func<Type, object> ViewFactory { get; init; }

    /// <summary>
    /// Gets the assemblies searched for views.
    /// </summary>
    public required List<Assembly> ViewAssemblies { get; init; }
}

/// <summary>
/// Default implementation of ViewManager. Responsible for locating, creating, and settings up Views. Also owns the View.Model and View.ActionTarget attached properties.
/// View location results are cached per ViewModel type: configure all conventions
/// (ViewAssemblies, NamespaceTransformations, suffixes) before the first lookup.
/// </summary>
public class ViewManager : IViewManager
{
    private readonly ILogger _logger;

    internal readonly ConcurrentDictionary<Type, Type> ViewTypeCache = new();

    private Func<Type, object> _viewFactory; // This is assigned by the ctor

    /// <summary>
    /// Gets or sets the delegate used to retrieve an instance of a view
    /// </summary>
    public Func<Type, object> ViewFactory
    {
        get { return _viewFactory; }
        set
        {
            if (value == null)
                throw new ArgumentNullException();
            _viewFactory = value;
        }
    }

    private List<Assembly> _viewAssemblies; // This is assigned by the ctor

    /// <summary>
    /// Gets or sets the assemblies which are used for IoC container auto-binding and searching for Views.
    /// </summary>
    public List<Assembly> ViewAssemblies
    {
        get { return _viewAssemblies; }
        set
        {
            if (value == null)
                throw new ArgumentNullException();
            _viewAssemblies = value;
        }
    }

    private Dictionary<string, string> _namespaceTransformations = new Dictionary<string, string>();

    /// <summary>
    /// Gets or sets a set of transformations to be applied to the ViewModel's namespace: string to find -> string to replace it with
    /// </summary>
    public Dictionary<string, string> NamespaceTransformations
    {
        get { return _namespaceTransformations; }
        set
        {
            if (value == null)
                throw new ArgumentNullException();
            _namespaceTransformations = value;
        }
    }

    private string _viewNameSuffix = "View";

    /// <summary>
    /// Gets or sets the suffix replacing 'ViewModel' (see <see cref="ViewModelNameSuffix"/>). Defaults to 'View'
    /// </summary>
    public string ViewNameSuffix
    {
        get { return _viewNameSuffix; }
        set
        {
            if (value == null)
                throw new ArgumentNullException();
            _viewNameSuffix = value;
        }
    }

    private string _viewModelNameSuffix = "ViewModel";

    /// <summary>
    /// Gets or sets the suffix of ViewModel names, defaults to 'ViewModel'. This will be replaced by <see cref="ViewNameSuffix"/>
    /// </summary>
    public string ViewModelNameSuffix
    {
        get { return _viewModelNameSuffix; }
        set
        {
            if (value == null)
                throw new ArgumentNullException();
            _viewModelNameSuffix = value;
        }
    }

    /// <summary>
    /// Initialises a new instance of the <see cref="ViewManager"/> class, with the given viewFactory
    /// </summary>
    /// <param name="config">Configuration object</param>
    /// <param name="logger">Logger to use</param>
    public ViewManager(ViewManagerConfig config, ILogger<ViewManager> logger)
    {
        ViewFactory = config.ViewFactory;
        ViewAssemblies = config.ViewAssemblies;
        _logger = logger;
    }

    /// <summary>
    /// Called by View whenever its current View.Model changes. Will locate and instantiate the correct view, and set it as the target's Content
    /// </summary>
    /// <param name="targetLocation">Thing which View.Model was changed on. Will have its Content set</param>
    /// <param name="oldValue">Previous value of View.Model</param>
    /// <param name="newValue">New value of View.Model</param>
    public virtual void OnModelChanged(AvaloniaObject targetLocation, object oldValue, object newValue)
    {
        if (oldValue == newValue)
            return;

        if (newValue != null)
        {
            _logger.LogInformation("View.Model changed for {0} from {1} to {2}", targetLocation, oldValue, newValue);
            var view = CreateAndBindViewForModelIfNecessary(newValue);
            if (view is Window)
            {
            var e = new SonataInvalidViewTypeException(@$"s:View.Model=""..."" tried to show a View of type '{view.GetType().Name}', but that View derives from the Window class. Make sure any Views you display using s:View.Model=""..."" do not derive from Window (use UserControl or similar)");
            _logger.LogError(e, "Located type is not a valid view");
            throw e;
            }
            View.SetContentProperty(targetLocation, view);
        }
        else
        {
            _logger.LogInformation("View.Model cleared for {0}, from {1}", targetLocation, oldValue);
            View.SetContentProperty(targetLocation, null);
        }
    }

    /// <summary>
    /// Create a View for the given ViewModel, and bind the two together, if the model doesn't already have a view
    /// </summary>
    /// <param name="model">ViewModel to create a Veiw for</param>
    /// <returns>Newly created View, bound to the given ViewModel</returns>
    public virtual Control CreateAndBindViewForModelIfNecessary(object model)
    {
        var modelAsViewAware = model as IViewAware;
        if (modelAsViewAware != null && modelAsViewAware.View != null)
        {
            _logger.LogInformation("ViewModel {0} already has a View attached to it. Not attaching another", model);
            return modelAsViewAware.View;
        }

        return CreateAndBindViewForModel(model);
    }

    /// <summary>
    /// Create a View for the given ViewModel, and bind the two together
    /// </summary>
    /// <param name="model">ViewModel to create a Veiw for</param>
    /// <returns>Newly created View, bound to the given ViewModel</returns>
    protected virtual Control CreateAndBindViewForModel(object model)
    {
        // Need to bind before we initialize the view
        // Otherwise e.g. the Command bindings get evaluated (by InitializeComponent) but the ActionTarget hasn't been set yet
        _logger.LogInformation("Instantiating and binding a new View to ViewModel {0}", model);
        var view = CreateViewForModel(model);
        BindViewToModel(view, model);
        return view;
    }

    /// <summary>
    /// Given the expected name for a view, locate its type (or return null if a suitable type couldn't be found)
    /// </summary>
    /// <param name="viewName">View name to locate the type for</param>
    /// <param name="extraAssemblies">Extra assemblies to search through</param>
    /// <returns>Type for that view name</returns>
    protected virtual Type ViewTypeForViewName(string viewName, IEnumerable<Assembly> extraAssemblies)
    {
        return ViewAssemblies.Concat(extraAssemblies).Select(x => x.GetType(viewName)).FirstOrDefault(x => x != null);
    }

    /// <summary>
    /// Given the full name of a ViewModel type, determine the corresponding View type nasme
    /// </summary>
    /// <remarks>
    /// This is used internally by LocateViewForModel. If you override LocateViewForModel, you
    /// can simply ignore this method.
    /// </remarks>
    /// <param name="modelTypeName">ViewModel type name to get the View type name for</param>
    /// <returns>View type name</returns>
    protected virtual string ViewTypeNameForModelTypeName(string modelTypeName)
    {
        string transformed = modelTypeName;

        foreach (var transformation in NamespaceTransformations)
        {
            if (transformed.StartsWith(transformation.Key + "."))
            {
                transformed = transformation.Value + transformed.Substring(transformation.Key.Length);
                break;
            }
        }

        transformed = Regex.Replace(transformed,
            string.Format(@"(?<=.){0}(?=s?\.)|{0}$", Regex.Escape(ViewModelNameSuffix)),
            Regex.Escape(ViewNameSuffix));

        return transformed;
    }

    /// <summary>
    /// Given the type of a model, locate the type of its View (or throw an exception)
    /// </summary>
    /// <param name="modelType">Model to find the view for</param>
    /// <returns>Type of the ViewModel's View</returns>
    /// <remarks>Results are cached per model type. Overriding this method replaces the cache entirely.</remarks>
    protected virtual Type LocateViewForModel(Type modelType)
    {
        return ViewTypeCache.GetOrAdd(modelType, mt =>
        {
            var modelName = mt.FullName;
            var viewName = ViewTypeNameForModelTypeName(modelName);
            if (modelName == viewName)
                throw new SonataViewLocationException(string.Format("Unable to transform ViewModel name {0} into a suitable View name", modelName), viewName);

            // Also include the ViewModel's assembly, to be helpful
            var viewType = ViewTypeForViewName(viewName, new[] { mt.Assembly });
            if (viewType == null)
            {
                var e = new SonataViewLocationException(string.Format("Unable to find a View with type {0}", viewName), viewName);
                _logger.LogError(e, "View location failed");
                throw e;
            }
            else
            {
                _logger.LogInformation("Searching for a View with name {0}, and found {1}", viewName, viewType);
            }

            return viewType;
        });
    }

    /// <summary>
    /// Given a ViewModel instance, locate its View type (using LocateViewForModel), and instantiates it
    /// </summary>
    /// <param name="model">ViewModel to locate and instantiate the View for</param>
    /// <returns>Instantiated and setup view</returns>
    public virtual Control CreateViewForModel(object model)
    {
        var viewType = LocateViewForModel(model.GetType());

        if (viewType.IsAbstract || !typeof(Control).IsAssignableFrom(viewType))
        {
            var e = new SonataViewLocationException(string.Format("Found type for view: {0}, but it wasn't a class derived from UIElement", viewType.Name), viewType.Name);
            _logger.LogError(e, "View location failed");
            throw e;
        }

        var view = (Control)ViewFactory(viewType);

        return view;
    }

    /// <summary>
    /// Given an instance of a ViewModel and an instance of its View, bind the two together
    /// </summary>
    /// <param name="view">View to bind to the ViewModel</param>
    /// <param name="viewModel">ViewModel to bind the View to</param>
    public virtual void BindViewToModel(Control view, object viewModel)
    {
        _logger.LogInformation("Setting {0}'s ActionTarget to {1}", view, viewModel);
        View.SetActionTarget(view, viewModel);

        var viewAsFrameworkElement = view as Control;
        if (viewAsFrameworkElement != null)
        {
            _logger.LogInformation("Setting {0}'s DataContext to {1}", view, viewModel);
            viewAsFrameworkElement.DataContext = viewModel;
        }

        var viewModelAsViewAware = viewModel as IViewAware;
        if (viewModelAsViewAware != null)
        {
            _logger.LogInformation("Setting {0}'s View to {1}", viewModel, view);
            viewModelAsViewAware.AttachView(view);
        }
    }
}

/// <summary>
/// Exception raised while attempting to locate a View for a ViewModel
/// </summary>
[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2237:MarkISerializableTypesWithSerializable")]
public class SonataViewLocationException : Exception
{
    /// <summary>
    /// Name of the View in question
    /// </summary>
    public readonly string ViewTypeName;

    /// <summary>
    /// Initialises a new instance of the <see cref="SonataViewLocationException"/> class
    /// </summary>
    /// <param name="message">Message associated with the Exception</param>
    /// <param name="viewTypeName">Name of the View this question was thrown for</param>
    public SonataViewLocationException(string message, string viewTypeName)
        : base(message)
    {
        ViewTypeName = viewTypeName;
    }
}

/// <summary>
/// Exception raise when the located View is of the wrong type (Window when expected UserControl, etc)
/// </summary>
[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2237:MarkISerializableTypesWithSerializable")]
public class SonataInvalidViewTypeException : Exception
{
    /// <summary>
    /// Initialises a new instance of the <see cref="SonataInvalidViewTypeException"/> class
    /// </summary>
    /// <param name="message">Message associated with the Exception</param>
    public SonataInvalidViewTypeException(string message)
        : base(message)
    { }
}
