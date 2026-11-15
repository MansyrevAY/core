using System;

namespace Core.Infra.Module
{
    public interface IModule : IDisposable
    {
        // Side effects only (subscribe, create entities, find scene objects). Dependencies arrive via the constructor.
        // Dispose undoes whatever Start did; it is called by the owning ModuleScope in reverse start order.
        void Start();
    }
}
