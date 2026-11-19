using System;
using Core.Input;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Core.Tests.Input
{
    public sealed class LocalInputServiceTests : InputTestFixture
    {
        private LocalInputService _input;
        private object _player;
        private IDisposable _binding;
        private Keyboard _keyboard;
        private Gamepad _gamepad;
        private Mouse _mouse;

        public override void Setup()
        {
            base.Setup();
            _keyboard = InputSystem.AddDevice<Keyboard>();
            _gamepad = InputSystem.AddDevice<Gamepad>();
            _mouse = InputSystem.AddDevice<Mouse>();
            var actions = InputActionsFactory.Create();
            _input = new LocalInputService(actions);
            UnityEngine.Object.DestroyImmediate(actions);
            _player = new object();
            _binding = _input.BindPlayer(_player);
            _input.SetGameplayBlocked(_player, false);
        }

        public override void TearDown()
        {
            _binding?.Dispose();
            _input?.Dispose();
            base.TearDown();
        }

        [Test]
        public void KeyboardAndGamepadDriveTheSameMovementAction()
        {
            Press(_keyboard.wKey);
            Assert.That(_input.Move.y, Is.EqualTo(1f).Within(0.001f));
            Release(_keyboard.wKey);
            Set(_gamepad.leftStick, Vector2.right);
            Assert.That(_input.Move.x, Is.EqualTo(1f).Within(0.001f));
            Assert.That(_input.IsGamepad, Is.True);
        }

        [Test]
        public void ShortJumpSurvivesReleaseAndIsCollectedOnce()
        {
            Press(_keyboard.spaceKey);
            Release(_keyboard.spaceKey);
            InputSystem.Update();
            Assert.That(_input.CollectJump(), Is.True);
            Assert.That(_input.CollectJump(), Is.False);
        }

        [Test]
        public void MouseDeltaResetDoesNotReplaceGamepadHints()
        {
            Set(_mouse.delta, new Vector2(10f, 5f));
            Press(_gamepad.buttonSouth);
            InputSystem.Update();
            Assert.That(_input.IsGamepad, Is.True);
        }

        [TestCase(true)]
        [TestCase(false)]
        public void SmallStickDriftDoesNotReplaceKeyboardHints(bool leftStick)
        {
            Press(_keyboard.wKey);
            Set(leftStick ? _gamepad.leftStick : _gamepad.rightStick, new Vector2(0.2f, 0f));
            Assert.That(_input.IsGamepad, Is.False);
        }

        [TestCase(true)]
        [TestCase(false)]
        public void HeldStickNoiseDoesNotReplaceKeyboardHintsButNewMovementDoes(bool leftStick)
        {
            var stick = leftStick ? _gamepad.leftStick : _gamepad.rightStick;
            Set(stick, new Vector2(0.8f, 0f));
            Press(_keyboard.spaceKey);
            Set(stick, new Vector2(0.79f, 0f));
            Set(stick, new Vector2(0.81f, 0f));
            Assert.That(_input.IsGamepad, Is.False);
            Set(stick, new Vector2(-0.8f, 0f));
            Assert.That(_input.IsGamepad, Is.True);
        }

        [Test]
        public void ReturningStickToRestDoesNotReplaceKeyboardHints()
        {
            Set(_gamepad.leftStick, Vector2.right);
            Press(_keyboard.spaceKey);
            Set(_gamepad.leftStick, Vector2.zero);
            Assert.That(_input.IsGamepad, Is.False);
            Set(_gamepad.leftStick, Vector2.right);
            Assert.That(_input.IsGamepad, Is.True);
        }

        [Test]
        public void SlowStickMovementEventuallySwitchesToGamepadHints()
        {
            Press(_keyboard.spaceKey);
            for (var i = 1; i <= 8; i++)
            {
                Set(_gamepad.leftStick, new Vector2(i * 0.1f, 0f));
            }

            Assert.That(_input.IsGamepad, Is.True);
        }

        [Test]
        public void PointerMovementAndScrollSwitchToKeyboardHints()
        {
            Set(_gamepad.dpad, Vector2.up);
            Assert.That(_input.IsGamepad, Is.True);
            Set(_mouse.position, new Vector2(100f, 50f));
            Assert.That(_input.IsGamepad, Is.False);
            Press(_gamepad.buttonSouth);
            Assert.That(_input.IsGamepad, Is.True);
            Set(_mouse.scroll, Vector2.up);
            Assert.That(_input.IsGamepad, Is.False);
        }

        [Test]
        public void HeldTriggerNoiseAndReleaseDoNotReplaceKeyboardHints()
        {
            Press(_gamepad.rightTrigger);
            Press(_keyboard.spaceKey);
            Set(_gamepad.rightTrigger, 0.98f);
            Assert.That(_input.IsGamepad, Is.False);
            Release(_gamepad.rightTrigger);
            Assert.That(_input.IsGamepad, Is.False);
            Press(_gamepad.rightTrigger);
            Assert.That(_input.IsGamepad, Is.True);
        }

        [Test]
        public void MouseButtonReleaseDoesNotReplaceGamepadHints()
        {
            Press(_mouse.leftButton);
            Press(_gamepad.buttonSouth);
            Release(_mouse.leftButton);
            Assert.That(_input.IsGamepad, Is.True);
        }

        [Test]
        public void InitialHeldGamepadStateDoesNotReplaceKeyboardHints()
        {
            _binding.Dispose();
            _input.Dispose();
            Set(_gamepad.rightStick, Vector2.right);
            _input = new LocalInputService(
                InputActionsFactory.Create());
            InputSystem.Update();
            Assert.That(_input.IsGamepad, Is.False);
        }

        [Test]
        public void DeliberateInputChangesHintsAndLabelsOnlyWhenDeviceFamilyChanges()
        {
            var keyboardLabel = _input.GetBindingLabel(GameInputAction.Jump);
            var changes = 0;
            _input.DeviceChanged += () => changes++;
            Press(_gamepad.buttonSouth);
            var gamepadLabel = _input.GetBindingLabel(GameInputAction.Jump);
            Press(_gamepad.buttonWest);
            Assert.That(gamepadLabel, Is.Not.EqualTo(keyboardLabel));
            Assert.That(changes, Is.EqualTo(1));
            Set(_mouse.delta, new Vector2(10f, 0f));
            Assert.That(_input.IsGamepad, Is.False);
            Assert.That(_input.GetBindingLabel(GameInputAction.Jump), Is.EqualTo(keyboardLabel));
            Assert.That(changes, Is.EqualTo(2));
        }

        [Test]
        public void HeldJumpRemainsHeldAfterBufferedPressIsCollected()
        {
            Press(_gamepad.buttonSouth);
            Assert.That(_input.CollectJump(), Is.True);
            Assert.That(_input.CollectJump(), Is.True);
            Release(_gamepad.buttonSouth);
            Assert.That(_input.CollectJump(), Is.False);
        }

        [Test]
        public void PausingDiscardsBufferedJumpAndBlocksGameplay()
        {
            Press(_keyboard.spaceKey);
            Release(_keyboard.spaceKey);
            Press(_keyboard.wKey);
            _input.SetGameplayBlocked(_player, true);
            Assert.That(_input.Move, Is.EqualTo(Vector2.zero));
            Assert.That(_input.CollectJump(), Is.False);
            _input.SetGameplayBlocked(_player, false);
            Assert.That(_input.CollectJump(), Is.False);
        }

        [Test]
        public void SubmitHeldWhileClosingMenuRequiresReleaseBeforeJump()
        {
            _input.SetGameplayBlocked(_player, true);
            Press(_gamepad.buttonSouth);
            _input.SetGameplayBlocked(_player, false);
            Assert.That(_input.CollectJump(), Is.False);
            Release(_gamepad.buttonSouth);
            Press(_gamepad.buttonSouth);
            Assert.That(_input.CollectJump(), Is.True);
        }



        [Test]
        public void DisposingOldBindingCannotRemoveReplacement()
        {
            var replacement = new object();
            using var replacementBinding = _input.BindPlayer(replacement);
            _input.SetGameplayBlocked(replacement, false);
            _binding.Dispose();
            _input.SetGameplayBlocked(_player, true);
            Assert.That(_input.GameplayEnabled, Is.True);
        }

        [Test]
        public void ReplacingPlayerDiscardsCommandsFromPreviousAvatar()
        {
            Press(_keyboard.spaceKey);
            Release(_keyboard.spaceKey);
            var replacement = new object();
            using var replacementBinding = _input.BindPlayer(replacement);
            _input.SetGameplayBlocked(replacement, false);
            Assert.That(_input.CollectJump(), Is.False);
            replacementBinding.Dispose();
            Assert.That(_input.GameplayEnabled, Is.False);
        }

        [Test]
        public void MouseLookUsesDisplacementAndGamepadLookUsesElapsedTime()
        {
            Set(_mouse.delta, new Vector2(10f, 5f));
            var shortFrame = _input.GetLookDelta(2f, 180f, 0.01f);
            var longFrame = _input.GetLookDelta(2f, 180f, 0.1f);
            Assert.That(shortFrame, Is.EqualTo(new Vector2(2f, 1f)));
            Assert.That(longFrame, Is.EqualTo(shortFrame));
            Set(_gamepad.rightStick, Vector2.right);
            shortFrame = _input.GetLookDelta(2f, 180f, 0.01f);
            longFrame = _input.GetLookDelta(2f, 180f, 0.1f);
            Assert.That(shortFrame.x, Is.EqualTo(1.8f).Within(0.001f));
            Assert.That(longFrame.x, Is.EqualTo(18f).Within(0.001f));
        }







        [Test]
        public void MenuNavigationRemainsAvailableWithoutPlayerBinding()
        {
            _binding.Dispose();
            Set(_gamepad.dpad, Vector2.up);
            var navigation = _input.GetAction(GameInputAction.Navigate).ReadValue<Vector2>();
            Assert.That(navigation.y, Is.EqualTo(1f).Within(0.001f));
            Assert.That(_input.GameplayEnabled, Is.False);
        }

        [Test]
        public void CancelHeldWhileClosingMenuDoesNotCrouchUntilReleased()
        {
            _input.SetGameplayBlocked(_player, true);
            Press(_gamepad.buttonEast);
            _input.SetGameplayBlocked(_player, false);
            Assert.That(_input.IsHeld(GameInputAction.Crouch), Is.False);
            Release(_gamepad.buttonEast);
            Press(_gamepad.buttonEast);
            Assert.That(_input.IsHeld(GameInputAction.Crouch), Is.True);
        }



        [Test]
        public void RemovingGamepadDiscardsPendingCommandsAndRestoresKeyboardHints()
        {
            Press(_gamepad.buttonSouth);
            Release(_gamepad.buttonSouth);
            InputSystem.RemoveDevice(_gamepad);
            Assert.That(_input.CollectJump(), Is.False);
            Assert.That(_input.IsGamepad, Is.False);
        }



        [TestCase(KeyCode.Alpha0, Key.Digit0)]
        [TestCase(KeyCode.Alpha1, Key.Digit1)]
        [TestCase(KeyCode.F8, Key.F8)]
        [TestCase(KeyCode.LeftShift, Key.LeftShift)]
        [TestCase(KeyCode.Return, Key.Enter)]
        public void TrailerShortcutsUseExistingSerializedKeyAssignments(KeyCode legacyKey, Key key)
        {
            Press(_keyboard[key]);
            Assert.That(KeyboardShortcuts.IsHeld(legacyKey), Is.True);
            Release(_keyboard[key]);
            Assert.That(KeyboardShortcuts.IsHeld(legacyKey), Is.False);
        }

        [Test]
        public void DisposedInputRejectsNewBindingsAndOldLeaseCanBeReleased()
        {
            _input.Dispose();
            Assert.Throws<ObjectDisposedException>(() => _input.BindPlayer(_player));
            Assert.DoesNotThrow(() => _binding.Dispose());
        }
    }
}
