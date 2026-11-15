# Architecture overview

## Assemblies

The code is in `Assets/Scripts`. It has two assembly definitions.

| Assembly | Path | References | Purpose |
|----------|------|------------|---------|
| `Core` | `Assets/Scripts/Core` | UniTask, `Unity.Addressables`, `Unity.ResourceManager` | The shared infrastructure and services |
| `Game` | `Assets/Scripts/Game` | `Core`, UniTask | The code of the game. Refer to [Game starter](game-starter.md). |

`Game` references `Core`. `Core` does not reference `Game`.

The `Core` code does not know the types of the game. There is one runtime link to the game assembly. `RecordService` scans each loaded assembly that references `Core`, to find the record types of the game. Refer to [Records](records.md).

Only the `Addressable` folder uses the Addressables types.

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

The scene that starts the game is `Assets/Scenes/ClientScene.unity`. It is the only scene in the build settings.

## Main ideas

- **No DI container and no service locator.** Code builds all objects by hand in a composition root. The order of construction is the order of dependency. Constructors receive the dependencies. Nothing is found by type. Refer to [Bootstrap and lifecycle](bootstrap-and-lifecycle.md).
- **A module holds the behavior. A view holds the presentation.** Refer to [Modules and views](modules-and-views.md).
- **Events separate modules from views.** Refer to [Messaging](messaging.md).
- **A service lives as long as the application.** `Client` makes each service one time. Refer to [Asset management](asset-management.md) and [Records](records.md).

## Boot sequence

`Client` is a `MonoBehaviour`. `GameClient` is the subclass in the Game assembly. The GameObject `GameClient` in `ClientScene` has it.

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
