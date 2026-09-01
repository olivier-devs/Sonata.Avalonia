namespace Sonata.Avalonia;

/// <summary>
/// Configuration object consumed by <see cref="ViewManager"/>.
/// This object is registered as an options instance and is typically configured
/// through <see cref="ViewManagerServiceCollectionExtensions.ConfigureViewManager"/>.
/// All configuration must be performed before the first view lookup, as results are cached.
/// </summary>
public sealed class ViewManagerConfig
{
    private readonly List<Assembly> _viewAssemblies = new();
    private readonly Dictionary<string, string> _namespaceTransformations = new(StringComparer.Ordinal);
    private readonly Dictionary<Type, Type> _explicitViewMappings = new();

    /// <summary>
    /// Gets the factory used to instantiate views from their located types.
    /// </summary>
    public Func<Type, object>? ViewFactory { get; private set; }

    /// <summary>
    /// Gets the assemblies searched for views.
    /// </summary>
    public IReadOnlyList<Assembly> ViewAssemblies => _viewAssemblies;

    /// <summary>
    /// Gets a read-only view of the namespace transformations applied to ViewModel type names.
    /// </summary>
    public IReadOnlyDictionary<string, string> NamespaceTransformations => _namespaceTransformations;

    /// <summary>
    /// Gets the explicit view-to-ViewModel mappings.
    /// </summary>
    public IReadOnlyDictionary<Type, Type> ExplicitViewMappings => _explicitViewMappings;

    /// <summary>
    /// The default value for <see cref="ViewNameSuffix"/>.
    /// </summary>
    public const string DefaultViewNameSuffix = "View";

    /// <summary>
    /// The default value for <see cref="ViewModelNameSuffix"/>.
    /// </summary>
    public const string DefaultViewModelNameSuffix = "ViewModel";

    /// <summary>
    /// Gets the suffix replacing <see cref="ViewModelNameSuffix"/>. Defaults to <see cref="DefaultViewNameSuffix"/>.
    /// </summary>
    public string ViewNameSuffix { get; private set; } = DefaultViewNameSuffix;

    /// <summary>
    /// Gets the suffix of ViewModel names. Defaults to <see cref="DefaultViewModelNameSuffix"/>.
    /// </summary>
    public string ViewModelNameSuffix { get; private set; } = DefaultViewModelNameSuffix;

    /// <summary>
    /// Sets the factory used to instantiate views from their located types.
    /// </summary>
    /// <param name="viewFactory">Factory delegate to use.</param>
    /// <returns>The current <see cref="ViewManagerConfig"/> instance.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="viewFactory"/> is null.</exception>
    public ViewManagerConfig SetViewFactory(Func<Type, object> viewFactory)
    {
        ArgumentNullException.ThrowIfNull(viewFactory);
        ViewFactory = viewFactory;
        return this;
    }

    /// <summary>
    /// Sets the suffix used for view type names.
    /// </summary>
    /// <param name="viewNameSuffix">Suffix to use. Must be non-empty.</param>
    /// <returns>The current <see cref="ViewManagerConfig"/> instance.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="viewNameSuffix"/> is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="viewNameSuffix"/> is empty or whitespace.</exception>
    public ViewManagerConfig SetViewNameSuffix(string viewNameSuffix)
    {
        ThrowIfNullOrWhiteSpace(viewNameSuffix, nameof(viewNameSuffix));
        ViewNameSuffix = viewNameSuffix;
        return this;
    }

    /// <summary>
    /// Sets the suffix used to identify ViewModel type names.
    /// </summary>
    /// <param name="viewModelNameSuffix">Suffix to use. Must be non-empty.</param>
    /// <returns>The current <see cref="ViewManagerConfig"/> instance.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="viewModelNameSuffix"/> is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="viewModelNameSuffix"/> is empty or whitespace.</exception>
    public ViewManagerConfig SetViewModelNameSuffix(string viewModelNameSuffix)
    {
        ThrowIfNullOrWhiteSpace(viewModelNameSuffix, nameof(viewModelNameSuffix));
        ViewModelNameSuffix = viewModelNameSuffix;
        return this;
    }

    /// <summary>
    /// Adds the assembly containing <typeparamref name="T"/> to the list of assemblies searched for views.
    /// </summary>
    /// <typeparam name="T">A type whose assembly will be searched.</typeparam>
    /// <returns>The current <see cref="ViewManagerConfig"/> instance.</returns>
    public ViewManagerConfig AddViewAssembly<T>() => AddViewAssembly(typeof(T).Assembly);

    /// <summary>
    /// Adds the specified assembly to the list of assemblies searched for views.
    /// Duplicate assemblies are ignored.
    /// </summary>
    /// <param name="assembly">Assembly to add.</param>
    /// <returns>The current <see cref="ViewManagerConfig"/> instance.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="assembly"/> is null.</exception>
    public ViewManagerConfig AddViewAssembly(Assembly assembly)
    {
        ArgumentNullException.ThrowIfNull(assembly);
        if (!_viewAssemblies.Contains(assembly))
            _viewAssemblies.Add(assembly);
        return this;
    }

    /// <summary>
    /// Adds a namespace transformation: ViewModel type names starting with
    /// <paramref name="viewModelNamespace"/> followed by a period are rewritten to start
    /// with <paramref name="viewNamespace"/>.
    /// </summary>
    /// <param name="viewModelNamespace">Namespace prefix in ViewModel type names.</param>
    /// <param name="viewNamespace">Namespace prefix to replace it with.</param>
    /// <returns>The current <see cref="ViewManagerConfig"/> instance.</returns>
    /// <exception cref="ArgumentNullException">Either argument is null.</exception>
    /// <exception cref="ArgumentException">Either argument is empty or whitespace.</exception>
    public ViewManagerConfig MapNamespace(string viewModelNamespace, string viewNamespace)
    {
        ThrowIfNullOrWhiteSpace(viewModelNamespace, nameof(viewModelNamespace));
        ThrowIfNullOrWhiteSpace(viewNamespace, nameof(viewNamespace));
        _namespaceTransformations[viewModelNamespace] = viewNamespace;
        return this;
    }

    /// <summary>
    /// Maps a ViewModel type to a view type explicitly, bypassing convention-based location.
    /// If the same ViewModel type is mapped multiple times, the last registration wins.
    /// The view type is validated to be a non-abstract class derived from <see cref="Control"/>.
    /// </summary>
    /// <typeparam name="TView">View type to map.</typeparam>
    /// <typeparam name="TViewModel">ViewModel type to map.</typeparam>
    /// <returns>The current <see cref="ViewManagerConfig"/> instance.</returns>
    /// <exception cref="ArgumentException"><typeparamref name="TView"/> is not a valid view type.</exception>
    public ViewManagerConfig AddView<TView, TViewModel>() => AddView(typeof(TView), typeof(TViewModel));

    /// <summary>
    /// Maps a ViewModel type to a view type explicitly, bypassing convention-based location.
    /// If the same ViewModel type is mapped multiple times, last registration wins.
    /// The view type is validated to be a non-abstract class derived from <see cref="Control"/>.
    /// </summary>
    /// <param name="viewType">View type to map.</param>
    /// <param name="viewModelType">ViewModel type to map.</param>
    /// <returns>The current <see cref="ViewManagerConfig"/> instance.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="viewType"/> or <paramref name="viewModelType"/> is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="viewType"/> is not a non-abstract class derived from <see cref="Control"/>.</exception>
    public ViewManagerConfig AddView(Type viewType, Type viewModelType)
    {
        ArgumentNullException.ThrowIfNull(viewType);
        ArgumentNullException.ThrowIfNull(viewModelType);
        ValidateViewType(viewType);
        _explicitViewMappings[viewModelType] = viewType;
        return this;
    }

    /// <summary>
    /// Removes the explicit view mapping for <typeparamref name="TViewModel"/> if one exists.
    /// Does nothing if no mapping exists.
    /// </summary>
    /// <typeparam name="TViewModel">ViewModel type whose mapping should be removed.</typeparam>
    /// <returns>The current <see cref="ViewManagerConfig"/> instance.</returns>
    public ViewManagerConfig RemoveView<TViewModel>() => RemoveView(typeof(TViewModel));

    /// <summary>
    /// Removes the explicit view mapping for <paramref name="viewModelType"/> if one exists.
    /// Does nothing if no mapping exists.
    /// </summary>
    /// <param name="viewModelType">ViewModel type whose mapping should be removed.</param>
    /// <returns>The current <see cref="ViewManagerConfig"/> instance.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="viewModelType"/> is null.</exception>
    public ViewManagerConfig RemoveView(Type viewModelType)
    {
        ArgumentNullException.ThrowIfNull(viewModelType);
        _explicitViewMappings.Remove(viewModelType);
        return this;
    }

    private static void ValidateViewType(Type viewType)
    {
        ArgumentNullException.ThrowIfNull(viewType);
        if (!typeof(Control).IsAssignableFrom(viewType))
            throw new ArgumentException($"The view type '{viewType.FullName}' must derive from '{typeof(Control).FullName}'.", nameof(viewType));
        if (viewType.IsAbstract)
            throw new ArgumentException($"The view type '{viewType.FullName}' must not be abstract.", nameof(viewType));
    }

    private static void ThrowIfNullOrWhiteSpace(string? value, string paramName)
    {
        ArgumentNullException.ThrowIfNull(value, paramName);
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Value cannot be empty or whitespace.", paramName);
    }
}
