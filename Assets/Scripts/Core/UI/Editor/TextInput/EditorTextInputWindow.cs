using System;
using Core.Input.TextInput;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.UIElements;

namespace Core.UI.Editor.TextInput
{
    public sealed class EditorTextInputWindow : EditorWindow, IDisposable
    {
        private TextInputRequest _request;
        private Action<TextInputResult> _completed;
        private TextField _draft;
        private bool _finished;
        private bool _listening;

        public void Begin(TextInputRequest request, Action<TextInputResult> completed)
        {
            _request = request;
            _completed = completed;
            titleContent = new GUIContent("Text input — " + request.Title);
            minSize = new Vector2(440f, 220f);
            var bounds = EditorGUIUtility.GetMainWindowPosition();
            position = new Rect(bounds.center.x - 260f, bounds.center.y - 125f, 520f, 250f);
            ShowUtility();
            Focus();
            InputSystem.onEvent += OnInputEvent;
            _listening = true;
        }

        public void CreateGUI()
        {
            rootVisualElement.Clear();
            var tree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
                "Assets/Scripts/Core/UI/Editor/TextInput/EditorTextInputWindow.uxml");
            tree.CloneTree(rootVisualElement);
            _draft = rootVisualElement.Q<TextField>("draft");
            _draft.label = _request.Title;
            _draft.multiline = _request.Multiline;
            _draft.isPasswordField = _request.Password;
            _draft.maxLength = _request.CharacterLimit > 0 ? _request.CharacterLimit : -1;
            _draft.SetValueWithoutNotify(_request.InitialText);
            rootVisualElement.Q<Button>("confirm").clicked += Submit;
            rootVisualElement.Q<Button>("cancel").clicked += Cancel;
            rootVisualElement.RegisterCallback<KeyDownEvent>(OnKeyDown, TrickleDown.TrickleDown);
            _draft.schedule.Execute(() => _draft.Focus());
        }

        public void Submit() => Finish(TextInputResult.Accept(_draft?.value ?? _request.InitialText));
        public void Cancel() => Finish(TextInputResult.Cancelled);

        public void Dispose()
        {
            if (_finished) return;
            _finished = true;
            _completed = null;
            StopListening();
            Close();
            FocusGameView();
        }

        private void OnKeyDown(KeyDownEvent eventData)
        {
            if (eventData.keyCode == KeyCode.Escape)
            {
                eventData.StopImmediatePropagation();
                Cancel();
            }
            else if ((eventData.keyCode == KeyCode.Return || eventData.keyCode == KeyCode.KeypadEnter) &&
                (!_request.Multiline || eventData.ctrlKey))
            {
                eventData.StopImmediatePropagation();
                Submit();
            }
        }

        private void OnInputEvent(InputEventPtr eventPtr, InputDevice device)
        {
            if (device is not Gamepad gamepad ||
                (eventPtr.type != StateEvent.Type && eventPtr.type != DeltaStateEvent.Type)) return;
            if (IsNewPress(gamepad.buttonEast, eventPtr)) Cancel();
            else if (IsNewPress(gamepad.buttonSouth, eventPtr)) Submit();
        }

        private static bool IsNewPress(ButtonControl button, InputEventPtr eventPtr)
        {
            return !button.isPressed && button.ReadValueFromEvent(eventPtr) >= button.pressPointOrDefault;
        }

        private void Finish(TextInputResult result)
        {
            if (_finished) return;
            _finished = true;
            var completed = _completed;
            _completed = null;
            StopListening();
            Close();
            completed?.Invoke(result);
            FocusGameView();
        }

        private void OnDisable()
        {
            StopListening();
            if (_finished) return;
            _finished = true;
            var completed = _completed;
            _completed = null;
            completed?.Invoke(TextInputResult.Cancelled);
            FocusGameView();
        }

        private void StopListening()
        {
            if (!_listening) return;
            InputSystem.onEvent -= OnInputEvent;
            _listening = false;
        }

        private static void FocusGameView()
        {
            if (!EditorApplication.isPlaying) return;
            var gameView = typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView");
            if (gameView != null) FocusWindowIfItsOpen(gameView);
        }
    }
}
