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
