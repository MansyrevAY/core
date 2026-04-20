using Core.Infra.Broadcaster;
using Core.Infra.Module;
using Core.Services.Asset;
using UnityEngine;

namespace Core.Infra.View
{
    public abstract class ViewBase<T> : MonoBehaviour where T: class, IInternalModule
    {
        protected IInternalModuleProvider Provider { get; private set; }
        protected IBroadcaster Broadcaster { get; private set; }
        protected T Module { get; private set; }
        
        protected bool Initialized = false;
        
        private IAssetService _assetService;

        public void Init(IInternalModuleProvider provider, IAssetService assetService)
        {
            if (Initialized)
            {
                Log.Log.Warning($"{GetType().Name} on {gameObject.name} is already initialized, aborting");
                return;
            }
            Initialized = true;
            
            Provider = provider;
            Module = Provider.GetModule<T>();
            Broadcaster = Module.Broadcaster;
            _assetService = assetService;
            
            ManagedAwake();
            ManagedStart();
        }

        protected virtual void ManagedAwake()
        {
            
        }

        protected virtual void ManagedStart()
        {
            
        }

        private void Update()
        {
            if (!Initialized)
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