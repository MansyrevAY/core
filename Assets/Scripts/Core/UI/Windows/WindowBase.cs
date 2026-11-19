using Core.Input;
using System;
using R3;
using UnityEngine;

using Core.Infra.Lifetime;
namespace Core.UI.Windows
{
    public abstract class WindowBase<T> : WindowBase
    {
        [SerializeField] private SafeArea _safeArea;

        public sealed override Type ModelType => typeof(T);
        protected T ActiveModel { get; private set; }

        protected DisposableBag Disposables = new();
        private GameObject _lastSelected;

        public sealed override void Open(object model)
        {
            Disposables.Clear();

            ActiveModel = (T)model;

            if (Cursor.lockState == CursorLockMode.Locked)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }

            if (_safeArea)
            {
                _safeArea.Fit();
            }

            OnOpen();

            gameObject.SetActive(true);
            Core.UI.UiNavigation.SelectFirst(gameObject, _lastSelected);
        }

        public sealed override void Close()
        {
            var eventSystem = UnityEngine.EventSystems.EventSystem.current;
            var selected = eventSystem ? eventSystem.currentSelectedGameObject : null;
            if (selected && selected.transform.IsChildOf(transform))
            {
                _lastSelected = selected;
            }

            Disposables.Clear();

            ActiveModel = default;

            this.gameObject.SetActive(false);
        }

        protected abstract void OnOpen();
    }

    public abstract class WindowBase : MonoBehaviour
    {
        [SerializeField] private Canvas _canvas;

        public LocalInputService Input { get; internal set; }
        internal Action CloseRequested { get; set; }

        protected void CloseWindow()
        {
            CloseRequested?.Invoke();
        }

        public abstract Type ModelType { get; }

        public void SetOrder(int order)
        {
            if (!_canvas)
            {
                _canvas = GetComponent<Canvas>();
            }
            _canvas.overrideSorting = true;
            _canvas.sortingOrder = order;
        }

        public virtual void Cancel()
        {
        }

        public abstract void Open(object model);
        public abstract void Close();
    }
}
