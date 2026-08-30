namespace Sonata.Avalonia.Xaml;

/// <summary>
/// <see cref="Binding"/> subclass which rethrows exceptions encountered on setting the source
/// </summary>
public class RethrowingBinding : Binding
{
    /// <inheritdoc/>
    public RethrowingBinding()
    {
    }

    /// <inheritdoc/>
    public RethrowingBinding(string path)
        : base(path)
    {
    }

    private object ExceptionFilter(object bindExpression, Exception exception)
    {
        var edi = ExceptionDispatchInfo.Capture(exception);
        UiThreadDispatch.OnUIThread(() => edi.Throw());
        return exception;
    }
}
