using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

namespace Core.Input
{
    public static class KeyboardShortcuts
    {
        private static readonly Dictionary<KeyCode, Key> Keys = new();

        public static bool IsHeld(KeyCode key)
        {
            var control = GetControl(key);
            return control != null && control.isPressed;
        }

        public static bool WasPressed(KeyCode key)
        {
            var control = GetControl(key);
            return control != null && control.wasPressedThisFrame;
        }

        private static KeyControl GetControl(KeyCode key)
        {
            var keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return null;
            }

            if (!Keys.TryGetValue(key, out var inputKey))
            {
                // Trailer assets retain their serialized KeyCode IDs while using the new backend.
                var name = key.ToString().Replace("Alpha", "Digit").Replace("Keypad", "Numpad").Replace("Control", "Ctrl");
                if (key == KeyCode.Return)
                {
                    name = "Enter";
                }

                Enum.TryParse(name, out inputKey);
                Keys[key] = inputKey;
            }

            return inputKey == Key.None ? null : keyboard[inputKey];
        }
    }
}
