namespace Sonata.Avalonia;

/// <summary>
/// Base class for all conductors which had a single active item
/// </summary>
/// <typeparam name="T">Type of item being conducted</typeparam>
public abstract class ConductorBaseWithActiveItem<T> : ConductorBase<T>, IHaveActiveItem<T> where T : class
{
    private T? _activeItem;

    /// <summary>
    /// Gets or sets the item which is currently active.
    /// Setting this fire-and-forgets the activation; exceptions are logged.
    /// </summary>
    public T? ActiveItem
    {
        get => _activeItem;
        set => FireAndForget.Run(ActivateItemAsync(value), SonataLogManager.GetLogger(GetType()));
    }

    /// <summary>
    /// From IParent, fetch all items
    /// </summary>
    public override IEnumerable<T> GetChildren()
    {
        return ActiveItem == null ? Enumerable.Empty<T>() : new[] { ActiveItem };
    }

    /// <summary>
    /// Switch the active item to the given item
    /// </summary>
    protected Task ChangeActiveItemAsync(T? newItem, bool closePrevious, CancellationToken ct = default)
    {
        return ExecuteTransitionAsync(async () =>
        {
            await ScreenExtensions.TryDeactivateAsync(ActiveItem, ct);
            if (closePrevious)
                await this.CloseAndCleanUpAsync(ActiveItem, DisposeChildren, ct);

            _activeItem = newItem;

            if (newItem is not null)
            {
                EnsureItem(newItem);

                if (IsActive)
                    await ScreenExtensions.TryActivateAsync(newItem, ct);
                else
                    await ScreenExtensions.TryDeactivateAsync(newItem, ct);
            }

            NotifyOfPropertyChange("ActiveItem");
        }, ct);
    }

    /// <summary>
    /// When we're activated, also activate the ActiveItem
    /// </summary>
    protected override Task OnActivateAsync(CancellationToken ct)
    {
        return ScreenExtensions.TryActivateAsync(ActiveItem, ct);
    }

    /// <summary>
    /// When we're deactivated, also deactivate the ActiveItem
    /// </summary>
    protected override Task OnDeactivateAsync(CancellationToken ct)
    {
        return ScreenExtensions.TryDeactivateAsync(ActiveItem, ct);
    }

    /// <summary>
    /// When we're closed, also close the ActiveItem
    /// </summary>
    protected override Task OnCloseAsync(CancellationToken ct)
    {
        return this.CloseAndCleanUpAsync(ActiveItem, DisposeChildren, ct);
    }
}
