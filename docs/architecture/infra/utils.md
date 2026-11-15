# Infra/Utils

Source folder: `Assets/Scripts/Core/Infra/Utils`. File: `TaskUtils.cs`.

Responsibilities: [Diagnostics](../diagnostics.md), [Commands](../commands.md).

## Purpose

`TaskUtils` is a static class with extension methods for tasks.

## FireAndForget

`FireAndForget(this UniTask task)` runs a task and does not wait for it.

- If the task fails, the method writes the exception to the log (`Log.Exception`).
- If the task is cancelled, the method writes a warning. UniTask cancels with `OperationCanceledException`. `TaskCanceledException` derives from it, so the same code handles both.
- The method name is different from the UniTask method `Forget`. The two names are never ambiguous.

Refer to [logging](logging.md).

`Client.Boot` and `Command.Execute` use this method. Refer to [bootstrap](bootstrap.md) and [commands](commands.md).

## Notes

- `TaskUtils` has no overload for `System.Threading.Tasks.Task`. A caller must convert a `Task` to a `UniTask` first.
