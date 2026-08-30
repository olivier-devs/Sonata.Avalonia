namespace Sonata.Avalonia.Internal;

/// <summary>
/// Observes a fire-and-forget task and logs any exception it produces. Used at
/// synchronous boundaries (property setters, event handlers) which cannot await.
/// No exception is ever swallowed silently.
/// </summary>
internal static class FireAndForget
{
    /// <summary>
    /// Observes <paramref name="task"/> and logs any exception it produces.
    /// Already-completed tasks are observed synchronously (deterministic for tests);
    /// in-flight tasks are observed via a fault-only continuation.
    /// </summary>
    internal static void Run(Task task, ILogger logger)
    {
        ArgumentNullException.ThrowIfNull(task);
        ArgumentNullException.ThrowIfNull(logger);

        if (task.IsCompleted)
        {
            if (task.IsFaulted)
                LogError(logger, task.Exception);
            return;
        }

        _ = task.ContinueWith(
            t => LogError(logger, t.Exception),
            CancellationToken.None,
            TaskContinuationOptions.OnlyOnFaulted,
            TaskScheduler.Default);
    }

    private static void LogError(ILogger logger, Exception? exception)
    {
        try
        {
            logger.LogError(exception, "Unhandled exception in fire-and-forget task {Task}", exception?.GetType().Name ?? "unknown");
        }
        catch
        {
            // A misbehaving logger must not crash the observing thread.
        }
    }
}
