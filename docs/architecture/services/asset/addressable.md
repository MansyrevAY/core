# Services/Asset/Addressable

Source folder: `Assets/Scripts/Core/Services/Asset/Addressable`. Files: `AddressablesAssetService.cs`, `ObjectAssetHandle.cs`, `GameObjectAssetHandle.cs`, `AssetHandle.cs`.

Responsibility: [Asset management](../../asset-management.md).

## Purpose

`AddressablesAssetService` implements `IAssetService` with the Unity Addressables package. `Client.CreateServices` does not use it. A subclass of `Client` must choose it. Refer to [Infra/Bootstrap](../../infra/bootstrap.md).

The interface and the common behavior are in the [Services/Asset](README.md) page. This folder is the only part of Core that uses the Addressables types.

## Ids

An id is an addressable address. The service loads an asset with `Addressables.LoadAssetAsync`.

## Handles

| Type | Purpose |
|------|---------|
| `ObjectAssetHandle<T>` | Holds the `AsyncOperationHandle<T>`. `Release` calls `Addressables.Release` for the operation. |
| `GameObjectAssetHandle` | `ObjectAssetHandle<GameObject>`. The service uses it for prefabs. |
| `AssetHandle` | A struct. No code uses it. |

## Behavior details

- A prefab handle starts with a count of 0. Each `Instantiate` adds 1. A plain `Load` handle starts with a count of 1.
- If a load fails, the service releases the operation. It throws `InvalidOperationException("Failed to load addressable asset ...")` with the operation exception as the inner exception.
- If another request registered the same id during the wait, the service releases its own duplicate operation. It uses the registered handle.

## Notes

- `AssetHandle` has `UnloadAtZero` set to `true` in its constructor. The handles that the service uses set `UnloadAtZero` to `false` by default. Do not use `AssetHandle` as a model for the behavior.
