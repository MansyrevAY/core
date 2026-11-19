# Core coop foundation

A modular Unity coop foundation extracted from RATATA into the existing Core framework.

- Unity: **6000.7.0b7**.
- Open `Assets/Demo/Scenes/Bootstrap.unity` and press Play.
- Use `root` to start from another scene. Select `CoreTwoPlayers` next to Play for an additional Editor window.
- Enter a Photon Fusion App ID for online rooms, or select **Solo smoke test**.
- [Run, build, and reuse the demo](docs/coop-demo.md).
- [Module boundaries and session lifecycle](docs/architecture/coop-sessions.md).
- [Reuse modules in another project](docs/reuse-coop.md).
- [Test commands and coverage](docs/testing.md).
- [Full documentation](docs/README.md).

Networking, players, input, UI, resources, and component lifetime use separate assemblies.
Game rules, vehicles, abilities, and the grab system are not included.
