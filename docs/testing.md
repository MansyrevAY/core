# Test the coop foundation

## Requirements

Use Unity `6000.7.0b7`. Save open scenes and stop Play Mode before a test run.
Select the `Default` Play Mode Scenario before automated tests. The `CoreTwoPlayers` scenario starts an additional Editor.
`SessionLifecycleTests` enters Play Mode itself from the EditMode suite.
The fixture restores the scene setup, play options, and input focus settings after each test.

The online test requires internet access, capacity for two peers, and a Fusion App ID in `CORE_PHOTON_APP_ID`.
Start the Editor or test process from a shell that has this variable.
An App ID entered in the demo menu does not configure the online test.
Without the variable, this test reports **Skipped**. The other tests do not need Photon Cloud.

## Run from the Editor

1. Open **Window > General > Test Runner**.
2. Select **EditMode**.
3. Run all tests or select one test assembly.
4. Inspect failed and skipped results.
5. Inspect the Console for unexpected errors.

Select `Core.Tests.Integration.SessionLifecycleTests.TwoPeersReconnectToExistingAvatarAndSurviveOwnerLeaving` for the real Photon check.
The test creates a unique room and two runners in one Editor process.
It does not start two separate executable processes.

## Run from PowerShell

The following commands use Unity CLI `1.0.0-beta.15`. Run them from the repository root.
Close the Editor for this project first. A headless Editor cannot use the same project lock.

```powershell
$project = (Get-Location).Path
$editor = 'U:/UnityClient/6000.7.0b7/Editor/Unity.exe'
unity test $project --mode EditMode --editor-path $editor `
    --output "$project/Logs/validation-editmode.xml" --timeout 360 -- `
    -nographics -logFile "$project/Logs/validation-editmode.log"
```

Change `$editor` to your installed Editor path.
Set `CORE_PHOTON_APP_ID` in this shell before the command to include the online test.
Do not put the App ID into source files or documentation.

To run only the session fixture, add this option before the standalone `--`:

```text
--filter Core.Tests.Integration.SessionLifecycleTests
```

The XML report contains the result, count, duration, failure message, and stack trace.
Use its test-case results as evidence. A process exit code alone does not establish that tests ran.
`Logs` is ignored by Git. Keep reports locally or attach them separately when you share results.

## Automated coverage

| Assembly | Checks | Current count |
|----------|--------|---------------|
| `Core.Networking.Tests` | Operation gate, stale generations, shutdown, resource scopes, registry, scene registration, spawn admission | 66 |
| `Core.Input.Tests` | Player input leases, gameplay gates, button transitions, device hints, text-input providers | 41 |
| `Core.UI.Tests` | Focus selection and navigation links | 4 |
| `Core.Integration.Tests` | Runner lifecycle, local initialization, demo windows, Play Mode stop, real reconnect, Shared master change | 7 |

The integration fixture verifies these behaviors:

- A solo session leaves and starts with a fresh runner and a larger generation.
- Each local initialization lease releases once on leave, including a second session.
- Cancellation during local initialization releases the runner and returns `Disconnected`.
- A local initialization failure releases the session and permits a successful retry.
- A lease returned after cancellation still releases once.
- The demo starts solo through a mouse click, opens Pause with Escape, and returns through a mouse click on Leave.
- Stopping Play Mode releases the demo input references even when async cleanup finishes in Edit Mode.
- A second Photon peer reconnects to the same avatar without a duplicate body.
- The remaining peer gains director authority after the Shared master leaves.

The failure test expects one `InvalidOperationException` log. It simulates the failed callback and checks recovery.
Do not treat this expected log as an unhandled test failure. Unexpected error logs fail Unity tests.

The demo test queues mouse position, press, and release events through the Input System.
It checks EventSystem raycast results before each click. It does not invoke `Button.onClick` directly.

## Build check

Use **Build Profiles** with the enabled Bootstrap and World scenes, or use the headless command below.
The CLI option permits a local build from the uncommitted extraction. It does not commit or push changes.

```powershell
unity build $project --target StandaloneWindows64 `
    --output-path "$project/Builds/CoopDemo/CoopDemo.exe" `
    --editor-path $editor --allow-dirty-build --no-tail `
    --log-file "$project/Logs/validation-build.log" --timeout 360
```

## Manual checks

Use one Editor and one built player with distinct local identities.

1. Create a room and join it through the room list.
2. Move each capsule and observe it from the other client.
3. Open Pause and verify that local movement stops.
4. Cancel a connection and try again.
5. Disconnect the second client with **Disconnect and keep avatar**.
6. Reconnect that client and check that no extra capsule appears.
7. Leave on the Shared master and continue from the remaining client.
8. Leave and start another room.

Automated tests do not measure camera smoothness, gamepad comfort, or physical mobile keyboards.
They do not cover packet-loss recovery, long sessions, or an application-specific authentication system.

## Latest local result

On 2026-11-19, the headless EditMode run passed **118 tests**, with **0 failures** and **0 skipped**.
This total includes 117 project tests and one Addressables package test.
All six session integration tests passed, including the real Photon test.
The run used the pinned Editor and the project package versions.

The Windows 64-bit build succeeded with exit code 0.
Its log contains initial license-client handshake errors, followed by a successful license update and **Build Finished, Result: Success**.
The earlier Play Mode inspection checked the menu and solo scene with no Console errors.

Local reports: `Logs/validation-editmode.xml` and `Logs/validation-editmode.log`.
Build log: `Logs/validation-build.log`.
These files can be replaced by the next run. Test results apply to the tested local state.

After the nested-window raycaster fix, the pointer-driven demo test passed and all four navigation tests passed.
Before the fix, the same demo test failed because the Solo button had no UI raycast hit.
The earlier version of this test invoked `onClick` directly and did not verify pointer delivery.
An ordinary Play Mode mouse click also started the Solo scene after the fix. The Console had no errors.
Local focused reports: `Logs/click-before-fix.json`, `Logs/click-after-fix.json`, and `Logs/click-ui-navigation.json`.
The updated Windows build succeeded with zero errors. Its report is `Logs/click-build.json`.

After the Multiplayer Play Mode transfer, all seven session tests passed, including real Photon reconnect and Play Mode stop cleanup.
The focused report is `Logs/mppm-session-tests.json`.
The `root` button started Bootstrap from World with `CoreTwoPlayers` selected. Stop restored World and cleared the temporary start-scene override.
Both Editor processes joined the same Photon room. Each peer reported two peers, two avatars, and a ready local avatar.
The local report is `Logs/mppm-two-editors.json`.

## Multiplayer Play Mode diagnostic

Unity `6000.7.0b7` reported four `Asset Database is set to Read Only` messages in the additional Editor at startup.
The messages came from the Unity clone asset refresh path. Both peers still completed the room check above.
The Fusion installer, Hub asset-creation exception, and late demo cleanup errors did not recur after their fixes.
The remaining startup messages are not suppressed. The local report is `Logs/mppm-second-launch-console.json`.
See the [related Unity custom-dependency report](https://issuetracker.unity3d.com/issues/multiplayer-play-mode-using-custom-dependencies-and-scriptedimporters-triggers-asset-database-is-set-to-read-only-errors-in-virtual-instances).
