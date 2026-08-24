namespace Sonata.Avalonia;

/// <summary>
/// Conductor with a single active item, and no other items
/// </summary>
/// <typeparam name="T">Type of child to conduct</typeparam>
public partial class Conductor<T> : ConductorBaseWithActiveItem<T> where T : class
{
    /// <summary>
    /// Activate the given item, discarding the previous ActiveItem
    /// </summary>
    public override Task ActivateItemAsync(T item, CancellationToken ct = default)
    {
        EnsureNotReentrant();
        return ActivateItemCoreAsync(item, ct);
    }

    private async Task ActivateItemCoreAsync(T item, CancellationToken ct)
    {
        if (item != null && item.Equals(ActiveItem))
        {
            if (IsActive)
                await ScreenExtensions.TryActivateAsync(item, ct);
        }
        else if (await CanCloseItem(ActiveItem, ct))
        {
            // CanCloseItem is null-safe
            await ChangeActiveItemAsync(item, true, ct);
        }
    }

    /// <summary>
    /// Deactive the given item
    /// </summary>
    public override async Task DeactivateItemAsync(T item, CancellationToken ct = default)
    {
        if (item != null && item.Equals(ActiveItem))
            await ScreenExtensions.TryDeactivateAsync(ActiveItem, ct);
    }

    /// <summary>
    /// Close the given item
    /// </summary>
    public override async Task CloseItemAsync(T item, CancellationToken ct = default)
    {
        if (item == null || !item.Equals(ActiveItem))
            return;

        if (await CanCloseItem(item, ct))
            await ChangeActiveItemAsync(default, true, ct);
    }

    /// <summary>
    /// Determine if this conductor can close. Depends on whether the ActiveItem can close
    /// </summary>
    public override Task<bool> CanCloseAsync(CancellationToken ct = default)
    {
        return CanCloseItem(ActiveItem, ct);
    }
}
