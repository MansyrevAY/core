# Networking

Source folder: `Assets/Scripts/Core/Networking`.

Responsibility: [Coop sessions](../coop-sessions.md).

| Assembly | Purpose |
|----------|---------|
| [Core](core.md) | Pure operations, epochs, registries, and spawn admission. |
| [Fusion](fusion.md) | Runner resources, room commands, and scenes. |
| [Players](players.md) | Avatar registration, reconnect, and local readiness. |

The dependency direction is Players to Fusion to Core.
Input and UI are supplied by the application. Networking does not reference them.
