# Infra/Bootstrap

Source folder: `Assets/Scripts/Core/Infra/Bootstrap`. File: `Client.cs`.

Responsibilities: [Bootstrap and lifecycle](../bootstrap-and-lifecycle.md), [Messaging](../messaging.md).

## Purpose

`Core.Infra.Bootstrap.Client` is the composition root of the application. It is a `MonoBehaviour`. `Game.GameClient` is the subclass of this project. Refer to [Game](../game/README.md).

`Client` builds the services and the modules by hand. It holds the `ModuleScope` for the app-lifetime modules. Refer to [module](module.md).

## Boot order

1. `Awake` makes the client `Broadcaster`. It calls `CreateServices()`. Then it starts `Boot()` and does not wait for the result.
2. `Boot()` stores `destroyCancellationToken` before it waits for `InitializeServices()`. The base method calls `RecordService.CreateAll()`. The records are loaded before any module exists.
3. If the client is destroyed during this load (`destroyCancellationToken`), `Boot()` stops.
4. `CreateModules()` builds the modules. The base method is empty.
5. `ModuleScope.Start()` starts each module in the order of addition.
6. `StartGame()` runs. The base method is empty.

`OnDestroy` disposes the module scope.

```
Client.Awake
 ├─ new Broadcaster()
 ├─ CreateServices()
 └─ Boot()  (asynchronous)
     ├─ InitializeServices()
     ├─ CreateModules()
     ├─ ModuleScope.Start()
     └─ StartGame()
```

## Extension points

A subclass can change these `protected virtual` methods:

| Method | Base behavior |
|--------|---------------|
| `CreateServices` | Makes `ResourcesAssetService` and `RecordService`. |
| `InitializeServices` | Returns `Records.CreateAll()`. A subclass that adds work must call the base method first if the work needs the records. |
| `CreateModules` | Empty. |
| `StartGame` | Empty. |

## Members

| Member | Purpose |
|--------|---------|
| `Broadcaster` | Public property. The client event bus. Refer to [messaging](messaging.md). |
| `Assets` | Protected property. The `IAssetService`. |
| `Records` | Protected property. The `IRecordService`. |
| `AddModule<T>(module)` | Adds a module to the scope and returns it. |

To use Addressables, a subclass overrides `CreateServices`. It makes an `AddressablesAssetService` for `Assets` and gives it to `RecordService`. Refer to [Services/Asset](../services/asset/README.md).

## Notes

- `Boot()` runs through `FireAndForget`. An exception during boot is written to the log. It is not thrown.
- `Client` does not dispose the services. It disposes only the modules.
