namespace Sonata.Avalonia;

public partial class Conductor<T>
{
    /// <summary>
    /// Stack-based navigation. A Conductor which has one active item, and a stack of previous items
    /// </summary>
    public class StackNavigation : ConductorBaseWithActiveItem<T>
    {
        // We need to remove arbitrary items, so no Stack<T> here!
        private readonly List<T> _history = new();

        /// <summary>
        /// Activate the given item. This deactivates the previous item, and pushes it onto the history stack
        /// </summary>
        public override async Task ActivateItemAsync(T? item, CancellationToken ct = default)
        {
            if (item != null && item.Equals(ActiveItem))
            {
                if (IsActive)
                    await ScreenExtensions.TryActivateAsync(ActiveItem, ct);
            }
            else
            {
                if (ActiveItem != null)
                    _history.Add(ActiveItem);
                await ChangeActiveItemAsync(item, false, ct);
            }
        }

        /// <summary>
        /// Deactivate the given item
        /// </summary>
        public override Task DeactivateItemAsync(T? item, CancellationToken ct = default)
        {
            return ScreenExtensions.TryDeactivateAsync(item, ct);
        }

        /// <summary>
        /// Close the active item, and re-activate the top item in the history stack
        /// </summary>
        public Task GoBackAsync(CancellationToken ct = default)
        {
            return CloseItemAsync(ActiveItem, ct);
        }

        /// <summary>
        /// Close and remove all items in the history stack, leaving the ActiveItem
        /// </summary>
        public async Task ClearAsync(CancellationToken ct = default)
        {
            foreach (var item in _history)
            {
                await this.CloseAndCleanUpAsync(item, DisposeChildren, ct);
            }
            _history.Clear();
        }

        /// <summary>
        /// Close the given item. If it was the ActiveItem, activate the top item in the history stack
        /// </summary>
        public override async Task CloseItemAsync(T? item, CancellationToken ct = default)
        {
            if (item == null || !await CanCloseItem(item, ct))
                return;

            if (item.Equals(ActiveItem))
            {
                var newItem = default(T);
                if (_history.Count > 0)
                {
                    newItem = _history.Last();
                    _history.RemoveAt(_history.Count - 1);
                }
                await ChangeActiveItemAsync(newItem, true, ct);
            }
            else if (_history.Contains(item))
            {
                await this.CloseAndCleanUpAsync(item, DisposeChildren, ct);
                _history.Remove(item);
            }
        }

        /// <summary>
        /// Returns true if and when all items (ActiveItem + everything in the history stack) can close
        /// </summary>
        public override Task<bool> CanCloseAsync(CancellationToken ct = default)
        {
            return CanAllItemsCloseAsync(_history.Concat(new[] { ActiveItem }.OfType<T>()), ct);
        }

        /// <summary>
        /// Ensures that all children are closed when this conductor is closed
        /// </summary>
        protected override async Task OnCloseAsync(CancellationToken ct)
        {
            // We've already been deactivated by this point
            foreach (var item in _history)
                await this.CloseAndCleanUpAsync(item, DisposeChildren, ct);
            _history.Clear();

            await this.CloseAndCleanUpAsync(ActiveItem, DisposeChildren, ct);
        }
    }
}
