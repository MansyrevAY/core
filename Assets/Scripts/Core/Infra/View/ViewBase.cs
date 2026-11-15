using Core.Infra.Broadcaster;
using Core.Infra.Module;
using Core.Services.Asset;
using UnityEngine;

namespace Core.Infra.View
{
    public abstract class ViewBase<T> : MonoBehaviour where T: class, IInternalModule
    {
        protected IBroadcaster Broadcaster { get; private set; }
        protected T Module { get; private set; }

        protected bool Initialized = false;

        private IAssetService _assetService;

        // Views are MonoBehaviours, so they can't take constructor arguments. The owning module passes
        // everything explicitly: the module itself here, anything else through a typed Inject method
        // on the concrete view, called before Init.
        public void Init(T module, IAssetService assetService)
        {
            if (Initialized)
            {
                Log.Log.Warning($"{GetType().Name} on {gameObject.name} is already initialized, aborting");
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