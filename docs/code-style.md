# Code style

These rules apply to the C# code in `Assets/Scripts`. The rules for the documents are in [conventions](conventions.md). Refer to the [README](README.md) for the index.

## Naming

- Start the name of a private member with an underscore (`_`). Use camelCase after it, for example `_modules`.
- Start the name of a protected or public member with an upper case letter.
- Start the name of a property with an upper case letter.
- Write the names of classes and methods in PascalCase.
- Write the name of a constant in upper case. Use an underscore between words, for example `CARD_TITLE`.

## Formatting

- Put an opening curly bracket on the next line.
- Use `var` where the language allows it.
- Always use curly brackets with `if`, `else`, `for`, `foreach` and `while`. Do not write them as a single statement without brackets.

## Order of members

Put the members of a type in this order:

1. Constants, fields and properties.
2. The constructor and the init and dispose methods. The Unity message methods, for example `Awake`, `Update` and `OnDestroy`, are in this group.
3. Public methods.
4. Protected methods.
5. Private methods.

If a method calls another method, put the calling method higher in the file. This rule is more important than the order of the groups. Apply it to methods in the same group and to methods in different groups.
