# Core

A Unity project (`6000.2.6f2`) with shared infrastructure and services (`Assets/Scripts/Core`) and a ready `Game` assembly (`Assets/Scripts/Game`). The scene `Assets/Scenes/ClientScene.unity` starts the game through `GameClient`. A user of Core adds game logic to the Game assembly. The code is C#. It uses UniTask and Addressables.

## Documentation

All project documentation is in [docs/](docs/README.md). It has three levels:

1. [docs/README.md](docs/README.md) lists the responsibility pages.
2. The responsibility pages in [docs/architecture/](docs/architecture/overview.md) describe one job of the code each. Read the [overview](docs/architecture/overview.md) first.
3. The folder pages in `docs/architecture/infra`, `docs/architecture/services` and `docs/architecture/game` describe the types in one source folder each. They have the same layout as `Assets/Scripts/Core` and `Assets/Scripts/Game`.

The rules for the docs are in [docs/conventions.md](docs/conventions.md). Read them before you write or change a doc.

### Before you edit code

1. Find the doc page for the source folder that you will edit:
   - `Assets/Scripts/Core/<A>/<B>` has the page `docs/architecture/<a>/<b>.md`.
   - `Assets/Scripts/Game/<A>` has the page `docs/architecture/game/<a>.md`.
   - If the folder has sub-folders, the page is `README.md` in a folder with the name of the source folder.
   - The [overview](docs/architecture/overview.md) lists all pages.
2. Read that page. Read the responsibility pages that it links to.
3. Make the code change.
4. Compare the change with the page. If the page is now wrong or incomplete, change the page in the same commit.

### Code conventions

- Make the folder names reflect the namespace names. The namespace of a file is the path of its folder under `Assets/Scripts`. For example, `Assets/Scripts/Core/Infra/Module` has the namespace `Core.Infra.Module`.
- Do not use spaces or underscores in a folder name. Use PascalCase.
- Set the `rootNamespace` of each assembly definition to the name of the assembly (`Core`, `Game`).
- Follow the [code style](docs/code-style.md) rules. In short:
  - Start a private member with `_`. Start a protected member, a public member and a property with an upper case letter. Use PascalCase for classes and methods. Write a constant in upper case with underscores, for example `CARD_TITLE`.
  - Put an opening curly bracket on the next line. Use `var` where possible. Always use brackets with `if` and loops.
  - Order the members: fields and properties, then the constructor and init and dispose methods, then public methods, then protected methods, then private methods. A calling method goes above the methods that it calls. This rule is more important than the order of the groups.
- Use minimal comments in code.
- Avoid `try` and `catch` blocks as much as possible. Refer to the rules in [Diagnostics](docs/architecture/diagnostics.md#rules).

### Game assembly

- Put the game code in `Assets/Scripts/Game`. Do not put game code in Core.
- Make one folder for each module. Make one folder page for each folder in `docs/architecture/game`.
- Add each module in `GameClient.CreateModules` and start the game in `GameClient.StartGame`. The [Game starter](docs/architecture/game-starter.md) page has the steps.
- Do not delete `Assets/Resources/RecordsStorage.asset`. The boot sequence loads it.

### Rules for the documentation

- Write all documents in ASD-STE100 Simplified Technical English.
- When you add, rename or remove a source folder or a responsibility page, change the tables that list them. The [maintenance rules](docs/conventions.md#maintenance) name the tables.
- Do not describe plans in `docs/architecture`. Describe only what exists in the code.
