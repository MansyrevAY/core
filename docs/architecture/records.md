# Records

## Responsibility

This responsibility gives typed design-time data to the modules. The data is authored in the Unity editor. The code reads it at runtime.

## Parts

| Part | What it does | Folder page |
|------|--------------|-------------|
| `BaseRecord`, `RecordSO`, `RecordStorage` | The data types and the assets that hold the data. | [Services/Data](services/data.md) |
| `IRecordService`, `RecordService` | Finds the record types, loads the storage and gives the records. | [Services/Data](services/data.md) |
| `IService` | The marker for an app-lifetime service. | [Infra/Service](infra/service.md) |

## How it works

1. `Client` calls `RecordService.CreateAll()` before any module exists.
2. The service finds each record type in `Core` and in each assembly that references `Core`. It makes a default instance of each.
3. The service loads the `RecordsStorage` asset through the asset service.
4. The service replaces each default instance with the authored instance of the same type.
5. A module calls `GetRecord<T>()` to read a record.

## Rules

- A record type is a concrete subclass of `BaseRecord` with a constructor without parameters.
- A record must be in the `RecordsStorage` list to get authored values. A record that is not in the list keeps its code defaults.
- All callers share one instance of a record.

## Related responsibilities

- [Asset management](asset-management.md): the service that loads the storage.
- [Bootstrap and lifecycle](bootstrap-and-lifecycle.md): when the records are loaded.
- [Diagnostics](diagnostics.md): how the service reports bad data.
