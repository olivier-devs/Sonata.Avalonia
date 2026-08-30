namespace Sonata.Avalonia;

public partial class Conductor<T>
{
    /// <summary>
    /// Contains specific Conductor{T} collection types
    /// </summary>
    public partial class Collection
    {
        /// <summary>
        /// Conductor which has many items, all of which active at the same time
        /// </summary>
        public class AllActive : ConductorBase<T>
        {
            private readonly BindableCollection<T> _items = new();

            private List<T>? itemsBeforeReset = new();

            /// <summary>
            /// Gets all items associated with this conductor
            /// </summary>
            public IObservableCollection<T> Items => _items;

            /// <summary>
            /// Initialises a new instance of the <see cref="Conductor{T}.Collection.AllActive"/> class
            /// </summary>
            public AllActive()
            {
                _items.CollectionChanging += ItemsCollectionChanging;
                _items.CollectionChanged += ItemsCollectionChanged;
            }

            private void ItemsCollectionChanging(object? sender, NotifyCollectionChangedEventArgs e)
            {
                if (e.Action == NotifyCollectionChangedAction.Reset)
                    itemsBeforeReset = _items.ToList();
            }

            private void ItemsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
            {
                switch (e.Action)
                {
                    case NotifyCollectionChangedAction.Add:
                        FireAndForget.Run(ActivateAndSetParentAsync(e.NewItems ?? Array.Empty<object>()), Logger);
                        break;

                    case NotifyCollectionChangedAction.Remove:
                        FireAndForget.Run(this.CloseAndCleanUpAsync(e.OldItems ?? Array.Empty<object>(), DisposeChildren), Logger);
                        break;

                    case NotifyCollectionChangedAction.Replace:
                        FireAndForget.Run(HandleReplaceAsync(e.OldItems ?? Array.Empty<object>(), e.NewItems ?? Array.Empty<object>()), Logger);
                        break;

                    case NotifyCollectionChangedAction.Reset:
                        FireAndForget.Run(HandleResetAsync(), Logger);
                        break;
                }
            }

            private async Task HandleReplaceAsync(IList oldItems, IList newItems)
            {
                await ActivateAndSetParentAsync(newItems);
                await this.CloseAndCleanUpAsync(oldItems, DisposeChildren);
            }

            private async Task HandleResetAsync()
            {
                var before = itemsBeforeReset ?? new List<T>();
                await ActivateAndSetParentAsync(_items.Except(before));
                await this.CloseAndCleanUpAsync(before.Except(_items), DisposeChildren);
                itemsBeforeReset = null;
            }

            /// <summary>
            /// Active all items in a given collection if appropriate, and set the parent of all items to this
            /// </summary>
            protected virtual Task ActivateAndSetParentAsync(IEnumerable items)
            {
                return this.SetParentAndSetActiveAsync(items, IsActive);
            }

            /// <summary>
            /// Activates all items whenever this conductor is activated
            /// </summary>
            protected override async Task OnActivateAsync(CancellationToken ct)
            {
                // Copy the list, in case someone tries to modify it as a result of being activated
                var itemsToActivate = _items.OfType<IScreenState>().ToList();
                foreach (var item in itemsToActivate)
                {
                    await item.ActivateAsync(ct);
                }
            }

            /// <summary>
            /// Deactivates all items whenever this conductor is deactivated
            /// </summary>
            protected override async Task OnDeactivateAsync(CancellationToken ct)
            {
                // Copy the list, in case someone tries to modify it as a result of being activated
                var itemsToDeactivate = _items.OfType<IScreenState>().ToList();
                foreach (var item in itemsToDeactivate)
                {
                    await item.DeactivateAsync(ct);
                }
            }

            /// <summary>
            /// Close, and clean up, all items when this conductor is closed
            /// </summary>
            protected override async Task OnCloseAsync(CancellationToken ct)
            {
                // Copy the list, in case someone tries to modify it as a result of being closed
                // We've already been deactivated by this point
                var itemsToClose = _items.ToList();
                foreach (var item in itemsToClose)
                {
                    await this.CloseAndCleanUpAsync(item, DisposeChildren, ct);
                }

                _items.Clear();
            }

            /// <summary>
            /// Determine if the conductor can close. Returns true if and when all items can close
            /// </summary>
            public override Task<bool> CanCloseAsync(CancellationToken ct = default)
            {
                return CanAllItemsCloseAsync(_items, ct);
            }

            /// <summary>
            /// Activate the given item, and add it to the Items collection
            /// </summary>
            public override Task ActivateItemAsync(T? item, CancellationToken ct = default)
            {
                if (item == null)
                    return Task.CompletedTask;

                return ExecuteTransitionAsync(async () =>
                {
                    EnsureItem(item);

                    if (IsActive)
                        await ScreenExtensions.TryActivateAsync(item, ct);
                    else
                        await ScreenExtensions.TryDeactivateAsync(item, ct);
                }, ct);
            }

            /// <summary>
            /// Deactive the given item
            /// </summary>
            public override Task DeactivateItemAsync(T? item, CancellationToken ct = default)
            {
                return ScreenExtensions.TryDeactivateAsync(item, ct);
            }

            /// <summary>
            /// Close a particular item, removing it from the Items collection
            /// </summary>
            public override async Task CloseItemAsync(T? item, CancellationToken ct = default)
            {
                if (item == null)
                    return;

                await ExecuteTransitionAsync(async () =>
                {
                    if (await CanCloseItem(item, ct))
                    {
                        await this.CloseAndCleanUpAsync(item, DisposeChildren, ct);
                        _items.Remove(item);
                    }
                }, ct);
            }

            /// <summary>
            /// Returns all children of this parent
            /// </summary>
            public override IEnumerable<T> GetChildren()
            {
                return _items;
            }

            /// <summary>
            /// Ensure an item is ready to be activated, by adding it to the items collection, as well as setting it up
            /// </summary>
            protected override void EnsureItem(T newItem)
            {
                if (!_items.Contains(newItem))
                    _items.Add(newItem);

                base.EnsureItem(newItem);
            }
        }
    }
}
