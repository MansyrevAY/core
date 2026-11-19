# Architecture overview

## Assemblies

The code is in `Assets/Scripts`. It uses separate assembly definitions for independent modules.

| Assembly | Path | References | Purpose |
|----------|------|------------|---------|
| `Core` | `Assets/Scripts/Core` | UniTask, `Unity.Addressables`, `Unity.ResourceManager` | The shared infrastructure and services |
| `Game` | `Assets/Scripts/Game` | Core modules, Fusion, uGUI | The coop demo. |
| `Core.Resources` | `Assets/Scripts/Core/Resources` | None | Pure resource scopes and leases. |
| `Core.Lifetime` | `Assets/Scripts/Core/Infra/Lifetime` | UniTask, R3 | Async component lifetime. |
| `Core.Networking.Core` | `Assets/Scripts/Core/Networking/Core` | None | Pure network policies. |
| `Core.Networking.Fusion` | `Assets/Scripts/Core/Networking/Fusion` | Network core, Resources, Lifetime, Fusion, UniTask, R3 | Rooms and runners. |
| `Core.Networking.Players` | `Assets/Scripts/Core/Networking/Players` | Network core, Fusion module, Resources, Lifetime, UniTask, R3 | Avatar lifetime. |
| `Core.Input` | `Assets/Scripts/Core/Input` | Input System | Local controls and text providers. |
| `Core.UI` | `Assets/Scripts/Core/UI` | Input, Lifetime, UniTask, R3, uGUI | Windows and navigation. |
| `Core.UI.Editor` | `Assets/Scripts/Core/UI/Editor` | Input, UI, Lifetime, Input System, uGUI | Editor text-input provider. |
| `Game.Editor` | `Assets/Scripts/Game/Editor` | Game, Players, Fusion, uGUI, URP | Demo asset setup. |

`Game` references `Core`. `Core` does not reference `Game`.

The `Core` code does not know the types of the game. There is one runtime link to the game assembly. `RecordService` scans each loaded assembly that references `Core`, to find the record types of the game. Refer to [Records](records.md).

The asset service uses Addressables in its `Addressable` folder. The Fusion scene manager also supports explicit addressable scene paths.

## How the docs are organized

The docs have three levels.

1. [docs/README.md](../README.md) lists the responsibility pages.
2. Each responsibility page describes one job of the code. It shows how the parts work together. It links to the folder pages.
3. Each folder page describes the types in one source folder. It links back to its responsibility pages.

The folder pages are in `docs/architecture/infra`, `docs/architecture/services` and `docs/architecture/game`. They have the same names and the same layout as the folders in `Assets/Scripts/Core` and `Assets/Scripts/Game`.

## Responsibilities

| Responsibility | Folder pages |
|----------------|--------------|
| [Bootstrap and lifecycle](bootstrap-and-lifecycle.md) | [Infra/Bootstrap](infra/bootstrap.md), [Infra/Module](infra/module.md), [Infra/Service](infra/service.md) |
| [Modules and views](modules-and-views.md) | [Infra/Module](infra/module.md), [Infra/View](infra/view.md) |
| [Messaging](messaging.md) | [Infra/Messaging](infra/messaging.md) |
| [Commands](commands.md) | [Infra/Commands](infra/commands.md), [Infra/Utils](infra/utils.md) |
| [Diagnostics](diagnostics.md) | [Infra/Logging](infra/logging.md), [Infra/Utils](infra/utils.md) |
| [Asset management](asset-management.md) | [Services/Asset](services/asset/README.md), [Services/Asset/Resource](services/asset/resource.md), [Services/Asset/Addressable](services/asset/addressable.md), [Infra/Service](infra/service.md) |
| [Records](records.md) | [Services/Data](services/data.md), [Infra/Service](infra/service.md) |
| [Game starter](game-starter.md) | [Game](game/README.md) |
| [Coop sessions](coop-sessions.md) | [Network](networking/README.md), [Input](input/README.md), [UI](ui/README.md), [Resources](resources.md), [Lifetime](infra/lifetime.md) |

The demo starts in `Assets/Demo/Scenes/Bootstrap.unity`. `World.unity` is the second enabled build scene.
See the [coop diagrams](coop-sessions.md#application-composition) for composition, room entry, and resource cleanup.
See the [Editor launch diagram](game/editor.md#editor-launch-flow) for `root` and the two-Editor scenario.

## Main ideas

- **No DI container and no service locator.** Code builds all objects by hand in a composition root. The order of construction is the order of dependency. Constructors receive the dependencies. Nothing is found by type. Refer to [Bootstrap and lifecycle](bootstrap-and-lifecycle.md).
- **A module holds the behavior. A view holds the presentation.** Refer to [Modules and views](modules-and-views.md).
- **Events separate modules from views.** Refer to [Messaging](messaging.md).
- **A service lives as long as the application.** `Client` makes each service one time. Refer to [Asset management](asset-management.md) and [Records](records.md).

## Boot sequence

`Client` is a `MonoBehaviour`. `GameClient` is the subclass in the Game assembly. The `GameClient` object in `Bootstrap` has this component.

```
Client.Awake
 ├─ new Broadcaster()                       (client event bus)
 ├─ CreateServices()   -> ResourcesAssetService, RecordService
 └─ Boot()  (asynchronous)
     ├─ InitializeServices()  -> RecordService.CreateAll()  (loads RecordsStorage)
     ├─ CreateModules()       (GameClient adds the modules of the game)
     ├─ ModuleScope.Start()
     └─ StartGame()           (GameClient starts the game)
```

[Infra/Bootstrap](infra/bootstrap.md) gives the details of each step.

## Extension points

The Game assembly changes the behavior of Core with these members of `Client`: `CreateServices`, `InitializeServices`, `CreateModules`, `StartGame` and `AddModule<T>`. It also makes its own `BaseRecord` subclasses, modules, views, events and commands.

## Coop modules

- [Coop sessions](coop-sessions.md)
- [Run the demo](../coop-demo.md)

## Coop folder pages

| Folder | Page |
|--------|------|
| Resources | [Resources](resources.md) |
| Infra/Lifetime | [Lifetime](infra/lifetime.md) |
| Networking | [Networking](networking/README.md) |
| Networking/Core | [Core](networking/core.md) |
| Networking/Fusion | [Fusion](networking/fusion.md) |
| Networking/Players | [Players](networking/players.md) |
| Input | [Input](input/README.md) |
| Input/TextInput | [Text input](input/textinput.md) |
| UI | [UI](ui/README.md) |
| UI/Windows | [Windows](ui/windows.md) |
| UI/Editor | [Editor](ui/editor/README.md) |
| UI/Editor/TextInput | [Editor text input](ui/editor/textinput.md) |
| Game/Demo | [Demo](game/demo.md) |
| Game/Editor | [Setup](game/editor.md) |
