# Game

Source folder: `Assets/Scripts/Game`. Files: `Game.asmdef`, `GameClient.cs`.

Responsibility: [Game starter](../game-starter.md).

## Purpose

`Game` is the assembly for the code of the game. It references `Core` and `UniTask`. `Core` does not reference `Game`.

## Types

### GameClient

`Game.GameClient` is a subclass of `Core.Infra.Bootstrap.Client`. It is a `MonoBehaviour`. The GameObject `GameClient` in `ClientScene` has this component. Refer to [Infra/Bootstrap](../infra/bootstrap.md).

| Method | Behavior now |
|--------|--------------|
| `CreateModules` | Calls the base method. Has no modules. Add the modules of the game here. |
| `StartGame` | Calls the base method. Writes "Game started" to the log. Start the game here. |

A method that you add to these calls must call the base method first.

## Module folders

Make one folder in `Assets/Scripts/Game` for each module. Make one folder page for each folder.

- The source folder `Assets/Scripts/Game/<Name>` has the page `docs/architecture/game/<name>.md`.
- If the folder has sub-folders, the page is `docs/architecture/game/<name>/README.md`. Each sub-folder has its own page next to it.
- Write the names in lowercase. Replace a space with a hyphen.

Each new folder page links to its responsibility pages. Add the folder page to the tables in the [overview](../overview.md).
