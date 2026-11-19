using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace Core.UI
{
    public static class PointerEnterRefresher
    {
        private static readonly List<RaycastResult> RaycastResults = new();

        public static void ForceEnterUnderCursor()
        {
            EventSystem eventSystem = EventSystem.current;

            if (eventSystem == null)
                return;

            var mouse = Mouse.current;
            if (mouse == null)
            {
                return;
            }

            Vector2 pointerPosition = mouse.position.ReadValue();

            PointerEventData pointerData = new(eventSystem) { position = pointerPosition };

            RaycastResults.Clear();
            eventSystem.RaycastAll(pointerData, RaycastResults);

            if (RaycastResults.Count == 0)
                return;

            GameObject topObject = RaycastResults[0].gameObject;

            GameObject enterHandler = ExecuteEvents.GetEventHandler<IPointerEnterHandler>(topObject);

            if (enterHandler == null)
                return;

            pointerData.pointerEnter = enterHandler;
            ExecuteEvents.Execute(enterHandler, pointerData, ExecuteEvents.pointerEnterHandler);
        }
    }
}
