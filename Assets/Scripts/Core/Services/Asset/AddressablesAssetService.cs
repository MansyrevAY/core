using Core.Infra.Service;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Core.Services.Asset
{
    public class AddressablesAssetService : IAssetService
    {
        public void Init(IServiceProvider provider)
        {
            
        }

        public async UniTask<T> Instantiate<T>(string id) where T : Object
        {
            var handle = Addressables.InstantiateAsync(id, Vector3.zero, Quaternion.identity);
            await handle.Task;

            if (typeof(MonoBehaviour).IsAssignableFrom(typeof(T)))
            {
                var script = handle.Result.GetComponent<T>();
                return script;
            }
            
            return handle.Result as T;
        }

        public async UniTask<T> Load<T>(string id) where T : Object
        {
            var handle = Addressables.LoadAssetAsync<T>(id);
            await handle.Task;
            
            return handle.Result;
        }

        public void Destroy(Object asset)
        {
            Object.Destroy(asset);
        }

        public void Unload(GameObject asset)
        {
            Addressables.ReleaseInstance(asset);
        }
    }
}