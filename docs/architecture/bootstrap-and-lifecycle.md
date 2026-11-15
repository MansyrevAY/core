# Bootstrap and lifecycle

## Responsibility

This responsibility starts the application. It builds the services and the modules in the correct order. It starts the modules. It stops them in the reverse order.

## Parts

| Part | What it does | Folder page |
|------|--------------|-------------|
| `Client` | The composition root. It runs the boot sequence. | [Infra/Bootstrap](infra/bootstrap.md) |
| `IModule`, `ModuleScope` | The lifecycle contract of a module. The scope starts and disposes a group of modules. | [Infra/Module](infra/module.md) |
| `IService` | The marker for a service that lives as long as the application. | [Infra/Service](infra/service.md) |

## How it works

1. `Client.Awake` makes the client event bus and the services.
2. `Client` waits for the services to initialize. The record service loads the records.
3. `Client` builds the modules by hand, in dependency order. It adds each module to the `ModuleScope`.
4. The `ModuleScope` starts the modules in the order of addition.
5. `Client` calls `StartGame`. A game subclass starts the game here.
6. When `Client` is destroyed, the `ModuleScope` disposes the modules in reverse order.

There is no DI container and no service locator. A constructor receives each dependency. A module can only use a dependency that was made before the module.

## Rules

- A module does its side effects in `Start`. It reverses them in `Dispose`.
- A module does not look up another module by type.
- A service finishes its initialization before the first module is built.

## Related responsibilities

- [Modules and views](modules-and-views.md): the structure of a module.
- [Asset management](asset-management.md) and [Records](records.md): the services that `Client` makes.
- [Commands](commands.md): a game can run a command from `StartGame`.
