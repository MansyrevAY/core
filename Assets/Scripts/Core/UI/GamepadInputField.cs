using System;
using Core.Input;
using Core.Input.TextInput;
using Core.UI.Windows;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Core.UI
{
    public sealed class GamepadInputField : TMP_InputField
    {
        [SerializeField] private string _textInputTitle = "Text";
        private TextInputService _service;
        private LocalInputService _input;
        private IDisposable _session;
        private CanvasGroup _group;
        private bool _wasInteractable;
        private GameObject _shield;
        private bool _editing;
        private bool _restoringFocus;
        private int _restoredFrame = -1;

        public void Configure(TextInputService service, LocalInputService input)
        {
            _service = service;
            _input = input;
        }

        public override void OnSelect(BaseEventData eventData)
        {
            var activate = shouldActivateOnSelect;
            if (_restoringFocus || _input?.IsGamepad == true) shouldActivateOnSelect = false;
            base.OnSelect(eventData);
            shouldActivateOnSelect = activate;
        }

        public override void OnSubmit(BaseEventData eventData)
        {
            if (Time.frameCount == _restoredFrame || _editing) return;
            if (_input?.IsGamepad == true && _service != null && IsActive() && IsInteractable() && !readOnly)
            {
                if (_service.IsOpen) return;
                var request = new TextInputRequest(_textInputTitle, text, characterLimit,
                    lineType != LineType.SingleLine, inputType == InputType.Password);
                SuspendInteraction();
                IDisposable session;
                try
                {
                    session = _service.Open(request, OnCompleted);
                }
                catch
                {
                    RestoreInteraction();
                    throw;
                }

                if (!_editing)
                {
                    session?.Dispose();
                    eventData?.Use();
                    return;
                }

                if (session != null)
                {
                    _session = session;
                    eventData?.Use();
                    return;
                }

                RestoreInteraction();
            }

            base.OnSubmit(eventData);
        }

        public override void OnCancel(BaseEventData eventData)
        {
            if (_input?.IsGamepad == true && !isFocused)
            {
                eventData?.Use();
                return;
            }

            base.OnCancel(eventData);
        }

        protected override void OnDisable()
        {
            _session?.Dispose();
            _session = null;
            RestoreInteraction();
            base.OnDisable();
        }

        private void SuspendInteraction()
        {
            _editing = true;
            var window = GetComponentInParent<WindowBase>();
            var root = window ? window.gameObject : gameObject;
            _group = root.GetComponent<CanvasGroup>();
            if (!_group) _group = root.AddComponent<CanvasGroup>();
            _wasInteractable = _group.interactable;
            _group.interactable = false;
            if (window)
            {
                _shield = new GameObject("TextInputShield", typeof(RectTransform), typeof(UnityEngine.UI.Image));
                var rect = (RectTransform)_shield.transform;
                rect.SetParent(root.transform, false);
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = rect.offsetMax = Vector2.zero;
                _shield.GetComponent<UnityEngine.UI.Image>().color = Color.clear;
            }

            if (EventSystem.current) EventSystem.current.SetSelectedGameObject(null);
            DeactivateInputField();
        }

        private void OnCompleted(TextInputResult result)
        {
            _session = null;
            RestoreInteraction();
            if (!IsActive()) return;
            if (result.Submitted)
            {
                text = result.Text;
                onEndEdit.Invoke(text);
            }

            if (IsActive() && IsInteractable() && EventSystem.current)
            {
                _restoredFrame = Time.frameCount;
                _restoringFocus = true;
                EventSystem.current.SetSelectedGameObject(gameObject);
                _restoringFocus = false;
            }
        }

        private void RestoreInteraction()
        {
            if (!_editing) return;
            _editing = false;
            if (_group) _group.interactable = _wasInteractable;
            _group = null;
            if (_shield)
            {
                _shield.SetActive(false);
                Destroy(_shield);
                _shield = null;
            }
        }
    }
}
