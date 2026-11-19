# Run the coop demo

## Requirements

- Unity 6000.7.0b7.
- A Photon Fusion App ID for online rooms.
- Internet access and capacity for two peers.

The repository contains the Fusion 2.1.3 SDK from the source project. Its original notices remain with the files.
The demo does not include an App ID. It stores an entered ID in local PlayerPrefs.
`CORE_PHOTON_APP_ID` can supply the ID through the process environment.

## Start

1. Open this project with the pinned Editor.
2. Wait for package resolution and compilation.
3. Open `Assets/Demo/Scenes/Bootstrap.unity`.
4. Enter Play Mode.
5. Enter your Photon Fusion App ID.
6. Enter a room name.
7. Select **Create room**.
8. Open a built player as the second client.
9. Enter the same App ID.
10. Select **Find rooms**.
11. Refresh the list if necessary.
12. Join the room.

Use WASD or the gamepad left stick to move the demo capsule.
Use Escape or Start to open Pause.
Use **Solo smoke test** to check scene and player initialization without Photon Cloud.
The loading window remains open until the local avatar is ready.

## Two Editor windows

The project includes Multiplayer Play Mode `3.0.0` and the `CoreTwoPlayers` scenario.

1. Select `CoreTwoPlayers` in the scenario dropdown next to Play.
2. Select the `root` button to start from Bootstrap, even when another scene is open.
3. Wait for the additional `Player 2` Editor window.
4. Enter the Fusion App ID in each demo menu.
5. Create a room in one window and join it from the other window.
6. Use the main Editor Stop control to stop the scenario.

Unity restores the authoring scene after Stop.
If `root` is hidden, right-click the toolbar and enable **Core > StartPlay**.
`Tools/Core/Play from bootstrap` runs the same action.
Use **Window > Play Mode > Scenarios** to change the number of additional Editors.
Select `Default` in the scenario dropdown for one Editor or automated tests.

## Reconnect

1. Keep the first client in the room.
2. Select **Disconnect and keep avatar** on the second client.
3. Select **Reconnect** on that client.
4. Verify that the previous avatar remains and no extra avatar appears.

The identity is stable within the same application instance.
Use `--core-client-id alice` and `--core-client-id bob` for separate local player processes.
Use distinct IDs for simultaneous clients. Reuse an ID after a disconnect to restore that avatar.
A room with no remaining peers does not provide durable avatar storage.

## Checks

Run the EditMode tests from Test Runner.
`SessionLifecycleTests` enters Play Mode itself and restores Editor settings.
Save scenes and stop Play Mode before you run this fixture.
The Photon test needs `CORE_PHOTON_APP_ID`. It uses a unique room.
Existing network-core, resource, text-input, input, and navigation tests accompany the extraction.
Refer to [Testing](testing.md) for commands, coverage, reports, and manual checks.

## Build

Use the Standalone build target. Build settings contain Bootstrap followed by World.
The App ID can be entered after the build starts. No source edit is required.

## Reuse

Keep the Core assemblies. Replace the Game demo module and demo assets.
Provide an avatar prefab, a scene director, and a local initialization callback.
The callback returns a lease for resources that belong to the local avatar.
Connect input and windows in the application module. Core Players does not depend on either module.
Follow [Reuse the modules](reuse-coop.md) for folder dependencies, scene setup, and resource ownership.

## Versions

The project uses the source Editor version to avoid downgrading extracted APIs.
Built-in package versions match that Editor. UniTask is pinned to 2.5.11.
Multiplayer Play Mode is pinned to 3.0.0.
R3 Unity is 1.3.1. Its NuGet runtime dependencies are included under `Assets/Packages`.
Addressables is 4.1.1, because the original template version could not resolve in this environment.
The existing asset-service API remains unchanged. The demo uses build-index scenes and Resources.

## Verified checks

On 2026-11-19, 118 EditMode tests passed with no failures or skipped tests.
The total includes 117 project tests and one Addressables package test.
The same run passed the real Photon two-peer reconnect and master-change test.
The Windows 64-bit build succeeded. Refer to [test results](testing.md#latest-local-result).
The demo menu and solo scene were inspected in Play Mode. The Console had no errors.
