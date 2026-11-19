# Documentation

This folder holds the project documentation for **Core**. Core is a Unity project (`6000.7.0b7`). It contains shared services and separate coop modules. The `Game` assembly composes these modules in a runnable demo. Add game modules, views, and records to this assembly.

## Practical guides

- [Run and build the coop demo](coop-demo.md).
- [Reuse the modules in another project](reuse-coop.md).
- [Run tests and inspect results](testing.md).

## How to read these docs

The docs have three levels.

1. **This README** lists the responsibility pages.
2. **Responsibility pages** describe one job of the code and how the parts work together. They link to the folder pages.
3. **Folder pages** describe the types in one source folder. They link back to their responsibility pages.

## Layout

```
docs/
├── README.md                   this file
├── conventions.md              rules for the language, structure and content of the docs
├── code-style.md               rules for the naming, formatting and order of the C# code
└── architecture/
    ├── overview.md             assembly, boot sequence, main ideas
    ├── <responsibility>.md     high-level pages (level 2)
    ├── infra/                  folder pages for Assets/Scripts/Core/Infra
    ├── services/               folder pages for Assets/Scripts/Core/Services
    └── game/                   folder pages for Assets/Scripts/Game
```

## Architecture documents

Read the [overview](architecture/overview.md) first. Then read the responsibility page that you need.

| Responsibility | Summary |
|----------------|---------|
| [Bootstrap and lifecycle](architecture/bootstrap-and-lifecycle.md) | How `Client` starts the application and how modules start and stop |
| [Modules and views](architecture/modules-and-views.md) | The structure of a feature: a module and its views |
| [Messaging](architecture/messaging.md) | The event buses between modules and between a module and its views |
| [Commands](architecture/commands.md) | Fixed sequences of synchronous and asynchronous steps |
| [Diagnostics](architecture/diagnostics.md) | The log and the handling of tasks that nobody waits for |
| [Asset management](architecture/asset-management.md) | Loading, instantiating and releasing assets (Resources and Addressables) |
| [Records](architecture/records.md) | Typed design-time data that modules read at runtime |
| [Game starter](architecture/game-starter.md) | The `Game` assembly, demo scenes, and steps to add game logic |
| [Coop sessions](architecture/coop-sessions.md) | Diagrams of Core composition, room entry, player lifetime, input, windows, and resource cleanup |

## Conventions

The rules for the language, the structure and the content of these docs are in [conventions](conventions.md). The rules for the C# code are in [code style](code-style.md). All documents use ASD-STE100 Simplified Technical English.
