namespace Sonata.Avalonia;

/// <summary>
/// Base class for all conductors
/// </summary>
/// <typeparam name="T">Type of item to be conducted</typeparam>
public abstract class ConductorBase<T> : Screen, IConductor<T>, IParent<T>, IChildDelegate where T : class
{
    private bool _disposeChildren = true;

    /// <summary>
    /// Gets or sets a value indicating whether to dispose a child when it's closed. True by default
    /// Can't be an auto-property, since it's virtual so we can't set it in the ctor
    /// </summary>
    public virtual bool DisposeChildren
    {
        get => _disposeChildren;
        set => _disposeChildren = value;
    }

    /// <summary>
    /// Retrieves the Item or Items associated with this Conductor
    /// </summary>
    /// <returns>Item or Items associated with this Conductor</returns>
    public abstract IEnumerable<T> GetChildren();

    /// <summary>
    /// Activate the given item
    /// </summary>
    public abstract Task ActivateItemAsync(T item, CancellationToken ct = default);

    /// <summary>
    /// Deactivate the given item
    /// </summary>
    public abstract Task DeactivateItemAsync(T item, CancellationToken ct = default);

    /// <summary>
    /// Close the given item
    /// </summary>
    public abstract Task CloseItemAsync(T item, CancellationToken ct = default);

    /// <summary>
    /// Ensure an item is ready to be activated
    /// </summary>
    protected virtual void EnsureItem(T newItem)
    {
        Debug.Assert(newItem != null);

        var newItemAsChild = newItem as IChild;
        if (newItemAsChild != null && newItemAsChild.Parent != this)
            newItemAsChild.Parent = this;
    }

    /// <summary>
    /// Utility method to determine if all of the give items can close
    /// </summary>
    protected virtual async Task<bool> CanAllItemsCloseAsync(IEnumerable<T> itemsToClose, CancellationToken ct = default)
    {
        // We need to call these in order: we don't want them all do show "are you sure you
        // want to close" dialogs at once, for instance.
        foreach (var itemToClose in itemsToClose)
        {
            if (!await CanCloseItem(itemToClose, ct))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Determine if the given item can be closed
    /// </summary>
    protected virtual Task<bool> CanCloseItem(T item, CancellationToken ct = default)
    {
        var itemAsGuardClose = item as IGuardClose;
        if (itemAsGuardClose != null)
            return itemAsGuardClose.CanCloseAsync(ct);
        else
            return Task.FromResult(true);
    }

    /// <summary>
    /// Close the given child
    /// </summary>
    async Task IChildDelegate.CloseItemAsync(object item, bool? dialogResult, CancellationToken ct)
    {
        if (item is T t)
        {
            await CloseItemAsync(t, ct);
        }
    }
}
