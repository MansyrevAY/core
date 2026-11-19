# TextInput

Source folder: `Assets/Scripts/Core/UI/Editor/TextInput`. Files: EditorTextInputProvider.cs, EditorTextInputWindow.cs, EditorTextInputWindow.uxml, EditorTextInputWindow.uss.

Responsibility: [Coop sessions](../../coop-sessions.md).

## Purpose

The editor provider registers with active window services during Play Mode. Its utility window supports Unicode text, keyboard confirmation, and cancellation. Provider registration releases when Play Mode ends.
