namespace Sonata.Avalonia.Xaml;

/// <summary>
/// ICommand returned by ActionExtension for binding buttons, etc, to methods on a ViewModel.
/// If the method has a parameter, CommandParameter is passed
/// </summary>
/// <remarks>
/// Watches the current View.ActionTarget, and looks for a method with the given name, calling it when the ICommand is called.
/// If a bool property with name Get(methodName) exists, it will be observed and used to enable/disable the ICommand.
/// </remarks>
public class CommandAction : ActionBase, ICommand
{
    private static ILogger Logger => SonataLogManager.GetLogger(typeof(CommandAction));

    /// <summary>
    /// Generated accessor to grab the value of the guard property, or null if there is none
    /// </summary>
    private Func<bool>? guardPropertyGetter;

    private bool _parameterSubscriptionsCreated;
    private bool _mixedGuardWarningLogged;

    /// <summary>
    /// Initialises a new instance of the <see cref="CommandAction"/> class to use <see cref="View.ActionTargetProperty"/> to get the target
    /// </summary>
    /// <param name="subject">View to grab the View.ActionTarget from</param>
    /// <param name="backupSubject">Backup subject to use if no ActionTarget could be retrieved from the subject</param>
    /// <param name="methodName">Method name. the MyMethod in Buttom Command="{s:Action MyMethod}".</param>
    /// <param name="targetNullBehaviour">Behaviour for it the relevant View.ActionTarget is null</param>
    /// <param name="actionNonExistentBehaviour">Behaviour for if the action doesn't exist on the View.ActionTarget</param>
    public CommandAction(AvaloniaObject subject, AvaloniaObject? backupSubject, string methodName, ActionUnavailableBehaviour targetNullBehaviour, ActionUnavailableBehaviour actionNonExistentBehaviour, IReadOnlyList<ActionParameter>? parameters = null)
        : base(subject, backupSubject, methodName, targetNullBehaviour, actionNonExistentBehaviour, Logger, parameters)
    { }

    /// <summary>
    /// Initialises a new instance of the <see cref="CommandAction"/> class to use an explicit target
    /// </summary>
    /// <param name="target">Target to find the method on</param>
    /// <param name="methodName">Method name. the MyMethod in Buttom Command="{s:Action MyMethod}".</param>
    /// <param name="targetNullBehaviour">Behaviour for it the relevant View.ActionTarget is null</param>
    /// <param name="actionNonExistentBehaviour">Behaviour for if the action doesn't exist on the View.ActionTarget</param>
    public CommandAction(object target, string methodName, ActionUnavailableBehaviour targetNullBehaviour, ActionUnavailableBehaviour actionNonExistentBehaviour, IReadOnlyList<ActionParameter>? parameters = null)
        : base(target, methodName, targetNullBehaviour, actionNonExistentBehaviour, Logger, parameters)
    { }

    private string GuardName => "Can" + MethodName;

    /// <summary>
    /// Invoked when a new non-null target is set, which has non-null MethodInfo. Used to assert that the method signature is correct
    /// </summary>
    /// <param name="targetMethodInfo">MethodInfo of method on new target</param>
    /// <param name="newTargetType">Type of new target</param>
    private protected override void AssertTargetMethodInfo(MethodInfo targetMethodInfo, Type newTargetType)
    {
        var methodParameters = targetMethodInfo.GetParameters();
        if (methodParameters.Length > 1)
        {
            var e = new ActionSignatureInvalidException(string.Format("Method {0} on {1} must have zero or one parameters", MethodName, newTargetType.Name));
            Logger.LogError(e, "Invalid command action signature");
            throw e;
        }
    }

    /// <summary>
    /// Invoked when a new target is set, after all other action has been taken
    /// </summary>
    /// <param name="oldTarget">Previous target</param>
    /// <param name="newTarget">New target</param>
    private protected override void OnTargetChanged(object? oldTarget, object? newTarget)
    {
        if (oldTarget is INotifyPropertyChanged oldInpc)
        {
            // PropertyChangedEventManager.RemoveHandler(oldInpc, this.PropertyChangedHandler, this.GuardName);
            PropertyChangedWeakEventManager.RemoveHandler(oldInpc, PropertyChangedHandler);
        }

        guardPropertyGetter = null;
        _mixedGuardWarningLogged = false;
        newTarget = Target;
        var guardPropertyInfo = newTarget?.GetType().GetProperty(GuardName);
        if (guardPropertyInfo != null)
        {
            if (guardPropertyInfo.PropertyType == typeof(bool))
            {
                var targetExpression = Expression.Constant(newTarget);
                var propertyAccess = Expression.Property(targetExpression, guardPropertyInfo);
                guardPropertyGetter = Expression.Lambda<Func<bool>>(propertyAccess).Compile();
            }
            else
            {
                Logger.LogWarning("Found guard property {0} for action {1} on target {2}, but its return type wasn't bool. Therefore, ignoring", GuardName, MethodName, newTarget);
            }
        }

        if (guardPropertyGetter != null)
        {
            if (newTarget is INotifyPropertyChanged inpc)
            {
                // PropertyChangedEventManager.AddHandler(inpc, this.PropertyChangedHandler, this.GuardName);
                PropertyChangedWeakEventManager.AddHandler(inpc, PropertyChangedHandler);
            }
            else
                Logger.LogWarning("Found guard property {0} for action {1} on target {2}, but the target doesn't implement INotifyPropertyChanged, so changes won't be observed", GuardName, MethodName, newTarget);
        }

        EnsureParameterSubscriptions();
        UpdateCanExecute();
    }

    private void EnsureParameterSubscriptions()
    {
        if (_parameterSubscriptionsCreated)
            return;
        _parameterSubscriptionsCreated = true;

        if (EffectiveParameters is not { Count: > 0 } parameters)
            return;

        foreach (var parameter in parameters.OfType<Parameter>())
            parameter.GetObservable(Parameter.ValueProperty).Subscribe(_ => UpdateCanExecute());
    }

    private void PropertyChangedHandler(object? sender, PropertyChangedEventArgs e)
    {
        UpdateCanExecute();
    }

    private void UpdateCanExecute()
    {
        var handler = CanExecuteChanged;
        // So. While we're safe firing PropertyChanged events on a non-UI thread, we
        // are not safe firing CanExecuteChanged events on other threads...
        // Therefore make sure we're on the UI thread
        if (handler != null)
            UiThreadDispatch.OnUIThread(() => handler(this, EventArgs.Empty));
    }

    /// <summary>
    /// Defines the method that determines whether the command can execute in its current state.
    /// </summary>
    /// <param name="parameter">Data used by the command. If the command does not require data to be passed, this object can be set to null.</param>
    /// <returns>true if this command can be executed; otherwise, false.</returns>
    public bool CanExecute(object? parameter)
    {
        if (Target == View.InitialActionTarget)
            return true;

        if (Target == null)
            return TargetNullBehaviour != ActionUnavailableBehaviour.Disable;

        if (HasParameters)
        {
            var context = CreateExecutionContext(null);
            var values = ResolveArguments(context) ?? Array.Empty<object?>();
            var method = ResolveParameterizedMethod(values);
            if (method == null)
                return ActionNonExistentBehaviour != ActionUnavailableBehaviour.Disable;

            var guardMethod = ResolveGuardMethod(method, values);
            if (guardMethod != null)
            {
                if (guardPropertyGetter != null && !_mixedGuardWarningLogged)
                {
                    Logger.LogWarning("Found both a guard method {GuardMethod} and a guard property {GuardProperty} for action {Action}; the method guard wins", GuardName, GuardName, MethodName);
                    _mixedGuardWarningLogged = true;
                }
                return InvokeGuard(guardMethod, values);
            }

            return guardPropertyGetter?.Invoke() ?? true;
        }

        if (TargetMethodInfo == null)
            return ActionNonExistentBehaviour != ActionUnavailableBehaviour.Disable;

        if (TargetMethodInfo.GetParameters().Length == 1)
        {
            var guardMethod = ResolveGuardMethod(TargetMethodInfo, new[] { parameter });
            if (guardMethod != null)
            {
                if (guardPropertyGetter != null && !_mixedGuardWarningLogged)
                {
                    Logger.LogWarning("Found both a guard method {GuardMethod} and a guard property {GuardProperty} for action {Action}; the method guard wins", GuardName, GuardName, MethodName);
                    _mixedGuardWarningLogged = true;
                }
                return InvokeGuard(guardMethod, new[] { parameter });
            }
        }

        return guardPropertyGetter?.Invoke() ?? true;
    }

    /// <summary>
    /// Occurs when changes occur that affect whether or not the command should execute.
    /// </summary>
    public event EventHandler? CanExecuteChanged;

    /// <summary>
    /// The method to be called when the command is invoked.
    /// </summary>
    /// <param name="parameter">Data used by the command. If the command does not require data to be passed, this object can be set to null.</param>
    public void Execute(object? parameter)
    {
        AssertTargetSet();

        if (Target == null)
            return;

        if (HasParameters)
        {
            AssertNoMixedParameters();

            if (parameter != null)
                throw new InvalidOperationException(
                    string.Format("Cannot combine 'CommandParameter' with 's:Action.Parameters' on the same control (action '{0}'). Use one or the other.", MethodName));

            var context = CreateExecutionContext(null);
            var values = ResolveArguments(context) ?? Array.Empty<object?>();
            var method = ResolveParameterizedMethod(values);
            if (method == null)
            {
                if (ActionNonExistentBehaviour == ActionUnavailableBehaviour.Throw)
                    throw new ActionNotFoundException(
                        string.Format("Unable to find method {0} on {1} accepting the supplied parameter values", MethodName, Target.GetType().Name));
                return;
            }
            InvokeParameterized(method, values);
            return;
        }

        if (TargetMethodInfo == null)
            return;

        var parameters = TargetMethodInfo.GetParameters().Length == 1 ? new[] { parameter } : null;
        InvokeTargetMethod(parameters);
    }

    private MethodInfo? ResolveGuardMethod(MethodInfo actionMethod, object?[] values)
    {
        if (Target == null)
            return null;

        var targetType = Target is Type t ? t : Target.GetType();
        var guard = ActionMethodResolver.Instance.Resolve(targetType, GuardName, values, GetBindingFlags());
        if (guard == null)
            return null;
        if (guard.ReturnType != typeof(bool))
        {
            var message = string.Format("Guard method {0} on {1} must return bool (action {2})", GuardName, targetType.Name, MethodName);
            Logger.LogError(message);
            throw new ActionSignatureInvalidException(message);
        }
        return guard;
    }

    private bool InvokeGuard(MethodInfo guard, object?[] values)
    {
        var parameters = guard.GetParameters();
        var coerced = new object?[values.Length];
        for (var i = 0; i < values.Length; i++)
            coerced[i] = ActionMethodResolver.TryConvert(values[i], parameters[i].ParameterType, out var converted)
                ? converted
                : values[i];

        var target = guard.IsStatic ? null : Target;
        return (bool)guard.Invoke(target, coerced)!;
    }
}
