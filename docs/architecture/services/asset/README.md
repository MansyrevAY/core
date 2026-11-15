# Services/Asset

Source folder: `Assets/Scripts/Core/Services/Asset`. Files: `IAssetService.cs`, `IInstance.cs`, `DefaultInstance.cs`, `IAssetHandle.cs`.

Responsibility: [Asset management](../../asset-management.md).

## Purpose

`IAssetService` loads and instantiates assets by a string id. It counts the instances of each asset that are alive. It is an `IService`. Refer to [Infra/Service](../../infra/service.md).

There are two implementations. They have the same behavior. They differ in the source of the assets.

| Implementation | Folder | Page |
|----------------|--------|------|
| `ResourcesAssetService` | `Resource` | [resource](resource.md) |
| `AddressablesAssetService` | `Addressable` | [addressable](addressable.md) |

## Interface

| Method | Purpose |
|--------|---------|
| `UniTask<GameObject> Instantiate(string id)` | Loads the prefab if necessary. Instantiates it. |
| `UniTask<T> Instantiate<T>(string id) where T : MonoBehaviour` | Does the same and returns the component. Throws an exception if the prefab has no `T`. |
| `GameObject Instantiate<T>(IInstance<T>)` | Instantiates an asset that is already loaded. It is synchronous. |
| `UniTask<T> Load<T>(string id)` | Loads an asset and does not instantiate it, for example `RecordStorage`. |
| `void Destroy<T>(IInstance<T>)` | Destroys an instance. Decrements the count. Unloads the asset at zero if the handle requests it. |
| `void Unload<T>(IInstance<T>)` | Removes the handle. Releases the asset. |

## Supporting types

- `IInstance<out T>` has `Id` and `Value`. `Value` is the object in the scene. `DefaultInstance<T>` implements it.
- `IAssetHandle` is the record for one asset. It has `Count`, `UnloadAtZero`, `UnloadRequested`, `Asset`, `AddInstance()`, `RemoveInstance()` and `Release()`.

## Common behavior

- Each service keeps a dictionary of handles. The key is the id.
- Two loads of the same id at the same time are safe. After the wait, the service looks in the dictionary again. It uses the handle that was added first. All callers share one handle, so the instance count stays correct.
- If a load fails, the service throws `InvalidOperationException`.
- If the prefab has no component `T`, `Instantiate<T>` reverses the instance count and destroys the copy. Then it throws a `NullReferenceException`.
- `Destroy` and `Unload` throw `ArgumentOutOfRangeException` for an unknown id. `Unload` logs a warning if instances are still counted. Refer to [Infra/Logging](../../infra/logging.md).
- `UnloadAtZero` is `false` by default. For this reason, the service does not unload an asset automatically. A caller can set the flag on a handle.

## Users

- `ViewBase<T>` uses `Instantiate` and `Destroy`. Refer to [Infra/View](../../infra/view.md).
- `RecordService` uses `Load`. Refer to [Services/Data](../data.md).
- `Client.CreateServices` makes the service. Refer to [Infra/Bootstrap](../../infra/bootstrap.md).

## Notes

- The type of `IAssetHandle.Asset` is `Object`. A `TODO` comment in the code asks for a stronger type.
