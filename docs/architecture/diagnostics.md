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

## Rules

- Avoid `try` and `catch` blocks as much as possible. Do not use a `catch` block to hide a failure or to control the flow.
- Prevent the error instead. Check the condition before the operation. Validate the input and return early.
- Let an exception go up to a place that already handles it. These places are `FireAndForget`, `Broadcaster`, `ModuleScope` and `Command.Execute`.
- Use `try` and `finally` only to release a resource that has no other cleanup. Use `catch` only when the code can recover or must add context to the failure.
- If you must use `catch`, catch the narrowest exception type. Write the exception with `Log.Exception` or throw it again. Do not leave a `catch` block empty.

## Related responsibilities

- [Messaging](messaging.md), [Bootstrap and lifecycle](bootstrap-and-lifecycle.md), [Modules and views](modules-and-views.md), [Asset management](asset-management.md), [Records](records.md) and [Commands](commands.md) use this responsibility.
