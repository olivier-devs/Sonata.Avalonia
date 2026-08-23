namespace Sonata.Avalonia;

/// <summary>
/// Internal service locator used by framework XAML integration (View.Model) and by
/// WindowManager for transient MessageBox ViewModels. Not part of the public API —
/// resolve services through constructor injection instead.
/// </summary>
internal static class IoC
{
    internal static Func<Type, string?, object> GetInstance =
        (service, key) => throw new InvalidOperationException(
            "IoC not initialized. Did you forget to call base.Initialize() from your App's Initialize() method?");

    internal static Func<Type, IEnumerable<object>> GetInstances =
        (service) => throw new InvalidOperationException(
            "IoC not initialized. Did you forget to call base.Initialize() from your App's Initialize() method?");

    internal static T Get<T>(string? key = null)
    {
        return (T)GetInstance(typeof(T), key);
    }

    internal static IEnumerable<T> GetAll<T>()
    {
        return GetInstances(typeof(T)).Cast<T>();
    }
}
