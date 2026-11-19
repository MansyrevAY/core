# Lifetime

Source folder: `Assets/Scripts/Core/Infra/Lifetime`. Files: AsyncInitialization.cs, DisposableBehaviour.cs, DisposableClass.cs, ReactiveExtensions.cs, SafeArea.cs.

Responsibility: [Coop sessions](../coop-sessions.md).

## Purpose

The lifetime assembly uses UniTask and R3. Async initialization propagates cancellation and releases partial resources. `DisposableBehaviour<T>` supports explicit model initialization. `SafeArea` adjusts UI bounds.
