using System;
using Core.Infra.Logging;
using Core.Infra.Messaging;
using Core.Infra.Module;
using Core.Services.Asset;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Core.Infra.View
{
    public abstract class ViewBase<T> : MonoBehaviour, IDisposable where T: class, IInternalModule
    {
        protected IBroadcaster Broadcaster { get; private set; }
        protected T Module { get; private set; }

        protected bool Initialized = false;

        private IAssetService _assetService;
        private bool _disposed;

        public void Init(T module, IAssetService assetService)
        {
            if (Initialized)
            {
                Log.Warning($"{GetType().Name} on {gameObject.name} is already initialized, aborting");
                return;
            }
            Initialized = true;

            Module = module;
            Broadcaster = Module.Broadcaster;
            _assetService = assetService;
            
            ManagedAwake();
            ManagedStart();
        }

        protected void InitChild(ViewBase<T> view)
        {
            view.Init(Module, _assetService);
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }
            _disposed = true;

            if (Initialized)
            {
                ManagedDispose();
            }
        }

        // Note: a subclass that declares its own OnDestroy hides this one. Override ManagedDispose instead.
        private void OnDestroy()
        {
            Dispose();
        }

        protected virtual void ManagedAwake()
        {
            
        }

        protected virtual void ManagedStart()
        {
            
        }

        protected virtual void ManagedDispose()
        {
        }

        private void Update()
        {
            if (!Initialized || _disposed)
            {
                return;
            }
            
            ManagedUpdate();
        }

        protected virtual void ManagedUpdate()
        {
        }

        public void Destroy<TG>(IInstance<TG> instance) where TG : Object
        {
            _assetService.Destroy(instance);
        }

        public GameObject Instantiate<TG>(IInstance<TG> instance) where TG : Object
        {
            return _assetService.Instantiate(instance);
        }
    }
}
