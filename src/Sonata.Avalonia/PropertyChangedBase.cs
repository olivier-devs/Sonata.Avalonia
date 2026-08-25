namespace Sonata.Avalonia;

/// <summary>
/// Base class for things which can raise PropertyChanged events
/// </summary>
public abstract class PropertyChangedBase : INotifyPropertyChanged
{
    /// <summary>
    /// Occurs when a property value changes
    /// </summary>
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// Refresh all properties
    /// </summary>
    public void Refresh()
    {
        NotifyOfPropertyChange(string.Empty);
    }

    /// <summary>
    /// Raise a PropertyChanged notification from the property in the given expression, e.g. NotifyOfPropertyChange(() => this.Property)
    /// </summary>
    protected virtual void NotifyOfPropertyChange<TProperty>(Expression<Func<TProperty>> property)
    {
        OnPropertyChanged(property.NameForProperty());
    }

    /// <summary>
    /// Raise a PropertyChanged notification from the property with the given name
    /// </summary>
    protected virtual void NotifyOfPropertyChange([CallerMemberName] string propertyName = "")
    {
        OnPropertyChanged(propertyName);
    }

    /// <summary>
    /// Fires the PropertyChanged notification on the UI thread (non-blocking).
    /// </summary>
    /// <remarks>Specially named so that Fody.PropertyChanged calls it</remarks>
    [EditorBrowsable(EditorBrowsableState.Never)]
    protected virtual void OnPropertyChanged(string propertyName)
    {
        var handler = PropertyChanged;
        if (handler != null)
            UiThreadDispatch.PostToUIThread(() => handler(this, new PropertyChangedEventArgs(propertyName)));
    }

    /// <summary>
    /// Takes, by reference, a field, and its new value. If field != value, will set field = value and raise a PropertyChanged notification
    /// </summary>
    protected virtual bool SetAndNotify<T>(ref T field, T value, [CallerMemberName] string propertyName = "")
    {
        if (!EqualityComparer<T>.Default.Equals(field, value))
        {
            field = value;
            NotifyOfPropertyChange(propertyName: propertyName);
            return true;
        }

        return false;
    }
}
