# Infra/View

Source folder: `Assets/Scripts/Core/Infra/View`. File: `ViewBase.cs`.

Responsibility: [Modules and views](../modules-and-views.md).

## Purpose

`ViewBase<T>` is the base class for the views of a module.

`ViewBase<T> : MonoBehaviour, IDisposable where T : class, IInternalModule`

`T` is the `Internal` interface of the module. Refer to [module](module.md).

## Connection

A view is a MonoBehaviour, so it has no constructor arguments. The code connects it in these steps:

1. The module makes the view from a prefab through `IAssetService`.
2. If the view needs more than its module, the view has a typed `Inject(...)` method. The module calls `Inject` before `Init`.
3. The module calls `view.Init(module, assetService)`.

`Init` does these actions:

- It logs a warning and returns if the view is already initialized.
- It stores `Module`. It takes `Broadcaster` from `module.Broadcaster`. It keeps the asset service.
- It calls `ManagedAwake()` and then `ManagedStart()`. Both calls are synchronous. The view does not put logic in the Unity `Awake` and `Start` methods.

## Lifecycle methods

A subclass overrides these methods, not the Unity methods:

| Method | When it runs |
|--------|--------------|
| `ManagedAwake` | During `Init`. Subscribe to events here. |
| `ManagedStart` | During `Init`, after `ManagedAwake`. |
| `ManagedUpdate` | Each frame, from `Update`. It runs only when the view is initialized and not disposed. |
| `ManagedDispose` | One time, when `Dispose()` or `OnDestroy` runs first. It does not run if the view was never initialized. |

## Helper members

| Member | Purpose |
|--------|---------|
| `Module` | The `Internal` interface of the module. |
| `Broadcaster` | The module bus. Refer to [messaging](messaging.md). |
| `InitChild(view)` | Initializes a child view with the same module and asset service. |
| `Instantiate(IInstance<T>)` | Instantiates an asset through the asset service. The instance count stays correct. |
| `Destroy(IInstance<T>)` | Destroys an instance through the asset service. |

Refer to [Services/Asset](../services/asset/README.md).

## Notes

- `ViewBase` has a private `OnDestroy`. If a subclass declares its own `OnDestroy`, it hides the base method and disposal does not run. Override `ManagedDispose` instead. A comment in the code gives the same warning.
- A view that subscribes in `ManagedAwake` must unsubscribe in `ManagedDispose`.
