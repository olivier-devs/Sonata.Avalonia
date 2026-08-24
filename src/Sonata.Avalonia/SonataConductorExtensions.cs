namespace Sonata.Avalonia;

// Don't name ConductorExtensions, otherwise it's too obvious when someone types 'Conductor'

/// <summary>
/// Extension methods used by the Conductor classes
/// </summary>
public static class SonataConductorExtensions
{
    /// <summary>
    /// For each item in a list, set the parent to the current conductor
    /// </summary>
    public static async Task SetParentAndSetActiveAsync<T>(this IConductor<T> parent, IEnumerable items, bool active, CancellationToken ct = default)
    {
        foreach (var item in items)
        {
            var itemAsChild = item as IChild;
            if (itemAsChild != null)
                itemAsChild.Parent = parent;

            if (active)
                await ScreenExtensions.TryActivateAsync(item, ct);
            else
                await ScreenExtensions.TryDeactivateAsync(item, ct);
        }
    }

    /// <summary>
    /// Close an item, and clear its parent if it's set to the current parent
    /// </summary>
    public static async Task CloseAndCleanUpAsync<T>(this IConductor<T> parent, T item, bool dispose, CancellationToken ct = default)
    {
        await ScreenExtensions.TryCloseAsync(item, ct);

        var itemAsChild = item as IChild;
        if (itemAsChild != null && itemAsChild.Parent == parent)
            itemAsChild.Parent = null;

        if (dispose)
            await ScreenExtensions.TryDisposeAsync(item);
    }

    /// <summary>
    /// For each item in a list, close it, and if its parent is set to the given parent, clear that parent
    /// </summary>
    public static async Task CloseAndCleanUpAsync<T>(this IConductor<T> parent, IEnumerable items, bool dispose, CancellationToken ct = default)
    {
        foreach (var item in items.OfType<T>())
        {
            await parent.CloseAndCleanUpAsync(item, dispose, ct);
        }
    }
}
