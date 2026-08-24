namespace Sonata.Avalonia.Internal;

/// <summary>
/// Implemented by services which need to act when the application framework
/// initialization completes. Registered in the IoC container; retrieved by
/// <c>SonataApplication{T}</c> after the root view is displayed.
/// </summary>
internal interface IApplicationLifetimeParticipant
{
    /// <summary>Called once, after the root view has been displayed.</summary>
    void OnFrameworkInitialized();
}
