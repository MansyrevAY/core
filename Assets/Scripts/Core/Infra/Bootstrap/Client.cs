using Core.Infra.Messaging;
using Core.Infra.Module;
using Core.Infra.Utils;
using Core.Services.Asset;
using Core.Services.Asset.Resource;
using Core.Services.Data;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Core.Infra.Bootstrap
{
    // Composition root of the app: builds services and client-lifetime modules by hand, in dependency order.
    //
    // Boot order: CreateServices -> InitializeServices (async) -> CreateModules -> modules Start -> StartGame.
    // Services are fully initialized before any module is built, so modules can rely on them in Start.
    public class Client : MonoBehaviour
    {
        private readonly ModuleScope _modules = new();

        public IBroadcaster Broadcaster { get; private set; }

        protected IAssetService Assets { get; private set; }
        protected IRecordService Records { get; private set; }

        private void Awake()
        {
            Broadcaster = new Broadcaster();

            CreateServices();
            Boot().FireAndForget();
        }

        private void OnDestroy()
        {
            _modules.Dispose();
        }

        private async UniTask Boot()
        {
            var cancellationToken = destroyCancellationToken;
            await InitializeServices();

            // The client may have been destroyed while the services were loading.
            if (cancellationToken.IsCancellationRequested)
            {
                return;
            }

            CreateModules();

            _modules.Start();
            StartGame();
        }

        protected virtual void CreateServices()
        {
            Assets = new ResourcesAssetService();
            Records = new RecordService(Assets);
        }

        // Async setup of the services built in CreateServices. Override to add more, and call base first
        // if the extra work needs the records loaded.
        protected virtual UniTask InitializeServices()
        {
            return Records.CreateAll();
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
