namespace Sonata.Avalonia;

/// <summary>
/// Handy extensions for working with screens
/// </summary>
public static class ScreenExtensions
{
    /// <summary>
    /// Attempt to activate the screen, if it implements IScreenState
    /// </summary>
    public static Task TryActivateAsync(object? screen, CancellationToken ct = default)
    {
        var screenAsScreenState = screen as IScreenState;
        if (screenAsScreenState != null)
            return screenAsScreenState.ActivateAsync(ct);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Attempt to deactivate the screen, if it implements IScreenState
    /// </summary>
    public static Task TryDeactivateAsync(object? screen, CancellationToken ct = default)
    {
        var screenAsScreenState = screen as IScreenState;
        if (screenAsScreenState != null)
            return screenAsScreenState.DeactivateAsync(ct);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Try to close the screen, if it implements IScreenState
    /// </summary>
    public static Task TryCloseAsync(object? screen, CancellationToken ct = default)
    {
        var screenAsScreenState = screen as IScreenState;
        if (screenAsScreenState != null)
            return screenAsScreenState.CloseAsync(ct);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Try to dispose a screen, if it implements IAsyncDisposable (or IDisposable)
    /// </summary>
    public static async ValueTask TryDisposeAsync(object? screen)
    {
        switch (screen)
        {
            case IAsyncDisposable asyncDisposable:
                await asyncDisposable.DisposeAsync();
                break;
            case IDisposable disposable:
                disposable.Dispose();
                break;
        }
    }

    /// <summary>
    /// Activate the child whenever the parent is activated
    /// </summary>
    /// <example>child.ActivateWith(this)</example>
    public static void ActivateWith(this IScreenState child, IScreenState parent)
    {
        var weakChild = new WeakReference<IScreenState>(child);
        EventHandler<ActivationEventArgs>? handler = null;
        handler = (o, e) =>
        {
            if (weakChild.TryGetTarget(out IScreenState? strongChild))
                FireAndForget.Run(strongChild.ActivateAsync(), SonataLogManager.GetLogger(typeof(ScreenExtensions)));
            else
                parent.Activated -= handler;
        };
        parent.Activated += handler;
    }

    /// <summary>
    /// Deactivate the child whenever the parent is deactivated
    /// </summary>
    /// <example>child.DeactivateWith(this)</example>
    public static void DeactivateWith(this IScreenState child, IScreenState parent)
    {
        var weakChild = new WeakReference<IScreenState>(child);
        EventHandler<DeactivationEventArgs>? handler = null;
        handler = (o, e) =>
        {
            if (weakChild.TryGetTarget(out IScreenState? strongChild))
                FireAndForget.Run(strongChild.DeactivateAsync(), SonataLogManager.GetLogger(typeof(ScreenExtensions)));
            else
                parent.Deactivated -= handler;
        };
        parent.Deactivated += handler;
    }

    /// <summary>
    /// Close the child whenever the parent is closed
    /// </summary>
    /// <example>child.CloseWith(this)</example>
    public static void CloseWith(this IScreenState child, IScreenState parent)
    {
        var weakChild = new WeakReference<IScreenState>(child);
        EventHandler<CloseEventArgs>? handler = null;
        handler = (o, e) =>
        {
            if (weakChild.TryGetTarget(out IScreenState? strongChild))
                FireAndForget.Run(TryCloseAsync(strongChild), SonataLogManager.GetLogger(typeof(ScreenExtensions)));
            else
                parent.Closed -= handler;
        };
        parent.Closed += handler;
    }

    /// <summary>
    /// Activate, Deactivate, or Close the child whenever the parent is Activated, Deactivated, or Closed
    /// </summary>
    /// <example>child.ConductWith(this)</example>
    public static void ConductWith(this IScreenState child, IScreenState parent)
    {
        child.ActivateWith(parent);
        child.DeactivateWith(parent);
        child.CloseWith(parent);
    }
}
