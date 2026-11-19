using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.LowLevel;

namespace Core.Input
{
    public sealed class LocalInputService : IDisposable
    {
        private readonly InputActionAsset _actions;
        private readonly Dictionary<GameInputAction, InputAction> _commands = new();
        private readonly HashSet<GameInputAction> _pendingPresses = new();
        private readonly Dictionary<GameInputAction, string> _bindingLabels = new();
        private readonly Dictionary<StickControl, Vector2> _stickActivity = new();
        private object _player;
        private long _bindingVersion;
        private bool _blocked = true;
        private bool _disposed;
        private readonly HashSet<GameInputAction> _requiresRelease = new();
        private readonly Dictionary<InputAction, GameInputAction> _actionCommands = new();
        private int _contextChangedFrame = -1;

        public event Action DeviceChanged;

        public InputActionAsset Actions => _actions;
        public bool IsGamepad { get; private set; }
        public bool GameplayEnabled => !_disposed && _player != null && !_blocked;
        public Vector2 Move => GameplayEnabled ? GetAction(GameInputAction.Move).ReadValue<Vector2>() : Vector2.zero;
        public Vector2 PointerPosition => GetAction(GameInputAction.Point).ReadValue<Vector2>();

        public LocalInputService(InputActionAsset actions)
        {
            if (!actions)
            {
                throw new ArgumentNullException(nameof(actions));
            }

            _actions = UnityEngine.Object.Instantiate(actions);
            foreach (GameInputAction command in Enum.GetValues(typeof(GameInputAction)))
            {
                var map = GetMapName(command);
                var path = $"{map}/{command}";
                var action = _actions.FindAction(path, true);
                _commands[command] = action;
                _actionCommands[action] = command;
            }

            foreach (var map in _actions.actionMaps)
            {
                map.actionTriggered += OnActionTriggered;
            }

            InputSystem.onEvent += OnInputEvent;
            InputSystem.onDeviceChange += OnDeviceChange;
            // Continue sampling behind the gameplay gate so pausing does not synthesize held-button releases.
            _actions.Enable();
        }

        public IDisposable BindPlayer(object player)
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(LocalInputService));
            }

            if (player == null)
            {
                throw new ArgumentNullException(nameof(player));
            }

            _player = player;
            _bindingVersion++;
            _blocked = true;
            ClearTransientInput();
            return new PlayerBinding(this, _bindingVersion);
        }

        public void SetGameplayBlocked(object player, bool blocked)
        {
            if (!ReferenceEquals(_player, player) || _blocked == blocked)
            {
                return;
            }

            _blocked = blocked;
            ClearTransientInput();
        }

        public InputAction GetAction(GameInputAction command)
        {
            return _commands[command];
        }

        public bool IsPhysicallyHeld(GameInputAction command)
        {
            return !_disposed && GetAction(command).IsPressed();
        }

        public bool IsHeld(GameInputAction command)
        {
            if (!IsAvailable(command))
            {
                return false;
            }

            if (_requiresRelease.Contains(command))
            {
                return false;
            }

            return GetAction(command).IsPressed();
        }

        public bool WasPressed(GameInputAction command)
        {
            if (!IsAvailable(command) || _requiresRelease.Contains(command) || Time.frameCount == _contextChangedFrame)
            {
                return false;
            }

            return GetAction(command).WasPressedThisFrame();
        }

        public bool ConsumePress(GameInputAction command)
        {
            var pending = _pendingPresses.Remove(command);
            var pressed = pending && IsAvailable(command);
            return pressed;
        }

        public bool CollectJump()
        {
            var pressed = ConsumePress(GameInputAction.Jump);
            return pressed || IsHeld(GameInputAction.Jump);
        }

        public Vector2 GetLookDelta(float mouseSensitivity, float gamepadDegreesPerSecond, float deltaTime)
        {
            if (!GameplayEnabled)
            {
                return Vector2.zero;
            }

            var action = GetAction(GameInputAction.Look);
            var value = action.ReadValue<Vector2>();
            if (action.activeControl?.device is Gamepad)
            {
                return value * (gamepadDegreesPerSecond * deltaTime);
            }

            // Legacy Mouse X/Y used sensitivity 0.1 in InputManager.asset.
            return value * (0.1f * mouseSensitivity);
        }

        public string GetBindingLabel(GameInputAction command)
        {
            if (_bindingLabels.TryGetValue(command, out var label))
            {
                return label;
            }

            var group = IsGamepad ? "Gamepad" : "Keyboard&Mouse";
            var mask = InputBinding.MaskByGroup(group);
            label = GetAction(command).GetBindingDisplayString(mask);
            _bindingLabels[command] = label;
            return label;
        }

        public void ClearTransientInput()
        {
            _pendingPresses.Clear();
            _contextChangedFrame = Time.frameCount;
            _requiresRelease.Clear();
            foreach (var entry in _commands)
            {
                if (RequiresReleaseAfterContextChange(entry.Key) && entry.Value.IsPressed())
                {
                    _requiresRelease.Add(entry.Key);
                }
            }
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            _player = null;
            InputSystem.onEvent -= OnInputEvent;
            _stickActivity.Clear();
            InputSystem.onDeviceChange -= OnDeviceChange;
            foreach (var map in _actions.actionMaps)
            {
                map.actionTriggered -= OnActionTriggered;
            }

            _actions.Disable();
            if (Application.isPlaying)
            {
                UnityEngine.Object.Destroy(_actions);
            }
            else
            {
                UnityEngine.Object.DestroyImmediate(_actions);
            }

            DeviceChanged = null;
        }

        private static bool RequiresReleaseAfterContextChange(GameInputAction command)
        {
            return command is GameInputAction.Jump or GameInputAction.Crouch;
        }

        private bool IsAvailable(GameInputAction command)
        {
            return !_disposed && (GetMapName(command) != "Gameplay" || GameplayEnabled);
        }

        private static string GetMapName(GameInputAction command)
        {
            return command switch
            {
                GameInputAction.Move or GameInputAction.Look or GameInputAction.Jump or
                    GameInputAction.Sprint or GameInputAction.Crouch => "Gameplay",
                GameInputAction.Pause => "Common",
                _ => "UI"
            };
        }

        private void OnDeviceChange(InputDevice device, InputDeviceChange change)
        {
            if (change is not (InputDeviceChange.Removed or InputDeviceChange.Disconnected or InputDeviceChange.Disabled))
            {
                return;
            }

            ClearTransientInput();
            if (device is Gamepad gamepad)
            {
                _stickActivity.Remove(gamepad.leftStick);
                _stickActivity.Remove(gamepad.rightStick);
            }

            if (IsGamepad && device is Gamepad)
            {
                IsGamepad = false;
                _bindingLabels.Clear();
                DeviceChanged?.Invoke();
            }
        }

        private void OnInputEvent(InputEventPtr eventPtr, InputDevice device)
        {
            if (eventPtr.type != StateEvent.Type && eventPtr.type != DeltaStateEvent.Type)
            {
                return;
            }

            if (device is not (Gamepad or Keyboard or Mouse))
            {
                return;
            }

            var activity = false;
            if (device is Gamepad gamepad)
            {
                // Sample both sticks even if the first one moved, keeping each baseline current.
                activity = HasStickActivity(gamepad.leftStick, eventPtr) |
                    HasStickActivity(gamepad.rightStick, eventPtr);
            }
            else if (device is Mouse mouse)
            {
                activity = (mouse.delta.ReadValueFromEvent(eventPtr, out var delta) && delta.sqrMagnitude >= 1f) ||
                    (mouse.position.ReadValueFromEvent(eventPtr, out var position) &&
                        (position - mouse.position.ReadValue()).sqrMagnitude >= 1f) ||
                    (mouse.scroll.ReadValueFromEvent(eventPtr, out var scroll) && scroll.sqrMagnitude > 0f);
            }

            // Changed controls exclude noisy sensors and synthetic stick-direction buttons.
            foreach (var control in eventPtr.EnumerateChangedControls(device))
            {
                if (control is ButtonControl button && !button.isPressed &&
                    button.ReadValueFromEvent(eventPtr) >= button.pressPointOrDefault)
                {
                    activity = true;
                    break;
                }
            }

            var usesGamepad = device is Gamepad;
            if (activity && IsGamepad != usesGamepad)
            {
                IsGamepad = usesGamepad;
                _bindingLabels.Clear();
                DeviceChanged?.Invoke();
            }
        }

        private bool HasStickActivity(StickControl stick, InputEventPtr eventPtr)
        {
            if (!stick.ReadValueFromEvent(eventPtr, out var value))
            {
                return false;
            }

            if (!_stickActivity.TryGetValue(stick, out var previous))
            {
                previous = stick.ReadValue();
                _stickActivity[stick] = previous;
            }

            // Returning to rest rearms detection without stealing hints from the keyboard.
            if (value.sqrMagnitude < 0.25f * 0.25f)
            {
                _stickActivity[stick] = Vector2.zero;
                return false;
            }

            // Accumulate slow deliberate movement, ignoring jitter around a held position.
            if (value.sqrMagnitude < 0.5f * 0.5f || (value - previous).sqrMagnitude < 0.25f * 0.25f)
            {
                return false;
            }

            _stickActivity[stick] = value;
            return true;
        }

private void OnActionTriggered(InputAction.CallbackContext context)
        {
            if (!_actionCommands.TryGetValue(context.action, out var command))
            {
                return;
            }

            if (context.canceled)
            {
                _requiresRelease.Remove(command);

            }

            if (!context.performed)
            {
                return;
            }

            if (!GameplayEnabled)
            {
                return;
            }

            if (context.action.type == InputActionType.Button && !_requiresRelease.Contains(command))
            {
                _pendingPresses.Add(command);
            }

        }

        private sealed class PlayerBinding : IDisposable
        {
            private LocalInputService _owner;
            private readonly long _version;

            public PlayerBinding(LocalInputService owner, long version)
            {
                _owner = owner;
                _version = version;
            }

            public void Dispose()
            {
                if (_owner == null)
                {
                    return;
                }

                if (!_owner._disposed && _owner._bindingVersion == _version)
                {
                    _owner._player = null;
                    _owner._blocked = true;
                    _owner.ClearTransientInput();
                }

                _owner = null;
            }
        }
    }
}
