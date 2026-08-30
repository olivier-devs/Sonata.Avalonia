# Commands

Simple command implementations and routed command infrastructure for AvalonUI.

> **Note:** For most cases, `{s:Action}` markup extension covers command binding directly from XAML. See [Actions](./actions.md) for that approach. `RelayCommand` and `RoutedCommand` are lower-level alternatives for programmatic or routed scenarios.

## Key types

| Type | Role | Package |
|------|------|---------|
| `RelayCommand` | Simple `ICommand` wrapping `Action` and optional `Func<bool>` guard | `Sonata.Avalonia` |
| `RoutedCommand` | AvalonUI routed command with bubbling/tunneling | `Sonata.Avalonia` |
| `RoutedCommandBinding` | Associates a `RoutedCommand` with handlers on a control | `Sonata.Avalonia` |
| `RoutedCommandBindableBase` | Abstract base for controls exposing `CommandBindings` | `Sonata.Avalonia` |
| `CanExecuteRoutedEventArgs` | Carries `CanExecute` state in routed events | `Sonata.Avalonia` |
| `ExecutedRoutedEventArgs` | Carries execution state in routed events | `Sonata.Avalonia` |

## Use cases

### RelayCommand for a ViewModel command

```csharp
// Simple command with no guard
public ICommand SaveCommand { get; } = new RelayCommand(() => _repository.Save());

// Command with guard
public ICommand DeleteCommand { get; } = new RelayCommand(
    execute: () => _repository.Delete(SelectedItem),
    canExecute: () => SelectedItem != null
);

// NotifyCanExecuteChanged when guard state changes
public void RefreshCanExecute()
{
    if (DeleteCommand is RelayCommand cmd)
        cmd.NotifyCanExecuteChanged();
}
```

`RelayCommand` (line 3-38 in `RelayCommand.cs`) wraps an `Action` and optional `Func<bool>` guard. `CanExecute` returns `true` if no guard is set, otherwise calls the guard. The constructor throws `ArgumentNullException` if `execute` is null.

### RoutedCommand with bubbling

`RoutedCommand` uses AvalonUI's routed event system for command bubbling/tunneling:

```csharp
// Define a command
public static readonly RoutedCommand AcceptCommand =
    new RoutedCommand("Accept", KeyGesture.Parse("Ctrl+Enter"));

// Attach to a button or menu item
// In XAML: Command={x:Static local:MyCommands.AcceptCommand}

// Handle in a UserControl or Window
public partial class OrderView : UserControl
{
    public OrderView()
    {
        CommandBindings.Add(new RoutedCommandBinding(AcceptCommand,
            executed: OnAccept,
            canExecute: OnCanAccept));
    }

    private void OnAccept(object sender, ExecutedRoutedEventArgs e)
    {
        // Handle the command
    }

    private void OnCanAccept(object sender, CanExecuteRoutedEventArgs e)
    {
        e.CanExecute = _viewModel?.CanAccept ?? false;
        e.Handled = true; // Stop bubbling
    }
}
```

`RoutedCommand` registers class handlers for `CanExecuteEvent` and `ExecutedEvent` on `RoutedCommandBindableBase` (line 14-18 in `RoutedCommand.cs`). When `CanExecute` or `Execute` is called, it raises a routed event that bubbles up the visual tree until handled by a `RoutedCommandBinding`.

`RoutedCommandBindableBase` (line 88-91) is abstract; you need to subclass it or use an existing control that already subclasses it. In practice, most controls in AvalonUI inherit from `Interactive` which provides `CommandBindings`.

### When to use each approach

| Approach | When to use |
|----------|-------------|
| `{s:Action}` | Most common: binding XAML button to VM method with guard support |
| `RelayCommand` | Programmatic `ICommand` in a ViewModel, no XAML binding needed |
| `RoutedCommand` | Command should bubble/tunnel through visual tree; needs to cross view boundaries |

## See also

- [`src/Sonata.Avalonia/RelayCommand.cs`](../../src/Sonata.Avalonia/RelayCommand.cs) — simple `ICommand`
- [`src/Sonata.Avalonia/RoutedCommand.cs`](../../src/Sonata.Avalonia/RoutedCommand.cs) — AvalonUI routed commands
- [Actions](./actions.md) — `{s:Action}` markup extension for XAML command binding
