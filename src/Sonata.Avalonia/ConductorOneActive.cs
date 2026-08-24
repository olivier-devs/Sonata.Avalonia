namespace Sonata.Avalonia;

public partial class Conductor<T>
{
    public partial class Collection
    {
        /// <summary>
        /// Conductor with many items, only one of which is active
        /// </summary>
        public class OneActive : ConductorBaseWithActiveItem<T>
        {
            private readonly BindableCollection<T> items = new();

            private List<T> itemsBeforeReset;

            /// <summary>
            /// Gets the items owned by this Conductor, one of which is active
            /// </summary>
            public IObservableCollection<T> Items => items;

            /// <summary>
            /// Initialises a new instance of the <see cref="Conductor{T}.Collection.OneActive"/> class
            /// </summary>
            public OneActive()
            {
                items.CollectionChanging += ItemsCollectionChanging;
                items.CollectionChanged += ItemsCollectionChanged;
            }

            private void ItemsCollectionChanging(object? sender, NotifyCollectionChangedEventArgs e)
            {
                if (e.Action == NotifyCollectionChangedAction.Reset)
                    itemsBeforeReset = items.ToList();
            }

            private void ItemsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
            {
                switch (e.Action)
                {
                    case NotifyCollectionChangedAction.Add:
                        FireAndForget.Run(this.SetParentAndSetActiveAsync(e.NewItems, false), Logger);
                        break;

                    case NotifyCollectionChangedAction.Remove:
                        FireAndForget.Run(HandleRemoveAsync(e.OldItems), Logger);
                        break;

                    case NotifyCollectionChangedAction.Replace:
                        FireAndForget.Run(HandleReplaceAsync(e.OldItems, e.NewItems), Logger);
                        break;

                    case NotifyCollectionChangedAction.Reset:
                        FireAndForget.Run(HandleResetAsync(), Logger);
                        break;
                }
            }

            private async Task HandleRemoveAsync(IList oldItems)
            {
                // ActiveItemMayHaveBeenRemovedFromItems may deactivate the ActiveItem; CloseAndCleanUp may close it.
                // Call the methods in this order to avoid closing then deactivating (which causes reactivation)
                await ActiveItemMayHaveBeenRemovedFromItemsAsync();
                await this.CloseAndCleanUpAsync(oldItems, DisposeChildren);
            }

            private async Task HandleReplaceAsync(IList oldItems, IList newItems)
            {
                await ActiveItemMayHaveBeenRemovedFromItemsAsync();
                await this.CloseAndCleanUpAsync(oldItems, DisposeChildren);
                await this.SetParentAndSetActiveAsync(newItems, false);
            }

            private async Task HandleResetAsync()
            {
                var before = itemsBeforeReset ?? new List<T>();
                await ActiveItemMayHaveBeenRemovedFromItemsAsync();
                await this.CloseAndCleanUpAsync(before.Except(items), DisposeChildren);
                await this.SetParentAndSetActiveAsync(items.Except(before), false);
                itemsBeforeReset = null;
            }

            /// <summary>
            /// Called when the ActiveItem may have been removed from the Items collection. If it has, will change the ActiveItem to something sensible
            /// </summary>
            protected virtual async Task ActiveItemMayHaveBeenRemovedFromItemsAsync()
            {
                if (items.Contains(ActiveItem))
                    return;

                // Only close the previous item if it's in this.items - if it isn't, we'll
                // have already have closed it as part of reacting to changes in this.items.
                await ChangeActiveItemAsync(items.FirstOrDefault(), items.Contains(ActiveItem));
            }

            /// <summary>
            /// Return all items associated with this conductor
            /// </summary>
            public override IEnumerable<T> GetChildren()
            {
                return items;
            }

            /// <summary>
            /// Activate the given item and set it as the ActiveItem, deactivating the previous ActiveItem
            /// </summary>
            public override async Task ActivateItemAsync(T item, CancellationToken ct = default)
            {
                if (item != null && item.Equals(ActiveItem))
                {
                    if (IsActive)
                        await ScreenExtensions.TryActivateAsync(ActiveItem, ct);
                }
                else
                {
                    await ChangeActiveItemAsync(item, false, ct);
                }
            }

            /// <summary>
            /// Deactive the given item, and choose another item to set as the ActiveItem
            /// </summary>
            public override async Task DeactivateItemAsync(T item, CancellationToken ct = default)
            {
                if (item == null)
                    return;

                if (item.Equals(ActiveItem))
                {
                    var nextItem = DetermineNextItemToActivate(item);
                    await ChangeActiveItemAsync(nextItem, false, ct);
                }
                else
                {
                    await ScreenExtensions.TryDeactivateAsync(item, ct);
                }
            }

            /// <summary>
            /// Close the given item (if and when possible, depending on IGuardClose.CanCloseAsync). This will deactive if it is the active item
            /// </summary>
            public override async Task CloseItemAsync(T item, CancellationToken ct = default)
            {
                if (item == null || !await CanCloseItem(item, ct))
                    return;

                if (item.Equals(ActiveItem))
                {
                    var nextItem = DetermineNextItemToActivate(item);
                    // Counter-intuitively, we *don't* want to close the old ActiveItem. Removing it from 'this.items' below
                    // will do that, and we don't want to do it twice.
                    await ChangeActiveItemAsync(nextItem, false, ct);
                }
                // Likewise if it isn't the ActiveItem, don't call CloseAndCleanup, as removing from 'this.items' will do that

                items.Remove(item);
            }

            /// <summary>
            /// Given a list of items, and and item which is going to be removed, choose a new item to be the next ActiveItem
            /// </summary>
            protected virtual T DetermineNextItemToActivate(T itemToRemove)
            {
                if (itemToRemove == null)
                {
                    return items.FirstOrDefault();
                }

                if (items.Count > 1)
                {
                    // indexOfItemBeingRemoved *can* be -1 - if the item being removed doesn't exist in the list
                    var indexOfItemBeingRemoved = items.IndexOf(itemToRemove);

                    if (indexOfItemBeingRemoved < 0)
                        return items[0];
                    if (indexOfItemBeingRemoved == 0)
                        return items[1];
                    return items[indexOfItemBeingRemoved - 1];
                }

                return default;
            }

            /// <summary>
            /// Returns true if and when all children can close
            /// </summary>
            public override Task<bool> CanCloseAsync(CancellationToken ct = default)
            {
                return CanAllItemsCloseAsync(items, ct);
            }

            /// <summary>
            /// Ensures that all items are closed when this conductor is closed
            /// </summary>
            protected override Task OnCloseAsync(CancellationToken ct)
            {
                // We've already been deactivated by this point
                // Clearing this.items causes all to be closed
                items.Clear();
                return Task.CompletedTask;
            }

            /// <summary>
            /// Ensure an item is ready to be activated
            /// </summary>
            protected override void EnsureItem(T newItem)
            {
                if (!items.Contains(newItem))
                    items.Add(newItem);

                base.EnsureItem(newItem);
            }
        }
    }
}
