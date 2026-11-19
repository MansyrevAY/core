using UnityEngine;
using UnityEngine.InputSystem;

namespace Core.Input
{
    public static class InputActionsFactory
    {
        public static InputActionAsset Create()
        {
            var asset = ScriptableObject.CreateInstance<InputActionAsset>();
            var gameplay = asset.AddActionMap("Gameplay");
            var move = gameplay.AddAction("Move", InputActionType.Value, expectedControlLayout: "Vector2");
            move.AddCompositeBinding("2DVector").With("Up", "<Keyboard>/w").With("Down", "<Keyboard>/s")
                .With("Left", "<Keyboard>/a").With("Right", "<Keyboard>/d");
            move.AddBinding("<Gamepad>/leftStick", groups: "Gamepad");
            var look = gameplay.AddAction("Look", InputActionType.Value, expectedControlLayout: "Vector2");
            look.AddBinding("<Mouse>/delta", groups: "Keyboard&Mouse");
            look.AddBinding("<Gamepad>/rightStick", groups: "Gamepad");
            AddButton(gameplay, "Jump", "<Keyboard>/space", "<Gamepad>/buttonSouth");
            AddButton(gameplay, "Sprint", "<Keyboard>/leftShift", "<Gamepad>/leftStickPress");
            AddButton(gameplay, "Crouch", "<Keyboard>/leftCtrl", "<Gamepad>/buttonEast");
            var common = asset.AddActionMap("Common");
            AddButton(common, "Pause", "<Keyboard>/escape", "<Gamepad>/start");
            var ui = asset.AddActionMap("UI");
            ui.AddAction("Point", InputActionType.PassThrough, "<Mouse>/position", expectedControlLayout: "Vector2");
            var click = ui.AddAction("Click", InputActionType.PassThrough, expectedControlLayout: "Button");
            click.AddBinding("<Mouse>/leftButton");
            click.AddBinding("<Touchscreen>/primaryTouch/press");
            var navigate = ui.AddAction("Navigate", InputActionType.PassThrough, expectedControlLayout: "Vector2");
            navigate.AddBinding("<Gamepad>/leftStick");
            navigate.AddBinding("<Gamepad>/dpad");
            navigate.AddCompositeBinding("2DVector").With("Up", "<Keyboard>/upArrow").With("Down", "<Keyboard>/downArrow")
                .With("Left", "<Keyboard>/leftArrow").With("Right", "<Keyboard>/rightArrow");
            AddButton(ui, "Submit", "<Keyboard>/enter", "<Gamepad>/buttonSouth");
            AddButton(ui, "Cancel", "<Keyboard>/escape", "<Gamepad>/buttonEast");
            return asset;
        }

        private static void AddButton(InputActionMap map, string name, string keyboard, string gamepad)
        {
            var action = map.AddAction(name, InputActionType.Button);
            action.AddBinding(keyboard, groups: "Keyboard&Mouse");
            action.AddBinding(gamepad, groups: "Gamepad");
        }
    }
}
