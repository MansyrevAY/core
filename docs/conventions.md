# Documentation conventions

These rules apply to all documents in `docs/`. Refer to the [README](README.md) for the index.

## Language

Write all documents in ASD-STE100 Simplified Technical English. Use these rules:

- Write short sentences. Use 20 words or fewer in an instruction. Use 25 words or fewer in a description.
- Write 6 sentences or fewer in a paragraph. Give one topic to each paragraph.
- Use the active voice. Use the simple present tense for facts. Use the simple past or future tense only when you must.
- Do not use the progressive tense or the present perfect tense. Do not use an -ing form as a noun.
- Use one word for one meaning. Use the same name for the same thing in all pages.
- Use simple words. Write "use", not "utilize". Write "start", not "initiate". Write "make sure", not "ensure".
- Use the articles "a", "an" and "the". Do not omit "that".
- Use "must" for a required action. Use "can" for a possible action.
- Do not use contractions. Do not use Latin abbreviations such as "e.g.", "i.e." and "etc.". Write "for example" in full.
- Write a code name exactly as it is in the code. Put it in backticks. A code name is a technical name, so the language rules do not change it.
- Put a list of steps in numbered order. Put one action in each step.

This is an approximation of the standard. It is not checked with the official ASD-STE100 dictionary or a checker tool.

## Structure

The docs have three levels. Refer to the [README](README.md).

- The README links to the responsibility pages and to the overview. It does not link to the folder pages.
- A responsibility page has these sections: Responsibility, Parts, How it works, Rules and Related responsibilities. The Parts table links to the folder pages.
- Make one folder page for each folder in `Assets/Scripts/Core` and in `Assets/Scripts/Game`. Use the same folder names and the same layout as the source. Write the names in lowercase and replace a space with a hyphen. A folder with sub-folders has a `README.md` page. Each sub-folder has its own page next to it.
- Use these rules to find the page for a source folder:
  - Core: the folder `Assets/Scripts/Core/<A>/<B>` has the page `docs/architecture/<a>/<b>.md`.
  - Game: the folder `Assets/Scripts/Game/<A>` has the page `docs/architecture/game/<a>.md`.
  - If the folder has sub-folders, the page is the `README.md` in a folder with the name of the source folder. For example, the page for `Assets/Scripts/Game/Foo` with sub-folders is `docs/architecture/game/foo/README.md`.
- Start each folder page with the source folder path and the file names. Then add a link to its responsibility pages.
- Give the details in the folder page. Give the summary and the flow in the responsibility page. Do not repeat the details.
- If a flow uses more than one folder, put the flow in the responsibility page. Link to the folder pages with relative links.

## Content

- Describe what exists in the code, not plans. If a feature has a known problem, put it under a **Notes** heading. Do not describe the problem as a design decision.
- Name a type (for example `ModuleScope`). Give the file path one time, near the top of the folder page.
- Describe the Core code and the code in the Game assembly of this repository. Do not describe the code of other projects that use Core.
- Document each module that you add to the Game assembly in the same way as the Core code.

## Maintenance

- When a code change alters the structure that a page describes, change that page in the same commit.
- When a code folder is added, renamed or removed, change the folder pages, the responsibility pages and the tables in the [overview](architecture/overview.md).
- When you add or remove a responsibility page, change the table in the [README](README.md) and the table in the overview.
