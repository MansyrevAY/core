using System;
using UnityEngine;

namespace Game.Demo
{
    public class ScreenModel
    {
        public string Title;
        public Action<Transform> Build;
        public Action Back;
    }
}
