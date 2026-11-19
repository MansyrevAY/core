# Windows

Source folder: `Assets/Scripts/Core/UI/Windows`. Files: WindowsService.cs, WindowBase.cs.

Responsibility: [Coop sessions](../coop-sessions.md).

## Purpose

The window service maps model types to window templates. `Configure` supplies the anchor and templates. `ConfigureInput` supplies local input. Opening a window clears transient input and restores navigation focus. Window subscriptions release on close. The game supplies concrete windows and controls gameplay blocking.

## Setup

Supply one template for each concrete model type before you open a window.
Each template needs a `Canvas`, `GraphicRaycaster`, and `WindowBase<T>` component.
The raycaster must be on the canvas that contains the interactive graphics, including a nested window canvas.
The demo creates inactive templates at runtime. A game can use prefabs instead.
Await the service `Init` before use, and own its returned lease.

`ConfigureInput` assigns the input action asset to existing `InputSystemUIInputModule` components.
The application still connects point, click, navigation, submit, and cancel references.
The demo's `CreateInterface` method shows that setup.

## Stack behavior

`Open` defaults to `isDisplacing: true`. It closes the current stack before it opens the requested window.
With `false`, it pushes the requested window and assigns its sort order.
`Close` removes the top window. `CloseAll` removes the stack.
The service reuses one instance per model type. Use distinct model types for different windows.

`CurrentWindow` lets the application block local gameplay while a window is active.
Window changes clear transient input so that the action that closed a window does not activate gameplay.
`WindowBase<T>.Disposables` releases on close and before the next open.

## Text input

The service owns one `TextInputService`. Configure custom providers through that service.
`GamepadInputField` uses it for platform keyboards.
Cancel closes an active text-input session before it calls the current window's `Cancel`.
Disposing the window service closes windows, releases their instances, and disposes text input and observables.
