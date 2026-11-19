# Reuse the coop modules

Use this guide to replace the demo with your game or copy selected modules into another Unity project.
The [demo guide](coop-demo.md) gives the tested Editor and package versions.
The [session architecture](architecture/coop-sessions.md) describes the runtime flow.

## Select modules

Copy each selected folder with its `.asmdef` and `.meta` files. Keep the listed Core dependencies with that folder.

| Module | Source folder under `Assets/Scripts/Core` | Core dependencies | External dependencies |
|--------|------------------------------------------|-------------------|-----------------------|
| Resources | `Resources` | None | None |
| Lifetime | `Infra/Lifetime` | None | UniTask, R3 |
| Network policies | `Networking/Core` | None | None |
| Fusion sessions | `Networking/Fusion` | Resources, Lifetime, Network policies | Photon Fusion, Photon Realtime, UniTask, R3 |
| Players | `Networking/Players` | Fusion sessions and its dependencies | Photon Fusion, UniTask, R3 |
| Input | `Input` | None | Unity Input System |
| Windows | `UI` | Input, Lifetime | uGUI, TextMeshPro, Input System, UniTask, R3 |

Resources and Network policies have no Unity engine references.
Input has no network or UI dependency. Players has no Input or UI dependency.
The nested `Core.UI.Editor` assembly supplies the Editor text-input provider. It does not enter player builds.

The original `Core` assembly supplies application modules, records, asset services, and messages.
The extracted assemblies do not reference that assembly. Keep it if you also use `GameClient` and its boot sequence.

## Keep package and SDK dependencies

1. Use the package versions from `Packages/manifest.json` as the tested baseline.
2. Keep `Assets/Packages` when you use R3. It contains the runtime DLL dependencies.
3. Keep `Assets/Photon/Fusion`, `Assets/Photon/PhotonRealtime`, and `Assets/Photon/PhotonLibs` for the Fusion modules.
4. Keep the SDK notices and `.meta` files.
5. Add the selected assemblies to your application `.asmdef` references.

The `R3.Unity` assembly and the R3 runtime DLL have different names. The assembly definitions reference `R3.Unity`.
URP renders the example assets. The reusable runtime modules do not depend on URP.
Addressables supports the existing asset service. The demo network scenes use build indices and need no addressable catalog.

## Replace the game composition

Use [CoopDemoModule.cs](../Assets/Scripts/Game/Demo/CoopDemoModule.cs) as the complete composition example.
Use [SessionLifecycleTests.cs](../Assets/Tests/Integration/Editor/SessionLifecycleTests.cs) for a smaller composition without windows or input.

1. Add `NetworkService` to an application object that survives scene changes.
2. Create `LocalPlayerIdentity`, `LobbyDirectory`, and `ReconnectStore`.
3. Create `PlayerSessionCoordinator` with the service, identity, level `SceneRef`, and local initialization callback.
4. Call `NetworkService.Configure` before `Init`.
5. Set `NetworkService.InitialScene` to the same level `SceneRef`.
6. Await `Init` and own its returned lease.
7. Own the coordinator and lobby directory in the application scope.
8. Connect your menu to the room commands described in [Fusion](architecture/networking/fusion.md).

The demo adds its application module in `GameClient.CreateModules`.
Replace that module and the `Game` assembly references when your game has different dependencies.
Replace `Assets/Demo` and `Assets/Scripts/Game/Editor` when you no longer need the generated example.
Keep `Assets/Resources/RecordsStorage.asset` if you use the original `Client` boot sequence.

## Supply the scene and avatar

1. Put the bootstrap scene first and the level scene second in the enabled build scenes.
2. Change both composition scene references if your level uses a different index.
3. Add one scene `NetworkObject` with `PlayerSessionDirector` to the level.
4. Assign an avatar prefab through `PlayerSessionDirector.Configure` or its Inspector field.
5. Add `NetworkObject` and `PlayerAvatar` to the prefab.
6. Use `NetworkObjectFlags.MasterClientObject` for the director and avatar, as in the demo.
7. Add your own presentation and movement components. Add `NetworkTransform` if you need transform replication.
8. Save and bake the network objects through the Fusion Editor tools.
9. Rebuild the Fusion prefab table.
10. Include your assemblies with networked members in the Fusion config assembly list.

The current config includes `Core.Networking.Fusion`, `Core.Networking.Players`, and `Game` alongside `Fusion.Unity`.
It enables multiple peers for the integration tests.
The demo setup code shows the complete asset procedure in [DemoSetup.cs](../Assets/Scripts/Game/Editor/DemoSetup.cs).
Its menu command replaces the demo scenes and prefab. Do not run it over customized demo assets that you need.

`FusionSceneManager.AddressableScenePaths` defaults to an empty array.
Explicit addressable scene paths need application setup and a valid content catalog. This path has no integration test here.

## Own local player resources

The local initialization callback receives a `PlayerAvatar` and cancellation token.
Await all required work before you return an `IDisposable` lease.
The bootstrap owns that lease and releases it on leave, cancellation, or failed room initialization.
If your callback fails before it returns a lease, release its partial resources inside the callback.

For example, the demo callback owns `LocalInputService.BindPlayer` and `NetworkService.RegisterInputSource` leases in a `ResourceScope`.
It also clears the local avatar reference when that scope releases.
You can add camera, HUD, or other local resources to the same application-owned lease.

`LocalInputService` copies the supplied action asset. Dispose the service and destroy a temporary source asset when you no longer need it.
`BindPlayer` initially blocks gameplay. Call `SetGameplayBlocked` for the same player object after local setup and menu closure.
Use `WindowsService.CurrentWindow` in your application to control this gate. Windows does not change player state itself.

## Leave and reconnect

Await `LeaveSessionAsync` before you dispose the application composition or request another session.
Use `LeaveMode.KeepReconnect` to keep the locator. Use `ForgetSession` to clear it.
These modes control the local locator. They do not despawn the remote disconnected body.

Use the same local identity to restore a disconnected body. Give simultaneous clients distinct identities.
The demo accepts `--core-client-id`. Your application can pass its own string to `LocalPlayerIdentity`.
This identifier does not authenticate an account.

Another peer must keep the room and its replicated objects alive for avatar restore.
The demo locator expires after ten minutes. That duration does not extend the lifetime of an empty room.
The implementation supports Fusion Shared Mode and local Single mode. Host and dedicated-server flows are not verified.

## Verify the replacement

Run the checks in [Testing](testing.md).
Check create, join, cancel, leave, retry, reconnect, and Shared master changes with your own avatar.
Verify that loading stays visible until local initialization completes.
Check movement and device navigation manually after you replace the presentation.
