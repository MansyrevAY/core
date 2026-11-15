# Game starter

## Responsibility

This responsibility gives a project that is ready to run. A user of Core opens the project and starts to write game logic at once. The scene starts the game. `GameClient` boots Core. The Game assembly is the place for the modules of the game.

## Parts

| Part | What it does | Page |
|------|--------------|------|
| `Game` assembly and `GameClient` | The assembly for the game code. `GameClient` is the subclass of `Client` that starts the game. | [Game](game/README.md) |
| `ClientScene` | The scene that starts the game. It holds a Main Camera and a GameObject with the `GameClient` component. It is the only scene in the build settings. | This page |
| `RecordsStorage` asset | An empty `RecordStorage` in `Assets/Resources`. `Client` loads it at boot. | [Records](records.md) |

Files: `Assets/Scenes/ClientScene.unity`, `Assets/Resources/RecordsStorage.asset`, `Assets/Scripts/Game`.

## How it works

1. The player or the editor runs `ClientScene`.
2. Unity starts the `GameClient` component.
3. `Client` runs the boot sequence. Refer to [Bootstrap and lifecycle](bootstrap-and-lifecycle.md).
4. `Client` loads `RecordsStorage` during `InitializeServices`.
5. `GameClient.CreateModules` runs. It has no modules at the start.
6. `GameClient.StartGame` runs. It writes the message "Game started" to the log.

## How to add game logic

1. Make a folder for the module in `Assets/Scripts/Game`. Make an interface, an internal interface and the module class. Refer to [Modules and views](modules-and-views.md).
2. Build the module in `GameClient.CreateModules`. Add it with `AddModule`. Build the modules in dependency order.
3. Make the first game action in `GameClient.StartGame`.
4. If the module uses records, make a `BaseRecord` subclass in the Game assembly. Add it to `RecordsStorage`. Refer to [Records](records.md).
5. If the module loads prefabs with the default asset service, put the prefabs in a `Resources` folder. Refer to [Asset management](asset-management.md).
6. Write the folder page for the module. Refer to the [conventions](../conventions.md).

## Rules

- Keep the game code in the Game assembly. Do not add game code to Core.
- Do not delete `RecordsStorage`. The default asset service throws an exception when it cannot load the asset, and the boot stops.
- To use more Unity packages in the game code, add their assemblies to the references in `Game.asmdef`.

## Notes

- `Game.asmdef` references `Core` and `UniTask` by name.
- `Assets/Scenes/SampleScene.unity` is the old sample scene. It is not in the build settings.

## Related responsibilities

- [Bootstrap and lifecycle](bootstrap-and-lifecycle.md): the boot sequence that `GameClient` extends.
- [Modules and views](modules-and-views.md): the structure of a module.
