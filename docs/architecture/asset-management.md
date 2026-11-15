# Asset management

## Responsibility

This responsibility loads assets by a string id and instantiates prefabs. It counts the live instances of each asset. It releases assets.

## Parts

| Part | What it does | Folder page |
|------|--------------|-------------|
| `IAssetService`, `IInstance`, `IAssetHandle` | The interface, the instance type and the handle type. Also the behavior that both implementations share. | [Services/Asset](services/asset/README.md) |
| `ResourcesAssetService` | Loads from the Unity `Resources` folders. The default. | [Services/Asset/Resource](services/asset/resource.md) |
| `AddressablesAssetService` | Loads with the Addressables package. | [Services/Asset/Addressable](services/asset/addressable.md) |
| `IService` | The marker for an app-lifetime service. | [Infra/Service](infra/service.md) |

## How it works

1. A caller asks for an asset by id.
2. The service looks for a handle with this id. If a handle exists, the service uses it.
3. If no handle exists, the service loads the asset and stores a new handle. If two requests load the same id at the same time, they share one handle.
4. Each `Instantiate` adds 1 to the count of the handle. Each `Destroy` removes 1.
5. When the handle has `UnloadAtZero` set and the count reaches zero, the service releases the asset.

## Users

- `ViewBase<T>` instantiates and destroys prefabs through the service. Refer to [Modules and views](modules-and-views.md).
- `RecordService` loads the record storage. Refer to [Records](records.md).
- `Client.CreateServices` chooses the implementation. Refer to [Bootstrap and lifecycle](bootstrap-and-lifecycle.md).

## Rules

- Make a view or a prefab instance through the service. Do not call `Instantiate` from Unity directly. The count then stays correct.
- The service does not unload an asset automatically. `UnloadAtZero` is `false` by default.

## Related responsibilities

- [Diagnostics](diagnostics.md): the warnings and the errors of the service.
