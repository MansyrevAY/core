# UI

Source folder: `Assets/Scripts/Core/UI`. Files: UiNavigation.cs, GamepadInputField.cs, UnityTouchKeyboardProvider.cs, PointerEnterRefresher.cs.

Responsibility: [Coop sessions](../coop-sessions.md).

## Purpose

UI depends on Input and Lifetime. It does not depend on Fusion or Players. Standard Unity buttons replace the RATATA audio and tween widgets. [Windows](windows.md) owns the window stack. [Editor providers](editor/README.md) provide desktop text input.
