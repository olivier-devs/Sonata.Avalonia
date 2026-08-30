# Event Aggregator

Weak-reference publish/subscribe event aggregator for decoupled communication between view models and services.

## Key types

| Type | Role | Package |
|------|------|---------|
| `IEventAggregator` | Publish/subscribe interface | `Sonata.Avalonia` |
| `EventAggregator` | Default implementation with weak references | `Sonata.Avalonia` |
| `IHandle` | Marker interface for message handlers | `Sonata.Avalonia` |
| `IHandle<TMessage>` | Handle a specific message type | `Sonata.Avalonia` |
| `Subscribe(handler, channels)` | Subscribe to message types | `IEventAggregator` |
| `Unsubscribe(handler, channels)` | Unsubscribe | `IEventAggregator` |
| `PublishWithDispatcher(message, dispatcher, channels)` | Publish with custom dispatcher | `IEventAggregator` |
| `Publish(message, channels)` | Synchronous publish on current thread | `EventAggregatorExtensions` |
| `PublishOnUIThread(message, channels)` | Dispatch handlers to UI thread | `EventAggregatorExtensions` |
| `EventAggregator.DefaultChannel` | Default channel name (`"DefaultChannel"`) | `EventAggregator` |

## Use cases

### Basic subscribe and publish

Handlers implement `IHandle<TMessage>` for each message type they care about:

```csharp
// A message type
public record OrderPlaced(int OrderId, string CustomerName);

// A handler (from tests/Sonata.Avalonia.Tests/TestFakes.cs)
public class TestMessageHandler : IHandle<string>
{
    public int Received { get; private set; }
    public void Handle(string message) => Received++;
}

// Subscribe
var handler = new TestMessageHandler();
_eventAggregator.Subscribe(handler);

// Publish
_eventAggregator.Publish(new OrderPlaced(42, "Acme Corp"));

// Unsubscribe
_eventAggregator.Unsubscribe(handler);
```

`EventAggregator` stores handlers using `WeakReference` (line 127 in `Handler` class). Dead handlers are cleaned up on every `Publish` (line 111: `handlers.RemoveAll(x => !x.IsAlive)`).

### Channel-based subscriptions

```csharp
// Subscribe to specific channels
_eventAggregator.Subscribe(handler, "Orders", "Notifications");

// Publish to specific channels
_eventAggregator.PublishOnUIThread(new OrderPlaced(42, "Acme"), "Orders");

// Unsubscribe from a specific channel only
_eventAggregator.Unsubscribe(handler, "Notifications");

// Unsubscribe from all channels (no channels param)
_eventAggregator.Unsubscribe(handler);
```

When subscribing without explicit channels, the handler is subscribed to `DefaultChannel`. When publishing without channels, the message goes to `DefaultChannel`.

### Publishing with UI thread dispatch

`PublishOnUIThread` (line 230-233) dispatches handler invocation via `UiThreadDispatch.OnUIThread`:

```csharp
public static void PublishOnUIThread(this IEventAggregator eventAggregator,
    object message, params string[] channels)
{
    eventAggregator.PublishWithDispatcher(message, UiThreadDispatch.OnUIThread, channels);
}
```

This means handlers are **non-blocking** when called from a background thread — the dispatch posts to the UI thread and returns immediately. From the UI thread, handlers run synchronously.

`Publish` (line 241-244) uses a direct dispatch (no thread switch):

```csharp
public static void Publish(this IEventAggregator eventAggregator,
    object message, params string[] channels)
{
    eventAggregator.PublishWithDispatcher(message, a => a(), channels);
}
```

### Handler invocation details

`HandlerInvoker` (line 186-216) uses expression compilation for efficient invocation:

```csharp
// Each Handle method is compiled once into Action<object, object>
var targetParam = Expression.Parameter(typeof(object), "target");
var messageParam = Expression.Parameter(typeof(object), "message");
var castTarget = Expression.Convert(targetParam, targetType);
var castMessage = Expression.Convert(messageParam, messageType);
var callExpression = Expression.Call(castTarget, invocationMethod, castMessage);
invoker = Expression.Lambda<Action<object, object>>(callExpression,
    targetParam, messageParam).Compile();
```

`CanInvoke` uses `IsAssignableFrom` (line 204-207), so a handler for a base class receives all subclasses too.

## See also

- [`src/Sonata.Avalonia/EventAggregator.cs`](../../src/Sonata.Avalonia/EventAggregator.cs) — full implementation with weak references
- [`tests/Sonata.Avalonia.Tests/TestFakes.cs`](../../tests/Sonata.Avalonia.Tests/TestFakes.cs) — `TestMessageHandler` example
- [`tests/Sonata.Avalonia.Tests/EventHygieneTests.cs`](../../tests/Sonata.Avalonia.Tests/EventHygieneTests.cs) — handler subscription tests
- [Dispatching](./dispatching.md) — `UiThreadDispatch` and `IDispatcher`
