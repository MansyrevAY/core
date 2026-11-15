# Infra/Service

Source folder: `Assets/Scripts/Core/Infra/Service`. File: `IService.cs`.

Responsibilities: [Bootstrap and lifecycle](../bootstrap-and-lifecycle.md), [Asset management](../asset-management.md), [Records](../records.md).

## Purpose

`IService` is an empty marker interface. It marks a service that lives as long as the application.

- A service gets its dependencies through the constructor.
- `Client.CreateServices` makes the services. Refer to [bootstrap](bootstrap.md).
- A module receives a service through its constructor. Refer to [module](module.md).

## Implementations

| Interface | Folder |
|-----------|--------|
| `IAssetService` | [Services/Asset](../services/asset/README.md) |
| `IRecordService` | [Services/Data](../services/data.md) |
