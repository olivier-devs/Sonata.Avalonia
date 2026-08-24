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

    private readonly SemaphoreSlim _transitionLock = new(1, 1);
    private volatile bool _executingTransition;
    private Task _activeItemTransition = Task.CompletedTask;

    /// <summary>
    /// Task which completes when the most recently started transition completes.
    /// </summary>
    public Task ActiveItemTransition => _activeItemTransition;

    /// <summary>
    /// Serializes a transition through the transition lock, exposing the in-flight task via
    /// <see cref="ActiveItemTransition"/>. A synchronous re-entrant call (a transition started
    /// from inside another transition's hook) throws a clear exception instead of deadlocking.
    /// </summary>
    protected Task ExecuteTransitionAsync(Func<Task> transition, CancellationToken ct = default)
    {
        if (_executingTransition)
            throw new InvalidOperationException(
                "Cannot start a transition while another transition is executing synchronously on the same conductor. " +
                "Do not call ActivateItemAsync/DeactivateItemAsync/CloseItemAsync from inside an On*Async hook of the same conductor.");

        var task = ExecuteTransitionCoreAsync(transition, ct);
        _activeItemTransition = task;
        return task;
    }

    private async Task ExecuteTransitionCoreAsync(Func<Task> transition, CancellationToken ct)
    {
        await _transitionLock.WaitAsync(ct);
        try
        {
            _executingTransition = true;
            try
            {
                var task = transition();
                _executingTransition = false;
                await task;
            }
            finally
            {
                _executingTransition = false;
            }
        }
        finally
        {
            _transitionLock.Release();
        }
    }

    /// <summary>
    /// Throws <see cref="InvalidOperationException"/> if a transition is currently executing
    /// synchronously on this conductor. Used by public transition methods which can short-circuit
    /// around <see cref="ExecuteTransitionAsync"/>.
    /// </summary>
    protected void EnsureNotReentrant()
    {
        if (_executingTransition)
            throw new InvalidOperationException(
                "Cannot start a transition while another transition is executing synchronously on the same conductor. " +
                "Do not call ActivateItemAsync/DeactivateItemAsync/CloseItemAsync from inside an On*Async hook of the same conductor.");
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
