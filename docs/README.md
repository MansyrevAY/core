# Documentation

This folder holds the project documentation for **Core**. Core is a Unity project (`6000.2.6f2`). It contains the infrastructure and the services that game projects share. The project also has a `Game` assembly and a scene that starts the game. A user of Core adds game modules, views and records to the Game assembly.

## How to read these docs

The docs have three levels.

1. **This README** lists the responsibility pages.
2. **Responsibility pages** describe one job of the code and how the parts work together. They link to the folder pages.
3. **Folder pages** describe the types in one source folder. They link back to their responsibility pages.

## Layout

```
docs/
├── README.md                   this file
├── conventions.md              rules for the language, structure and content
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
| [Game starter](architecture/game-starter.md) | The `Game` assembly and the `ClientScene` that start the game, and how to add game logic |

## Conventions

The rules for the language, the structure and the content of these docs are in [conventions](conventions.md). All documents use ASD-STE100 Simplified Technical English.
