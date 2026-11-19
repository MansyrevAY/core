using Core.Infra.Lifetime;
using Core.Input;
using Core.Input.TextInput;
using Core.UI;
using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using R3;
using UnityEngine;

namespace Core.UI.Windows
{
    public class WindowsService : DisposableBehaviour<WindowsService.Model>
    {
        public LocalInputService Input { get; private set; }
        public TextInputService TextInput { get; } = new();
        private bool _textInputConfigured;

        public void Configure(Transform anchor, List<WindowBase> prefabs)
        {
            _anchor = anchor;
            _prefabs = prefabs;
        }

        public void ConfigureInput(LocalInputService input)
        {
            Input = input;
            if (!_textInputConfigured)
            {
                _textInputConfigured = true;
                TextInput.Register(new UnityTouchKeyboardProvider(), -100);
                TextInput.SessionChanged += OnTextInputChanged;
                TextInput.ProviderFailed += Debug.LogException;
            }
            var modules = FindObjectsByType<UnityEngine.InputSystem.UI.InputSystemUIInputModule>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var module in modules)
            {
                module.actionsAsset = input.Actions;
            }
        }

        public class Model
        {
        }

        [SerializeField] private List<WindowBase> _prefabs;
        [SerializeField] private Transform _anchor;

        private const int EnvironmentOrder = 1;
        private readonly List<WindowBase> _instances = new();
        private readonly Stack<WindowBase> _windowsStack = new();

        public Observable<Unit> ObserveWindowOpen<T>() =>
            CurrentWindow.Where(window => window != null && window.GetType() == typeof(T))
                .Select(_ => _)
                .Take(1)
                .AsUnitObservable();

        public Observable<Unit> ObserveWindowClose<T>() =>
            CurrentWindow.Pairwise()
                .Where(pair => pair.Previous != null && pair.Previous.GetType() == typeof(T))
                .Select(_ => _)
                .Take(1)
                .AsUnitObservable();

        public Observable<Unit> ObserveWindowFullClose<T>()
        {
            return _windowClosedObservable.Where(closingWindowType => closingWindowType == typeof(T))
                .Take(1)
                .AsUnitObservable();
        }

        public ReadOnlyReactiveProperty<WindowBase> CurrentWindow => _currentWindow;

        private readonly ReactiveCommand<Type> _windowClosedObservable = new();
        private readonly ReactiveProperty<WindowBase> _currentWindow = new();

        private DisposableBag _windowDisposable = new();

        public static void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        public void Open<T>(T model, bool isDisplacing = true)
        {
            if (isDisplacing)
            {
                while (_windowsStack.Count > 0)
                {
                    var closingWindow = _windowsStack.Pop();

                    closingWindow.Close();

                    _windowClosedObservable.Execute(closingWindow.GetType());
                }
            }

            var currentWindow = GetOrCreateWindow(typeof(T));

            currentWindow.SetOrder(_windowsStack.Count + EnvironmentOrder);

            currentWindow.Open(model);

            _windowsStack.Push(currentWindow);

            OnWindowsQueueChanged();
        }

        public void Open(object model, bool isDisplacing = true)
        {
            if (isDisplacing)
            {
                while (_windowsStack.Count > 0)
                {
                    var closingWindow = _windowsStack.Pop();

                    closingWindow.Close();

                    _windowClosedObservable.Execute(closingWindow.GetType());
                }
            }

            var currentWindow = GetOrCreateWindow(model.GetType());

            currentWindow.SetOrder(_windowsStack.Count + EnvironmentOrder);

            currentWindow.Open(model);

            _windowsStack.Push(currentWindow);

            OnWindowsQueueChanged();
        }

        public void Close()
        {
            if (_windowsStack.Count > 0)
            {
                var currentWindow = _windowsStack.Pop();

                currentWindow.Close();

                _windowClosedObservable.Execute(currentWindow.GetType());

                OnWindowsQueueChanged();
            }
        }

        public void CloseAll()
        {
            while (_windowsStack.Count > 0)
                Close();
        }

        protected override UniTask OnInit()
        {
            Disposable.Create(OnDisposed).AddTo(ref Disposables);
            Observable.EveryUpdate().SafeSubscribe(_ => HandleCancel()).AddTo(ref Disposables);
            return UniTask.CompletedTask;
        }

        private WindowBase GetOrCreateWindow(Type modelType)
        {
            var instance = _instances.Find(instanceArg => instanceArg.ModelType == modelType);

            if (instance == null)
            {
                instance = CreateWindowInstance(modelType);
            }

            return instance;
        }

        private WindowBase CreateWindowInstance(Type modelType)
        {
            var prefab = _prefabs.Find(windowInstanceArg => windowInstanceArg.ModelType == modelType);

            var instance = GameObject.Instantiate(prefab, _anchor);

            instance.Input = Input;
            instance.CloseRequested = Close;
            foreach (var field in instance.GetComponentsInChildren<GamepadInputField>(true))
            {
                field.Configure(TextInput, Input);
            }
            _instances.Add(instance);

            return instance;
        }

        private void OnDisposed()
        {
            TextInput.SessionChanged -= OnTextInputChanged;
            TextInput.ProviderFailed -= Debug.LogException;
            TextInput.Dispose();
            _windowDisposable.Clear();

            while (_windowsStack.Count > 0)
            {
                Close();
            }

            foreach (var instance in _instances)
            {
                GameObject.Destroy(instance.gameObject);
            }

            _instances.Clear();
            _currentWindow.Dispose();
            _windowClosedObservable.Dispose();
        }

        private void HandleCancel()
        {
            if (Input != null && Input.WasPressed(GameInputAction.Cancel))
            {
                if (TextInput.IsOpen)
                {
                    TextInput.Cancel();
                    return;
                }

                _currentWindow.CurrentValue?.Cancel();
            }
        }

        private void OnTextInputChanged()
        {
            Input?.ClearTransientInput();
        }

        private void OnWindowsQueueChanged()
        {
            Input?.ClearTransientInput();
            _currentWindow.Value = _windowsStack.Count > 0 ? _windowsStack.Peek() : null;
        }
    }
}
