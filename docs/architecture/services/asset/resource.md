# Services/Asset/Resource

Source folder: `Assets/Scripts/Core/Services/Asset/Resource`. Files: `ResourcesAssetService.cs`, `ResourceAssetHandle.cs`.

Responsibility: [Asset management](../../asset-management.md).

## Purpose

`ResourcesAssetService` implements `IAssetService` with the Unity `Resources` system. It is the default asset service. `Client.CreateServices` makes it. Refer to [Infra/Bootstrap](../../infra/bootstrap.md).

The interface and the common behavior are in the [Services/Asset](README.md) page.

## Ids

An id is a path inside any `Resources` folder. The service loads an asset with `Resources.LoadAsync`.

## Handles

`ResourceAssetHandle` holds the loaded `Object`. It implements `IAssetHandle`.

- `AddInstance` and `RemoveInstance` change `Count`. `RemoveInstance` never goes below zero.
- `UnloadRequested` is `true` when `UnloadAtZero` is `true` and `Count` is zero.
- `Release` does nothing for a `GameObject` or a `Component`. Unity does not let code call `Resources.UnloadAsset` on them. `Resources.UnloadUnusedAssets` frees prefabs.
- For other assets, `Release` calls `Resources.UnloadAsset`.

## Behavior details

- A prefab handle starts with a count of 0. Each `Instantiate` adds 1. A plain `Load` handle starts with a count of 1.
- `Unload` releases the asset of the handle. It does not release `instance.Value`, because that object is the scene copy.
- If `Resources.LoadAsync` returns no asset, the service throws `InvalidOperationException("Failed to load resource ...")`.
