# Diagnostics

## Responsibility

This responsibility reports problems. It writes messages and exceptions to the Unity log. It makes sure that a task that nobody waits for does not lose its failure.

## Parts

| Part | What it does | Folder page |
|------|--------------|-------------|
| `Log` | A static wrapper for the Unity `Debug` methods. | [Infra/Logging](infra/logging.md) |
| `TaskUtils.FireAndForget` | Runs a `UniTask` and does not wait. It writes a failure to the log. | [Infra/Utils](infra/utils.md) |

## How it works

- Core code calls `Log.Info`, `Log.Warning`, `Log.Error` or `Log.Exception`. It does not call `Debug` directly.
- `FireAndForget` catches the exceptions of a task. It writes a cancellation as a warning. It writes other exceptions with `Log.Exception`.

## Who reports to the log

| Part | Reported problem |
|------|------------------|
| `Broadcaster` | A handler threw an exception. |
| `ModuleScope` | A module threw an exception in `Dispose`. |
| `ViewBase<T>` | `Init` ran a second time. |
| Asset services | An instance was not destroyed before the unload. |
| `RecordService` | A record is missing or has bad data. |
| `Client.Boot`, `Command.Execute` | A task failed or was cancelled. |

## Related responsibilities

- [Messaging](messaging.md), [Bootstrap and lifecycle](bootstrap-and-lifecycle.md), [Modules and views](modules-and-views.md), [Asset management](asset-management.md), [Records](records.md) and [Commands](commands.md) use this responsibility.
