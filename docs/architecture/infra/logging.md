# Infra/Logging

Source folder: `Assets/Scripts/Core/Infra/Logging`. File: `Log.cs`.

Responsibility: [Diagnostics](../diagnostics.md).

## Purpose

`Log` is a static class. It is a thin wrapper for the Unity `Debug` methods. All Core code uses it for diagnostics. A call site does not use `Debug` directly.

| Method | Unity method |
|--------|--------------|
| `Log.Info(string)` | `Debug.Log` |
| `Log.Warning(string)` | `Debug.LogWarning` |
| `Log.Error(string)` | `Debug.LogError` |
| `Log.Exception(Exception)` | `Debug.LogException` |

## Users

- [messaging](messaging.md): the broadcaster writes handler exceptions to the log.
- [module](module.md): `ModuleScope` writes dispose exceptions to the log.
- [utils](utils.md): `FireAndForget` writes task failures to the log.
- [view](view.md): `ViewBase` writes a warning when `Init` runs twice.
- The services in [Services/Asset](../services/asset/README.md) and [Services/Data](../services/data.md) write warnings and errors.
