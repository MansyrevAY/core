using Core.UI;
using Core.UI.Windows;
using UnityEngine;

namespace Game.Demo
{
    public abstract class DemoPanel<T> : WindowBase<T> where T : ScreenModel
    {
        private RectTransform _content;

        protected override void OnOpen()
        {
            if (_content)
            {
                _content.gameObject.SetActive(false);
                Destroy(_content.gameObject);
            }
            _content = DemoUi.Panel(transform, "Content", new Vector2(620, 640));
            DemoUi.Label(_content, ActiveModel.Title, 48).fontSize = 28;
            ActiveModel.Build?.Invoke(_content);
            foreach (var field in _content.GetComponentsInChildren<GamepadInputField>(true))
            {
                var service = GetComponentInParent<WindowsService>();
                field.Configure(service.TextInput, Input);
            }
        }

        public override void Cancel()
        {
            ActiveModel?.Back?.Invoke();
        }
    }
}
