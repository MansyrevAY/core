using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Core.UI
{
    public static class UiNavigation
    {
        public static void SelectFirst(GameObject root, GameObject previous = null)
        {
            var eventSystem = EventSystem.current;
            if (!eventSystem)
            {
                return;
            }

            if (previous)
            {
                var selectable = previous.GetComponent<UnityEngine.UI.Selectable>();
                if (CanSelect(selectable))
                {
                    eventSystem.SetSelectedGameObject(previous);
                    return;
                }
            }

            var controls = root.GetComponentsInChildren<UnityEngine.UI.Selectable>();
            foreach (var control in controls)
            {
                if (CanSelect(control))
                {
                    eventSystem.SetSelectedGameObject(control.gameObject);
                    return;
                }
            }

            eventSystem.SetSelectedGameObject(null);
        }

        public static void ConfigureLoop(IReadOnlyList<UnityEngine.UI.Selectable> candidates, bool horizontal)
        {
            var controls = new List<UnityEngine.UI.Selectable>();
            foreach (var candidate in candidates)
            {
                if (CanSelect(candidate))
                {
                    controls.Add(candidate);
                }
            }

            for (var index = 0; index < controls.Count; index++)
            {
                var previous = controls[(index + controls.Count - 1) % controls.Count];
                var next = controls[(index + 1) % controls.Count];
                controls[index].navigation = new UnityEngine.UI.Navigation
                {
                    mode = UnityEngine.UI.Navigation.Mode.Explicit,
                    selectOnUp = previous,
                    selectOnDown = next,
                    selectOnLeft = horizontal ? previous : null,
                    selectOnRight = horizontal ? next : null
                };
            }

            var eventSystem = EventSystem.current;
            if (!eventSystem)
            {
                return;
            }

            var selected = eventSystem.currentSelectedGameObject;
            foreach (var control in controls)
            {
                if (control.gameObject == selected)
                {
                    return;
                }
            }

            eventSystem.SetSelectedGameObject(controls.Count > 0 ? controls[0].gameObject : null);
        }

        private static bool CanSelect(UnityEngine.UI.Selectable selectable)
        {
            return selectable && selectable.IsActive() && selectable.IsInteractable();
        }
    }
}
