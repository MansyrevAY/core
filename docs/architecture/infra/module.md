# Infra/Module

Source folder: `Assets/Scripts/Core/Infra/Module`. Files: `IModule.cs`, `BaseModule.cs`, `IInternalModule.cs`, `ModuleScope.cs`.

Responsibilities: [Bootstrap and lifecycle](../bootstrap-and-lifecycle.md), [Modules and views](../modules-and-views.md).

## Purpose

A module holds the behavior of one feature. A view holds the presentation. Core defines the module types. A game project uses them for each feature. A feature has this shape:

```
IFooModule           public interface for other modules             : IModule
IFooModuleInternal   interface for the views of FooModule           : IInternalModule
FooModule            implementation                                 : BaseModule, IFooModule, IFooModuleInternal
View/FooView         MonoBehaviour for the presentation             : ViewBase<IFooModuleInternal>
```

Refer to [view](view.md) for `ViewBase<T>`.

## Types

### IModule

```csharp
public interface IModule : IDisposable
{
    void Start();
}
```

- A module gets its dependencies through the constructor.
- `Start` does side effects only. For example, it subscribes to events, makes entities or finds scene objects.
- `Dispose` reverses what `Start` did.

### IInternalModule

`IInternalModule` has one member: `IBroadcaster Broadcaster`. A view is typed against an `Internal` interface. The view can call only the methods that the module chooses to give it.

### BaseModule

`BaseModule` gives each module its own `Broadcaster`. This bus is a private channel between the module and its views. It is not the client bus. Refer to [messaging](messaging.md).

A module implements its public interface and its internal interface. Other modules receive the module as the public interface. If a module must give an object to a view, it uses the `Internal` interface.

### ModuleScope

`ModuleScope` controls the life of a group of modules. The group can be the whole client or one scene. `ModuleScope` is not a registry.

- `Add<T>(module)` stores the module and returns it. The order of the `Add` calls is the start order.
- `Start()` starts the modules in the order of addition.
- `Dispose()` disposes the modules in reverse order. Dependents are disposed before their dependencies. Then it clears the list.
- If a module throws an exception in `Dispose`, `ModuleScope` writes the exception to the log. It then continues with the other modules.

The order of construction is the order of dependency. A module can only receive a dependency that was made and added before the module.

`Client` holds one `ModuleScope` for the app-lifetime modules. Refer to [bootstrap](bootstrap.md). A game project can make more `ModuleScope` objects. For example, it can make one for each scene.
