# Demo

Source folder: `Assets/Scripts/Game/Demo`. Files: CoopDemoModule.cs, DemoAvatar.cs, DemoUi.cs, DemoPanel.cs, ScreenModel.cs, MenuWindow.cs, RoomsWindow.cs, LoadingWindow.cs, PauseWindow.cs, ConnectionWindow.cs.

Responsibility: [Coop sessions](../coop-sessions.md).

## Purpose

The demo composes the Core modules. It creates runtime uGUI windows and uses the existing `GameClient`. The avatar movement is a small example in Game. It is not part of the player module. The demo contains no RATATA game rules, vehicles, grab system, abilities, or content.

Each window template has its own `Canvas` and `GraphicRaycaster`.
The nested canvas holds the window graphics. Its raycaster lets the EventSystem reach those graphics with pointer events.
The root canvas raycaster alone does not handle the nested window graphics.

The module owns the temporary `InputActionReference` objects for UI input.
Late async cleanup can finish after Play Mode stops. The module uses immediate destruction for those objects in Edit Mode.
