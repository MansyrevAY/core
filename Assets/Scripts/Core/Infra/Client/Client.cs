using Core.Infra.Broadcaster;
using Core.Infra.Module;
using Core.Services.Asset;
using Core.Services.Asset.Resource;
using Core.Services.Data;
using UnityEngine;

namespace Core.Infra.Client
{
    // Composition root of the app: builds services and client-lifetime modules by hand, in dependency order.
    public class Client : MonoBehaviour
    {
        private readonly ModuleScope _modules = new();

        public IBroadcaster Broadcaster { get; private set; }

        protected IAssetService Assets { get; private set; }
        protected IRecordService Records { get; private set; }

        private void Awake()
        {
            Broadcaster = new Broadcaster.Broadcaster();

            CreateServices();
            CreateModules();

            _modules.Start();
            StartGame();
        }

        private void OnDestroy()
        {
            _modules.Cleanup();
        }

        protected virtual void CreateServices()
        {
            Assets = new ResourcesAssetService();
            Records = new RecordService(Assets);
        }

        protected virtual void CreateModules()
        {
        }

        protected T AddModule<T>(T module) where T : class, IModule
        {
            return _modules.Add(module);
        }

        protected virtual void StartGame()
        {
        }
    }
}
