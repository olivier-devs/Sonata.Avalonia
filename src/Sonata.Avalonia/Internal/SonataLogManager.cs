namespace Sonata.Avalonia.Internal;

/// <summary>
/// Bridge providing loggers to framework types which are not created by the IoC container
/// (e.g. Screen subclasses created by user code, actions created by the XAML parser).
/// The factory is assigned by the bootstrapper during startup; before that (and for custom
/// bootstrappers which don't provide one) a no-op factory is used.
/// </summary>
internal static class SonataLogManager
{
    private static volatile ILoggerFactory _factory = NullLoggerFactory.Instance;

    public static void SetFactory(ILoggerFactory? factory)
    {
        _factory = factory ?? NullLoggerFactory.Instance;
    }

    public static ILogger GetLogger(Type type) => _factory.CreateLogger(type);
}
