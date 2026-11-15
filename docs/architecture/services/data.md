# Services/Data

Source folder: `Assets/Scripts/Core/Services/Data`. Files: `IRecordService.cs`, `RecordService.cs`, `BaseRecord.cs`, `RecordSO.cs`, `RecordStorage.cs`.

Responsibility: [Records](../records.md).

## Purpose

`IRecordService` gives typed design-time data ("records") to the modules. It is an `IService`. Refer to [Infra/Service](../infra/service.md).

## Types

| Type | Purpose |
|------|---------|
| `BaseRecord` | `[Serializable]` abstract class with an abstract `Name`. A subclass defines one data set. |
| `RecordSO` | ScriptableObject. It holds one record through `[SerializeReference]`. |
| `RecordStorage` | ScriptableObject with a `RecordSO[]`. The asset with the id `RecordsStorage` lists all records. |
| `IRecordService` | `CreateAll()` and `GetRecord<T>()`. |
| `RecordService` | The implementation. It uses `IAssetService` to load the storage. |

The `RecordsStorage` asset must be in a place that the asset service can load, for example a `Resources` folder. Refer to [Services/Asset](asset/README.md).

## RecordService.CreateAll

1. It clears the record dictionary.
2. It scans each loaded assembly that is not dynamic and that is `Core` or references `Core`. It finds each concrete `BaseRecord` subclass that has a constructor without parameters. It makes a default instance of each. If an assembly cannot load all its types, `CreateAll` throws and the boot stops.
3. It loads `RecordsStorage` through the asset service. It replaces each default instance with the stored instance of the same type.

`GetRecord<T>()` returns the record. If the type is unknown, it logs an error and returns `null`.

## Bad data

| Case | Result |
|------|--------|
| `RecordsStorage` is `null` or has no list | Warning. The service keeps the default records. |
| An entry in the list is empty | Warning. The service skips the entry. |
| An entry has a type with no default record | Error. The service skips the entry. |

Refer to [Infra/Logging](../infra/logging.md).

## Add a record in a game project

1. Make a subclass of `BaseRecord` in the game assembly.
2. Make a `RecordSO` asset that holds the record.
3. Add the asset to the list in `RecordsStorage`.

## Notes

- All users share the record instances. The instances can change. A game must not store runtime state in a record unless it accepts this.
- `CreateAll` reads the types of every assembly that references `Core`. It does not need a fixed assembly name.
- `CreateAll` does not catch `ReflectionTypeLoadException`. A type that fails to load in a game assembly is a defect that must be fixed.
