using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Core.Tests.UI.Editor
{
    public sealed class UiNavigationTests
    {
        private readonly List<GameObject> _objects = new();
        private EventSystem _previousEventSystem;
        private EventSystem _eventSystem;
        private bool _eventSystemEnabledManually;
        private Type _navigationType;

        [SetUp]
        public void SetUp()
        {
            _previousEventSystem = EventSystem.current;
            _eventSystem = CreateObject("EventSystem").AddComponent<EventSystem>();
            // EventSystem is not ExecuteAlways; register its lifecycle explicitly in EditMode.
            _eventSystemEnabledManually = EventSystem.current != _eventSystem;
            if (_eventSystemEnabledManually)
            {
                typeof(EventSystem).GetMethod("OnEnable", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(_eventSystem, null);
            }
            _navigationType = RuntimeType("Core.UI.UiNavigation");
        }

        [TearDown]
        public void TearDown()
        {
            if (_eventSystem)
            {
                _eventSystem.SetSelectedGameObject(null);
            }

            if (_eventSystemEnabledManually && _eventSystem)
            {
                typeof(EventSystem).GetMethod("OnDisable", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(_eventSystem, null);
            }

            if (_previousEventSystem)
            {
                EventSystem.current = _previousEventSystem;
            }
            for (var index = _objects.Count - 1; index >= 0; index--)
            {
                UnityEngine.Object.DestroyImmediate(_objects[index]);
            }

            _objects.Clear();
        }

        [Test]
        public void VerticalTraversalVisitsSlidersAndAllLeftSettingsButtons()
        {
            var names = new[] { "sound_slider", "music_slider", "mouse_slider", "video_reset", "back_button", "accept_button" };
            var controls = new List<UnityEngine.UI.Selectable>();
            foreach (var name in names)
            {
                controls.Add(CreateButton(name));
            }

            Configure(controls, false);
            foreach (var name in names)
            {
                Assert.That(_eventSystem.currentSelectedGameObject.name, Is.EqualTo(name));
                Move(MoveDirection.Down);
            }

            Assert.That(_eventSystem.currentSelectedGameObject.name, Is.EqualTo(names[0]));
            Move(MoveDirection.Up);
            Assert.That(_eventSystem.currentSelectedGameObject.name, Is.EqualTo(names[names.Length - 1]));
        }



        [Test]
        public void DisabledInactiveAndMissingControlsAreSkipped()
        {
            var disabled = CreateButton("disabled");
            disabled.interactable = false;
            var inactive = CreateButton("inactive");
            inactive.gameObject.SetActive(false);
            var first = CreateButton("first");
            var second = CreateButton("second");
            Configure(new[] { null, disabled, inactive, first, second }, true);
            Assert.That(_eventSystem.currentSelectedGameObject, Is.SameAs(first.gameObject));
            Move(MoveDirection.Right);
            Assert.That(_eventSystem.currentSelectedGameObject, Is.SameAs(second.gameObject));
            Move(MoveDirection.Right);
            Assert.That(_eventSystem.currentSelectedGameObject, Is.SameAs(first.gameObject));
        }

        [Test]
        public void RefreshingAvailableControlsPreservesCurrentSelection()
        {
            var first = CreateButton("first");
            var current = CreateButton("current");
            var unavailable = CreateButton("unavailable");
            unavailable.interactable = false;
            var controls = new[] { first, current, unavailable };
            Configure(controls, true);
            _eventSystem.SetSelectedGameObject(current.gameObject);
            unavailable.interactable = true;
            Configure(controls, true);
            Assert.That(_eventSystem.currentSelectedGameObject, Is.SameAs(current.gameObject));
        }

        [Test]
        public void ChangingNavigationGroupSelectsItsFirstControl()
        {
            var board = CreateButton("own_photo");
            var accept = CreateButton("accept_character");
            Configure(new[] { board }, true);
            Configure(new[] { accept }, true);
            Assert.That(_eventSystem.currentSelectedGameObject, Is.SameAs(accept.gameObject));
        }









        private UnityEngine.UI.Selectable CreateButton(string name)
        {
            return CreateObject(name, typeof(RectTransform)).AddComponent<UnityEngine.UI.Button>();
        }

        private GameObject CreateObject(string name, params Type[] components)
        {
            var gameObject = new GameObject(name, components);
            _objects.Add(gameObject);
            return gameObject;
        }

        private void Configure(IReadOnlyList<UnityEngine.UI.Selectable> controls, bool horizontal)
        {
            _navigationType.GetMethod("ConfigureLoop").Invoke(null, new object[] { controls, horizontal });
        }

        private void Move(MoveDirection direction)
        {
            var data = new AxisEventData(_eventSystem) { moveDir = direction };
            ExecuteEvents.Execute(_eventSystem.currentSelectedGameObject, data, ExecuteEvents.moveHandler);
        }

        private static Type RuntimeType(string name)
        {
            return Type.GetType(name + ", Core.UI", true);
        }

        private static void SetField(object target, string name, object value)
        {
            target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);
        }
    }
}
