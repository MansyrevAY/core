# Players

Source folder: `Assets/Scripts/Core/Networking/Players`. Files: PlayerService.cs, PlayerAvatar.cs, PlayerSessionContext.cs, PlayerSessionCoordinator.cs, LocalPlayerBootstrapper.cs, PlayerSpawnCoordinator.cs, PlayerSessionDirector.cs, ReplicatedSpawnRequest.cs.

Responsibility: [Coop sessions](../coop-sessions.md).

## Purpose

`PlayerService` owns the avatar registry for one runner generation. `PlayerSessionDirector` replicates spawn admission and binds disconnected avatars. The Shared master owns the director and avatars. Master ownership transfers when that peer leaves. `PlayerSessionCoordinator` awaits scene registration, spawn, and local readiness. A callback initializes the local presentation and returns its resource lease. This assembly has no dependency on Input or UI.

## Player state

| Member | Meaning |
|--------|---------|
| `PlayerAvatar.Identity` | Stable token-derived identity for body restore. |
| `PlayerAvatar.Player` | Current peer bound to this body. |
| `PlayerAvatar.IsDisconnected` | The body remains, but its peer no longer participates. |
| `PlayerAvatar.IsReady` | The bound peer completed local initialization and the authority accepted readiness. |

`Bind` resets readiness when the director assigns a peer.
`MarkDisconnected` clears the peer and readiness while it keeps the body and identity.
`RPC_SetReady` checks that the sender matches the bound peer.
The authority rejects another active peer that requests an identity already in use.

## Local initialization

The coordinator installs a new `PlayerSessionContext` on each runner.
That context contains the registry, spawn coordinator, local bootstrap, and scene director reference.
The coordinator waits for scene registration before it requests a body.
The preparation deadline is 45 seconds. The spawn coordinator has a 30-second deadline.

The application callback receives the avatar and a cancellation token.
The bootstrap owns the callback's returned lease before it checks cancellation again.
It then requests readiness, waits for replicated `IsReady`, and publishes `LocalPlayer`.
The network service publishes `InRoom` only after this sequence completes.

Release partial resources inside a callback that throws before it returns its lease.
Do not hide required initialization in an unobserved task. The loading window depends on this callback's completion.
The demo callback binds input. Your callback can also prepare a camera or HUD.

## Reconnect and ownership

The director checks active peers and marks departed bodies as disconnected.
A later request with the same identity binds that existing body and repeats local initialization.
`PlayerService.TryGet` returns only a connected body for the requested peer.
`FindIdentity` can also return a retained disconnected body.

The scene director and avatar network objects use the Master Client Object flag in the demo.
The Shared master owns their state. The remaining master takes authority when that owner leaves.
This model does not give each client state authority over its own avatar.
Game components must send authorized requests to the current state authority, as `DemoAvatar` does.

The room must retain a peer for body restore. No durable body store exists after the last peer leaves.
Refer to [Testing](../../testing.md) for the real two-peer reconnect and authority test.
