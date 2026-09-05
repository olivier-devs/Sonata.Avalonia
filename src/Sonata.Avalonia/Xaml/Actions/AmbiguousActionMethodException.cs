namespace Sonata.Avalonia.Xaml;

/// <summary>
/// Thrown when more than one overload of the action method matches the supplied parameters.
/// </summary>
[SuppressMessage("Microsoft.Usage", "CA2237:MarkISerializableTypesWithSerializable")]
public class AmbiguousActionMethodException : Exception
{
    internal AmbiguousActionMethodException(string message) : base(message) { }
}
