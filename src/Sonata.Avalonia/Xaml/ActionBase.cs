namespace Sonata.Avalonia.Xaml;

/// <summary>
/// Common base class for CommandAction and EventAction
/// </summary>
public abstract class ActionBase : AvaloniaObject
{
    private readonly ILogger _logger;
    private readonly IReadOnlyList<ActionParameter>? _inlineParameters;

    /// <summary>
    /// Gets the View to grab the View.ActionTarget from
    /// </summary>
    public AvaloniaObject? Subject { get; private set; }

    /// <summary>
    /// Gets the method name. E.g. if someone's gone Buttom Command="{s:Action MyMethod}", this is MyMethod.
    /// </summary>
    public string MethodName { get; private set; }

    /// <summary>
    /// Gets the MethodInfo for the method to call. This has to exist, or we throw a wobbly
    /// </summary>
    protected MethodInfo? TargetMethodInfo { get; private set; }

    /// <summary>
    /// Behaviour for if the target is null
    /// </summary>
    protected readonly ActionUnavailableBehaviour TargetNullBehaviour;

    /// <summary>
    /// Behaviour for if the action doesn't exist on the target
    /// </summary>
    protected readonly ActionUnavailableBehaviour ActionNonExistentBehaviour;

    /// <summary>
    /// Gets the object on which methods will be invoked
    /// </summary>
    public object? Target
    {
        get => GetValue(targetProperty);
        private set => SetValue(targetProperty, value);
    }

    private static readonly StyledProperty<object?> targetProperty;


    static ActionBase()
    {
        targetProperty = AvaloniaProperty.Register<ActionBase, object?>(nameof(Target));
        targetProperty.Changed.Subscribe(e =>
        {
            ((ActionBase)e.Sender).UpdateActionTarget(e.OldValue, e.NewValue);
        });
    }


    /// <summary>
    /// Initialises a new instance of the <see cref="ActionBase"/> class to use <see cref="View.ActionTargetProperty"/> to get the target
    /// </summary>
    /// <param name="subject">View to grab the View.ActionTarget from</param>
    /// <param name="backupSubject">Backup subject to use if no ActionTarget could be retrieved from the subject</param>
    /// <param name="methodName">Method name. the MyMethod in Buttom Command="{s:Action MyMethod}".</param>
    /// <param name="targetNullBehaviour">Behaviour for it the relevant View.ActionTarget is null</param>
    /// <param name="actionNonExistentBehaviour">Behaviour for if the action doesn't exist on the View.ActionTarget</param>
    /// <param name="logger">Logger to use</param>
    public ActionBase(AvaloniaObject subject, AvaloniaObject? backupSubject, string methodName, ActionUnavailableBehaviour targetNullBehaviour, ActionUnavailableBehaviour actionNonExistentBehaviour, ILogger logger, IReadOnlyList<ActionParameter>? parameters = null)
        : this(methodName, targetNullBehaviour, actionNonExistentBehaviour, logger, parameters)
    {
        Subject = subject;

        // If a 'backupSubject' was given, observe both that and 'subject' for View.ActionTarget changes,
        // picking the subject's target when available. If it wasn't given, just observe the subject.

        Target = Subject.GetValue(View.ActionTargetProperty);
        Subject.GetObservable(View.ActionTargetProperty).Subscribe(e => Target = e);

        if (backupSubject != null)
        {
            backupSubject.GetObservable(View.ActionTargetProperty).Subscribe(e => Target = e);
        }
    }

    /// <summary>
    /// Initialises a new instance of the <see cref="ActionBase"/> class to use an explicit target
    /// </summary>
    /// <param name="target">Target to find the method on</param>
    /// <param name="methodName">Method name. the MyMethod in Buttom Command="{s:Action MyMethod}".</param>
    /// <param name="targetNullBehaviour">Behaviour for it the relevant View.ActionTarget is null</param>
    /// <param name="actionNonExistentBehaviour">Behaviour for if the action doesn't exist on the View.ActionTarget</param>
    /// <param name="logger">Logger to use</param>
    public ActionBase(object target, string methodName, ActionUnavailableBehaviour targetNullBehaviour, ActionUnavailableBehaviour actionNonExistentBehaviour, ILogger logger, IReadOnlyList<ActionParameter>? parameters = null)
        : this(methodName, targetNullBehaviour, actionNonExistentBehaviour, logger, parameters)
    {
        Target = target ?? throw new ArgumentNullException(nameof(target));
    }

    private ActionBase(string methodName, ActionUnavailableBehaviour targetNullBehaviour, ActionUnavailableBehaviour actionNonExistentBehaviour, ILogger logger, IReadOnlyList<ActionParameter>? parameters)
    {
        MethodName = methodName ?? throw new ArgumentNullException(nameof(methodName));
        TargetNullBehaviour = targetNullBehaviour;
        ActionNonExistentBehaviour = actionNonExistentBehaviour;
        _logger = logger;
        _inlineParameters = parameters;
    }

    private void UpdateActionTarget(object? oldTarget, object? newTarget)
    {
        MethodInfo? targetMethodInfo = null;

        // If it's being set to the initial value, ignore it
        // At this point, we're executing the View's InitializeComponent method, and the ActionTarget hasn't yet been assigned
        // If they've opted to throw if the target is null, then this will cause that exception.
        // We'll just wait until the ActionTarget is assigned, and we're called again
        if (newTarget == View.InitialActionTarget)
            return;

        if (newTarget == null)
        {
            // If it's Enable or Disable we don't do anything - CanExecute will handle this
            if (TargetNullBehaviour == ActionUnavailableBehaviour.Throw)
            {
                var e = new ActionTargetNullException(string.Format("ActionTarget on element {0} is null (method name is {1})", Subject, MethodName));
                _logger.LogError(e, "ActionTarget is null");
                throw e;
            }
            else
            {
                _logger.LogInformation("ActionTarget on element {0} is null (method name is {1}), but NullTarget is not Throw, so carrying on", Subject, MethodName);
            }
        }
        else
        {
            BindingFlags bindingFlags;
            Type newTargetType;
            if (newTarget is Type newTargetTypeValue)
            {
                bindingFlags = BindingFlags.Public | BindingFlags.Static;
                newTargetType = newTargetTypeValue;
            }
            else
            {
                newTargetType = newTarget.GetType();

                var info = newTarget.GetType().GetTypeInfo();


                bindingFlags = BindingFlags.Public | BindingFlags.Instance;
            }
            if (!HasParameters)
            {
                try
                {
                    targetMethodInfo = newTargetType.GetMethod(MethodName, bindingFlags);

                    if (targetMethodInfo == null)
                    {
                        var target = Target ?? throw new InvalidOperationException("Target was unexpectedly null while resolving the action method");
                        var t = target.GetType();
                        targetMethodInfo = t.GetMethod(MethodName, bindingFlags);
                        if (targetMethodInfo == null)
                            _logger.LogWarning("Unable to find{0} method {1} on {2}", newTarget is Type ? " static" : "", MethodName, newTargetType.Name);
                    }
                    else
                        AssertTargetMethodInfo(targetMethodInfo, newTargetType);
                }
                catch (AmbiguousMatchException e)
                {
                    var ex = new AmbiguousMatchException(string.Format("Ambiguous match for {0} method on {1}", MethodName, newTargetType.Name), e);
                    _logger.LogError(ex, "Ambiguous method match");
                    throw ex;
                }
            }
        }

        TargetMethodInfo = targetMethodInfo;

        OnTargetChanged(oldTarget, newTarget);
    }

    /// <summary>
    /// Invoked when a new non-null target is set, which has non-null MethodInfo. Used to assert that the method signature is correct
    /// </summary>
    /// <param name="targetMethodInfo">MethodInfo of method on new target</param>
    /// <param name="newTargetType">Type of new target</param>
    private protected abstract void AssertTargetMethodInfo(MethodInfo targetMethodInfo, Type newTargetType);

    /// <summary>
    /// Invoked when a new target is set, after all other action has been taken
    /// </summary>
    /// <param name="oldTarget">Previous target</param>
    /// <param name="newTarget">New target</param>
    private protected virtual void OnTargetChanged(object? oldTarget, object? newTarget) { }

    /// <summary>
    /// Assert that the target is not View.InitialActionTarget
    /// </summary>
    private protected void AssertTargetSet()
    {
        // If we've made it this far and the target is still the default, then something's wrong
        // Make sure they know
        if (Target == View.InitialActionTarget)
        {
            var ex = new ActionNotSetException(string.Format("View.ActionTarget not set on control {0} (method {1}). " +
                                                             "This probably means the control hasn't inherited it from a parent, e.g. because a ContextMenu or Popup sits in the visual tree. " +
                                                             "You will need so set 's:View.ActionTarget' explicitly. See the wiki section \"Actions\" for more details.", Subject, MethodName));
            _logger.LogError(ex, "View.ActionTarget not set");
            throw ex;
        }

        if (HasParameters)
            return;

        if (TargetMethodInfo == null && ActionNonExistentBehaviour == ActionUnavailableBehaviour.Throw)
        {
            var ex = new ActionNotFoundException(string.Format("Unable to find method {0} on {1}", MethodName, TargetName()));
            _logger.LogError(ex, "Action not found");
            throw ex;
        }
    }

    /// <summary>
    /// Effective parameters: the attached <c>s:Action.Parameters</c> collection when present,
    /// otherwise the inline parameters parsed from the compact syntax.
    /// Reads the RAW attached value (not the lazy <see cref="Action.GetParameters"/>) to avoid
    /// creating an empty collection as a side effect on the CanExecute hot path.
    /// </summary>
    private protected IReadOnlyList<ActionParameter> EffectiveParameters
    {
        get
        {
            if (Subject is Control c && c.GetValue(Action.ParametersProperty) is { Count: > 0 } attached)
                return attached;
            return _inlineParameters ?? Array.Empty<ActionParameter>();
        }
    }

    /// <summary>True when the action should resolve and invoke with declared parameters.</summary>
    private protected bool HasParameters => EffectiveParameters.Count > 0;

    /// <summary>Computes the BindingFlags for the current target, mirroring the eager path.</summary>
    private protected BindingFlags GetBindingFlags()
        => Target is Type ? BindingFlags.Public | BindingFlags.Static : BindingFlags.Public | BindingFlags.Instance;

    /// <summary>Builds an execution context from the current target and subject.</summary>
    private protected ActionExecutionContext CreateExecutionContext(object? eventArgs)
    {
        return new ActionExecutionContext
        {
            Target = Target,
            Source = Subject,
            DataContext = Subject is Control c ? c.DataContext : null,
            EventArgs = eventArgs,
        };
    }

    /// <summary>Resolves the current argument values from the declared parameters, or null when parameterless.</summary>
    private protected object?[]? ResolveArguments(ActionExecutionContext context)
    {
        var parameters = EffectiveParameters;
        if (parameters.Count == 0)
            return null;

        var values = new object?[parameters.Count];
        for (var i = 0; i < parameters.Count; i++)
            values[i] = parameters[i].GetValue(context);

        return values;
    }

    /// <summary>Resolves the method to invoke for the parameterized path, or null if not found.</summary>
    private protected MethodInfo? ResolveParameterizedMethod(object?[] values)
        => Target == null
            ? null
            : ActionMethodResolver.Instance.Resolve(
                Target is Type t ? t : Target.GetType(), MethodName, values, GetBindingFlags());

    /// <summary>
    /// Coerces the resolved argument values to the method's parameter types (minimal literal
    /// conversion) and invokes the method, observing any returned Task.
    /// </summary>
    private protected void InvokeParameterized(MethodInfo method, object?[] values)
    {
        var parameters = method.GetParameters();
        var coerced = new object?[values.Length];
        for (var i = 0; i < values.Length; i++)
            coerced[i] = ActionMethodResolver.TryConvert(values[i], parameters[i].ParameterType, out var converted)
                ? converted
                : values[i];

        InvokeTargetMethod(method, coerced);
    }

    /// <summary>
    /// Invoke the target method with the given parameters
    /// </summary>
    /// <param name="parameters">Parameters to pass to the target method</param>
    private protected void InvokeTargetMethod(object?[]? parameters)
    {
        if (TargetMethodInfo == null)
            return;
        InvokeTargetMethod(TargetMethodInfo, parameters);
    }

    private protected void InvokeTargetMethod(MethodInfo method, object?[]? parameters)
    {
        _logger.LogInformation("Invoking method {0} on {1} with parameters ({2})", MethodName, TargetName(), parameters == null ? "none" : string.Join(", ", parameters));

        try
        {
            var target = method.IsStatic ? null : Target;
            var result = method.Invoke(target, parameters);
            // Observe the task so exceptions are logged, not swallowed silently
            if (result is Task task)
            {
                FireAndForget.Run(task, _logger);
            }
        }
        catch (TargetInvocationException e)
        {
            _logger.LogError(e.InnerException, string.Format("Failed to invoke method {0} on {1} with parameters ({2})", MethodName, TargetName(), parameters == null ? "none" : string.Join(", ", parameters)));
            ExceptionDispatchInfo.Capture(e.InnerException ?? e).Throw();
        }
    }

    private string TargetName()
    {
        if (Target is Type t)
            return $"static target {t.Name}";

        var targetName = Target?.GetType().Name ?? "(null)";
        return $"target {targetName}";
    }

    private class MultiBindingToActionTargetConverter : IMultiValueConverter
    {
        public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
        {
            Debug.Assert(values.Count == 2);

            if (values[0] != View.InitialActionTarget)
                return values[0];

            if (values[1] != View.InitialActionTarget)
                return values[1];

            return View.InitialActionTarget;
        }
    }
}
