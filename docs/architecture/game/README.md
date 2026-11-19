# Game

Source folder: `Assets/Scripts/Game`. Files: `Game.asmdef`, `GameClient.cs`.

Responsibility: [Game starter](../game-starter.md).

## Purpose

`Game` contains the game composition and demo consumers. It references Core, the coop assemblies, and their external Unity assemblies. Core assemblies do not reference `Game`.

## Types

### GameClient

`Game.GameClient` is a subclass of `Core.Infra.Bootstrap.Client`. The `GameClient` object in `Bootstrap` has this component. Refer to [Infra/Bootstrap](../infra/bootstrap.md).

| Method | Behavior now |
|--------|--------------|
| `CreateModules` | Calls the base method. Adds `CoopDemoModule`. |
| `StartGame` | Inherits the empty base implementation. The demo module controls startup. |

A method that you add to these calls must call the base method first.

## Module folders

Make one folder in `Assets/Scripts/Game` for each module. Make one folder page for each folder.

- The source folder `Assets/Scripts/Game/<Name>` has the page `docs/architecture/game/<name>.md`.
- If the folder has sub-folders, the page is `docs/architecture/game/<name>/README.md`. Each sub-folder has its own page next to it.
- Write the names in lowercase. Replace a space with a hyphen.

Each new folder page links to its responsibility pages. Add the folder page to the tables in the [overview](../overview.md).

- [Demo module](demo.md)
- [Demo setup](editor.md)
