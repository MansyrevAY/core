# Commands

## Responsibility

This responsibility runs a fixed sequence of actions. Each action is a step. A step can be synchronous or asynchronous.

## Parts

| Part | What it does | Folder page |
|------|--------------|-------------|
| `Command`, `IStep`, `SimpleStep`, `AsyncStep` | The command and its steps. | [Infra/Commands](infra/commands.md) |
| `FireAndForget` | Lets `Execute()` run a command without a wait. | [Infra/Utils](infra/utils.md) |

## How it works

1. A game subclass of `Command` adds its steps in the constructor.
2. A caller runs `ExecuteAsync()` to wait for the result. A caller runs `Execute()` to start the command and not wait.
3. The command waits for each step before it starts the next step.
4. If a step throws an exception, the command stops. `Execute()` writes the exception to the log.

## Rules

- Use a command one time. It never clears its step list.
- Core defines no commands. A game project defines them.

## Related responsibilities

- [Bootstrap and lifecycle](bootstrap-and-lifecycle.md): a game can run a start-up command from `StartGame`.
- [Diagnostics](diagnostics.md): how `Execute()` reports failures.
