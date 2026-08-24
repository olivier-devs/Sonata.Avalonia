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
                logger.LogError(task.Exception, "Unhandled exception in fire-and-forget task");
            return;
        }

        _ = task.ContinueWith(
            t => logger.LogError(t.Exception, "Unhandled exception in fire-and-forget task"),
            CancellationToken.None,
            TaskContinuationOptions.OnlyOnFaulted,
            TaskScheduler.Default);
    }
}
