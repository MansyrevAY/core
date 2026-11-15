# Messaging

## Responsibility

This responsibility sends events between parts of the code that do not know each other. It gives a synchronous bus and an asynchronous bus.

## Parts

| Part | What it does | Folder page |
|------|--------------|-------------|
| `IBroadcaster`, `Broadcaster` | The event bus. It keys the subscribers by event type. | [Infra/Messaging](infra/messaging.md) |
| `Event`, `AsyncEvent` | The base classes of the events. | [Infra/Messaging](infra/messaging.md) |
| `Client.Broadcaster` | The client bus. | [Infra/Bootstrap](infra/bootstrap.md) |
| `BaseModule.Broadcaster` | The bus of one module. | [Infra/Module](infra/module.md) |

## How it works

- A sender calls `Broadcast` for an `Event`. Each handler runs. The sender does not wait.
- A sender calls `BroadcastAsync` for an `AsyncEvent`. The handlers run one after the other. The sender waits for each handler.
- A failing handler does not stop the other handlers. The bus writes the failure to the log. A cancellation stops an asynchronous broadcast.
- The subscriber list is copy-on-write. A handler can subscribe or unsubscribe during a broadcast.

## Two buses

| Bus | Use |
|-----|-----|
| Client bus | Events between modules. |
| Module bus | Events from a module to its own views. |

## Rules

- A module subscribes in `Start` and unsubscribes in `Dispose`.
- A view subscribes in `ManagedAwake` and unsubscribes in `ManagedDispose`.
- Core defines no events. A game project defines its own events.

## Related responsibilities

- [Modules and views](modules-and-views.md): the owners of the module bus.
- [Diagnostics](diagnostics.md): how the bus reports handler failures.
