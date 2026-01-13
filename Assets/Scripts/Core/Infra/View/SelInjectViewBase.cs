using System;
using Core.Infra.Module;

namespace Core.Infra.View
{
    public abstract class SelInjectViewBase<T> : ViewBase<T> where T : class, IInternalModule
    {
        private void Awake()
        {
            if (!Initialized)
            {
                Init(Client.Client.Instance);
            }
        }

        private void OnEnable()
        {
            if (!Initialized)
            {
                Init(Client.Client.Instance);
            }
        }
    }
}