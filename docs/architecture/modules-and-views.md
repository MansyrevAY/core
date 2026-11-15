# Modules and views

## Responsibility

This responsibility gives the structure of a feature. A module holds the behavior. A view holds the presentation. Core defines the types. A game project makes the features.

## Parts

| Part | What it does | Folder page |
|------|--------------|-------------|
| `IModule`, `BaseModule`, `IInternalModule` | The module types. `BaseModule` gives each module its own event bus. | [Infra/Module](infra/module.md) |
| `ViewBase<T>` | The base class for the views of a module. | [Infra/View](infra/view.md) |

## Shape of a feature

```
IFooModule           public interface for other modules             : IModule
IFooModuleInternal   interface for the views of FooModule           : IInternalModule
FooModule            implementation                                 : BaseModule, IFooModule, IFooModuleInternal
View/FooView         MonoBehaviour for the presentation             : ViewBase<IFooModuleInternal>
```

## How it works

1. Another module uses `IFooModule`.
2. `FooModule` makes a view from a prefab through the asset service.
3. If the view needs more objects, `FooModule` calls the `Inject` method of the view.
4. `FooModule` calls `view.Init(this, assetService)`.
5. The view calls only the methods of `IFooModuleInternal`. It receives events on the module bus.
6. The view releases its resources in `ManagedDispose`.

## Rules

- A view is typed against the `Internal` interface. It cannot call the other methods of the module.
- A view uses `ManagedAwake`, `ManagedStart`, `ManagedUpdate` and `ManagedDispose`. It does not put logic in the Unity methods.
- A view that subscribes to an event must unsubscribe in `ManagedDispose`.

## Related responsibilities

- [Bootstrap and lifecycle](bootstrap-and-lifecycle.md): who makes, starts and disposes the modules.
- [Messaging](messaging.md): the module bus and the client bus.
- [Asset management](asset-management.md): the service that makes the views.
