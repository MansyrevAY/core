# TextInput

Source folder: `Assets/Scripts/Core/Input/TextInput`. Files: TextInputService.cs, ITextInputProvider.cs, DelegateTextInputProvider.cs, TextInputRequest.cs, TextInputResult.cs.

Responsibility: [Coop sessions](../coop-sessions.md).

## Purpose

Providers register with priorities. One text session can be active. Session disposal closes its provider and reports cancellation. Requests preserve Unicode text and limit submitted length without splitting surrogate pairs.
