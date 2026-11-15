# Infra/Messaging

Source folder: `Assets/Scripts/Core/Infra/Messaging`. Files: `IBroadcaster.cs`, `Broadcaster.cs`, `Event.cs`, `AsyncEvent.cs`.

Responsibility: [Messaging](../messaging.md).

## Purpose

`IBroadcaster` is a small event bus. It uses the event type as the key. It has a synchronous version and an asynchronous version. Core defines no events. A game project defines its own events.

## Event types

| Base class | Methods | Notes |
|------------|---------|-------|
| `Event` | `Subscribe`, `Unsubscribe` and `Broadcast` | Synchronous. The sender does not wait. |
| `AsyncEvent` | `SubscribeAsync`, `UnsubscribeAsync` and `BroadcastAsync` | A handler is a `Func<T, UniTask>`. The broadcaster waits for each handler. The class has a `TaskCompletionSource<bool> Source`. The broadcaster does not use it. |

The key is the exact event type (`typeof(T)`). A handler for a base class does not receive the events of a derived class.

## Broadcaster behavior

- **Copy-on-write subscriber arrays.** Subscribe and unsubscribe put a new array in place. They never change an array that is in use. A handler can subscribe or unsubscribe during a broadcast, also for itself. A handler that is removed during a broadcast still receives the event that is now in delivery.
- **Synchronous `Broadcast`.** Each handler runs. If one handler throws an exception, the broadcaster writes it to the log (`Log.Exception`). The other handlers still run.
- **Asynchronous `BroadcastAsync`.** The handlers run one after the other, in the order of subscription. The broadcaster waits for each handler. If a handler throws an exception, the broadcaster writes it to the log and goes to the next handler. An `OperationCanceledException` is thrown again and stops the broadcast.
- If an event has no subscribers, the broadcast does nothing.

Refer to [logging](logging.md).

## Two kinds of bus

| Bus | Holder | Use |
|-----|--------|-----|
| Client broadcaster | `Client.Broadcaster`. Refer to [bootstrap](bootstrap.md). | Events between modules |
| Module broadcaster | `BaseModule.Broadcaster`. `IInternalModule` gives it to the views. Refer to [module](module.md). | Events from a module to its own views |

## Conventions

- A module subscribes in `Start` and unsubscribes in `Dispose`.
- A view subscribes in `ManagedAwake` and unsubscribes in `ManagedDispose`. Refer to [view](view.md).
