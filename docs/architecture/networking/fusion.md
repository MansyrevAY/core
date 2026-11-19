# Fusion

Source folder: `Assets/Scripts/Core/Networking/Fusion`.

Files: NetworkService.cs, NetworkService.Operations.cs, FusionRunnerSession.cs, FusionSceneManager.cs, NetworkSceneLoader.cs, LobbyDirectory.cs, ReconnectStore.cs, LocalPlayerIdentity.cs, NetworkSnapshot.cs, NetworkEvent.cs, NetworkInputData.cs, IPlayerInputSource.cs, IRunnerScopeInstaller.cs, SharedAuthorityLease.cs, NetworkDisposableBehaviour.cs, NetworkServiceLink.cs, NetworkExtensions.cs, SessionData.cs, ConnectionState.cs.

Responsibility: [Coop sessions](../coop-sessions.md).

## Purpose

`NetworkService` owns callbacks and room commands. A fresh `FusionRunnerSession` owns each runner. The service accepts an `IRunnerScopeInstaller`. It does not know player avatar types. `InitialScene` selects the first network scene. `LocalPlayerIdentity` accepts an explicit identity for separate local clients.

## Composition contract

Call `Configure` with the installer, directory, reconnect store, and identity before `Init`.
The application owns the directory and installer lifetimes.
The runner owns the lease returned by `IRunnerScopeInstaller.Install`.
`PrepareRoomAsync` must complete before the service publishes `InRoom`.
`PrepareLeaveAsync` releases local player resources before runner shutdown.

## Room commands

| Method | Purpose |
|--------|---------|
| `ConnectToLobbyAsync` | Resolve regions and connect to the Shared lobby. |
| `RefreshLobbyAsync` | Replace the lobby connection and refresh its room list. |
| `CreateSessionAsync` | Create a Shared room or a local Single session. |
| `JoinSessionAsync` | Join an existing room by name and region. |
| `JoinRandomSessionAsync` | Join a random room without a saved reconnect locator. |
| `LeaveSessionAsync` | Release the session and apply the local reconnect policy. |

Each command returns `OperationResult`. Inspect `Status`, `Error`, `OperationId`, and `Generation` before you update application UI.
`Cancelled`, `Busy`, and `Rejected` are distinct from `Failed`.
Do not call Fusion runner shutdown directly from game menus. Use the service so that its operation gate and resource barrier remain valid.

## State and cleanup

`Snapshot` contains the current phase, generation, session locator, local peer, owner flag, and peer roster.
`Events` publishes phase, participant, and owner changes.
`OnUnexpectedDisconnect` lets the application show its connection window.
`Directory.Sessions` contains the current lobby room list.

The service retires a generation before it shuts down the runner. Callbacks from a retired runner have no effect.
`ShutdownBarrier` shares one shutdown operation between callers.
The runner releases scene state, input registration, and installed resources after shutdown completes.
A shutdown deadline failure blocks replacement while cleanup remains unconfirmed. Do not bypass that barrier with a reused runner.

## Identity and scene selection

`LocalPlayerIdentity` uses PlayerPrefs unless the application supplies an override string.
Editor processes add their process identity. An Editor restart can therefore change the default identity.
Use an explicit identity when you need restore across separate process launches.
The token-derived number selects a body. It does not prove account ownership.

The demo loads a build-index scene. `FusionSceneManager` accepts explicit addressable paths when the application needs them.
The default empty path list avoids addressable catalog discovery for build-index scenes.
Refer to [Reuse the modules](../../reuse-coop.md) for the scene and config procedure.
