# Core

Source folder: `Assets/Scripts/Core/Networking/Core`. Files: AvatarRegistry.cs, NetworkOperationGate.cs, OperationResult.cs, RosterDelta.cs, RunnerEpoch.cs, SceneRegistrationOperation.cs, ShutdownBarrier.cs, SpawnAdmission.cs.

Responsibility: [Coop sessions](../coop-sessions.md).

## Purpose

This assembly contains pure C# policies copied from RATATA. Operations serialize room commands. Leave cancels an active command and waits for cleanup. Runner generations reject stale callbacks. Spawn admission records duplicate requests, reservations, and authority changes.
