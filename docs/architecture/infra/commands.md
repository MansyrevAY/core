# Infra/Commands

Source folder: `Assets/Scripts/Core/Infra/Commands`. Files: `Command.cs`, `IStep.cs`, `SimpleStep.cs`, `AsyncStep.cs`.

Responsibility: [Commands](../commands.md).

## Purpose

A `Command` is an ordered list of steps. It runs the steps one after the other.

```csharp
public abstract class Command
{
    public void Execute();               // runs ExecuteAsync and does not wait; the log shows failures
    public async UniTask ExecuteAsync(); // waits for each step in order
    protected void Add(Action action);          // adds a SimpleStep
    protected void Add(Func<UniTask> action);   // adds an AsyncStep
}
```

## Types

| Type | Purpose |
|------|---------|
| `Command` | Abstract base class. It holds the list of steps. |
| `IStep` | One step. `ExecuteAsync()` returns a `UniTask`. |
| `SimpleStep` | Wraps a synchronous `Action`. It returns a completed task. |
| `AsyncStep` | Wraps a `Func<UniTask>`. |

## Rules

- A subclass adds the steps in its constructor.
- The command never clears the step list. Use a command one time.
- If a step throws an exception, the command stops. The remaining steps do not run. `Execute()` writes the exception to the log.

## Use

Core defines no commands. A game project makes a subclass of `Command` for a sequence of actions. For example, a game can make a start-up command. The subclass of `Client` can create and run it in `StartGame`. Refer to [bootstrap](bootstrap.md).
