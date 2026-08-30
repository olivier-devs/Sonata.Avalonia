using Avalonia.Metadata;

namespace Sonata.Avalonia.Xaml;

/// <summary>
/// What to do if the given target is null, or if the given action doesn't exist on the target
/// </summary>
public enum ActionUnavailableBehaviour
{
    /// <summary>
    /// The default behaviour. What this is depends on whether this applies to an action or target, and an event or ICommand
    /// </summary>
    Default,

    /// <summary>
    /// Enable the control anyway. Clicking/etc the control won't do anything
    /// </summary>
    Enable,

    /// <summary>
    /// Disable the control. This is only valid for commands, not events
    /// </summary>
    Disable,

    /// <summary>
    /// An exception will be thrown when the control is clicked
    /// </summary>
    Throw
}

/// <summary>
/// MarkupExtension used for binding Commands and Events to methods on the View.ActionTarget
/// </summary>
public class ActionExtension : MarkupExtension
{
    /// <summary>
    /// Gets or sets the name of the method to call
    /// </summary>
    [ConstructorArgument("method")]
    public string? Method { get; set; }

    /// <summary>
    /// Gets or sets a target to override that set with View.ActionTarget
    /// </summary>
    public object? Target { get; set; }

    /// <summary>
    /// Gets or sets the behaviour if the View.ActionTarget is nulil
    /// </summary>
    public ActionUnavailableBehaviour NullTarget { get; set; }

    /// <summary>
    /// Gets or sets the behaviour if the action itself isn't found on the View.ActionTarget
    /// </summary>
    public ActionUnavailableBehaviour ActionNotFound { get; set; }

    /// <summary>
    /// Initialises a new instance of the <see cref="ActionExtension"/> class
    /// </summary>
    public ActionExtension()
    {
    }

    /// <summary>
    /// Initialises a new instance of the <see cref="ActionExtension"/> class with the given method name
    /// </summary>
    /// <param name="method">Name of the method to call</param>
    public ActionExtension(string method)
    {
        Method = method;
    }

    private ActionUnavailableBehaviour CommandNullTargetBehaviour => NullTarget == ActionUnavailableBehaviour.Default ? UiThreadDispatch.InDesignMode ? ActionUnavailableBehaviour.Enable : ActionUnavailableBehaviour.Disable : NullTarget;

    private ActionUnavailableBehaviour CommandActionNotFoundBehaviour => ActionNotFound == ActionUnavailableBehaviour.Default ? ActionUnavailableBehaviour.Throw : ActionNotFound;

    private ActionUnavailableBehaviour EventNullTargetBehaviour => NullTarget == ActionUnavailableBehaviour.Default ? ActionUnavailableBehaviour.Enable : NullTarget;

    private ActionUnavailableBehaviour EventActionNotFoundBehaviour => ActionNotFound == ActionUnavailableBehaviour.Default ? ActionUnavailableBehaviour.Throw : ActionNotFound;

    /// <summary>
    /// When implemented in a derived class, returns an object that is provided as the value of the target property for this markup extension.
    /// </summary>
    /// <param name="serviceProvider">A service provider helper that can provide services for the markup extension.</param>
    /// <returns>The object value to set on the property where the extension is applied.</returns>
    public override object ProvideValue(IServiceProvider serviceProvider)
    {
        if (Method == null)
            throw new InvalidOperationException("Method has not been set");

        var valueService = serviceProvider.GetService(typeof(IProvideValueTarget)) as IProvideValueTarget;

        switch (valueService?.TargetObject)
        {
            case AvaloniaObject targetObject:
                return HandleDependencyObject(serviceProvider, valueService, targetObject);
            // TODO: case CommandBinding commandBinding:
            case RoutedCommandBinding commandBinding:
                {
                    var eventInfo = valueService.TargetProperty as EventInfo
                        ?? throw new InvalidOperationException("Action used with a CommandBinding whose TargetProperty is not an event");
                    var eventType = eventInfo.EventHandlerType
                        ?? throw new InvalidOperationException($"Event {eventInfo.Name} does not have a handler type");
                    return CreateEventAction(serviceProvider, null, eventType, isCommandBinding: true);
                }
            default:
                // Seems this is the case when we're in a template. We'll get called again properly in a second.
                // http://social.msdn.microsoft.com/Forums/vstudio/en-US/a9ead3d5-a4e4-4f9c-b507-b7a7d530c6a9/gaining-access-to-target-object-instead-of-shareddp-in-custom-markupextensions-providevalue-method?forum=wpf
                return this;
        }
    }

    private object HandleDependencyObject(IServiceProvider serviceProvider, IProvideValueTarget valueService, AvaloniaObject targetObject)
    {
        if (valueService.TargetProperty is string str)
        {
            var type = valueService.TargetObject.GetType();
            var eventInfo = type.GetEvent(str)
                ?? throw new InvalidOperationException(string.Format("Unable to find event {0} on {1}", str, type.Name));
            var eventHandlerType = eventInfo.EventHandlerType
                ?? throw new InvalidOperationException($"Event {eventInfo.Name} does not have a handler type");
            return CreateEventAction(serviceProvider, targetObject, eventHandlerType);
        }

        switch (valueService.TargetProperty)
        {
            case AvaloniaProperty dependencyProperty when dependencyProperty.PropertyType == typeof(ICommand):
                // If they're in design mode and haven't set View.ActionTarget, default to looking sensible
                return CreateCommandAction(serviceProvider, targetObject);
            case EventInfo eventInfo:
                var eventType = eventInfo.EventHandlerType
                    ?? throw new InvalidOperationException($"Event {eventInfo.Name} does not have a handler type");
                return CreateEventAction(serviceProvider, targetObject, eventType);
            case MethodInfo methodInfo: // For attached events
                {
                    var parameters = methodInfo.GetParameters();
                    if (parameters.Length == 2 && typeof(Delegate).IsAssignableFrom(parameters[1].ParameterType))
                    {
                        return CreateEventAction(serviceProvider, targetObject, parameters[1].ParameterType);
                    }
                    throw new ArgumentException("Action used with an attached event (or something similar) which didn't follow the normal pattern");
                }
            default:
                throw new ArgumentException("Can only use ActionExtension with a Command property or an event handler");
        }
    }

    private ICommand CreateCommandAction(IServiceProvider serviceProvider, AvaloniaObject? targetObject)
    {
        if (targetObject == null)
            throw new InvalidOperationException("CommandAction requires a target control");

        var methodName = Method ?? throw new InvalidOperationException("Method has not been set");

        if (Target == null)
        {
            var rootObjectProvider = serviceProvider.GetService(typeof(IRootObjectProvider)) as IRootObjectProvider;
            var rootObject = rootObjectProvider?.RootObject as AvaloniaObject;
            return new CommandAction(targetObject, rootObject, methodName, CommandNullTargetBehaviour, CommandActionNotFoundBehaviour);
        }
        else
        {
            return new CommandAction(Target, methodName, CommandNullTargetBehaviour, CommandActionNotFoundBehaviour);
        }
    }

    private Delegate CreateEventAction(IServiceProvider serviceProvider, AvaloniaObject? targetObject, Type eventType, bool isCommandBinding = false)
    {
        var methodName = Method ?? throw new InvalidOperationException("Method has not been set");

        EventAction ec;
        if (Target == null)
        {
            var rootObjectProvider = serviceProvider.GetService(typeof(IRootObjectProvider)) as IRootObjectProvider;
            var rootObject = rootObjectProvider?.RootObject as AvaloniaObject;
            if (isCommandBinding)
            {
                if (rootObject == null)
                    throw new InvalidOperationException("Action may only be used with CommandBinding from a XAML view (unable to retrieve IRootObjectProvider.RootObject)");
                ec = new EventAction(rootObject, null, eventType, methodName, EventNullTargetBehaviour, EventActionNotFoundBehaviour);
            }
            else
            {
                if (targetObject == null)
                    throw new InvalidOperationException("EventAction requires a target control");
                ec = new EventAction(targetObject, rootObject, eventType, methodName, EventNullTargetBehaviour, EventActionNotFoundBehaviour);
            }
        }
        else
        {
            ec = new EventAction(Target, eventType, methodName, EventNullTargetBehaviour, EventActionNotFoundBehaviour);
        }

        return ec.GetDelegate();
    }
}

/// <summary>
/// The View.ActionTarget was not set. This probably means the item is in a ContextMenu/Popup
/// </summary>
[SuppressMessage("Microsoft.Usage", "CA2237:MarkISerializableTypesWithSerializable")]
public class ActionNotSetException : Exception
{
    internal ActionNotSetException(string message) : base(message) { }
}

/// <summary>
/// The Action Target was null, and shouldn't have been (NullTarget = Throw)
/// </summary>
[SuppressMessage("Microsoft.Usage", "CA2237:MarkISerializableTypesWithSerializable")]
public class ActionTargetNullException : Exception
{
    internal ActionTargetNullException(string message) : base(message) { }
}

/// <summary>
/// The method specified could not be found on the Action Target
/// </summary>
[SuppressMessage("Microsoft.Usage", "CA2237:MarkISerializableTypesWithSerializable")]
public class ActionNotFoundException : Exception
{
    internal ActionNotFoundException(string message) : base(message) { }
}

/// <summary>
/// The method specified does not have the correct signature
/// </summary>
[SuppressMessage("Microsoft.Usage", "CA2237:MarkISerializableTypesWithSerializable")]
public class ActionSignatureInvalidException : Exception
{
    internal ActionSignatureInvalidException(string message) : base(message) { }
}
