using System.Collections.Generic;
using Core.Infra.Log;
using Core.Infra.Service;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Core.Services.Asset.Addressables
{
    public class AddressablesAssetService : IAssetService
    {
        private readonly Dictionary<string, IAssetHandle> _loadedAssets = new();
        
        public void Init(IServiceProvider provider)
        {
            
        }

        public async UniTask<T> Instantiate<T>(string id) where T : Object
        {
            var handle = UnityEngine.AddressableAssets.Addressables.InstantiateAsync(id, Vector3.zero, Quaternion.identity);
            await handle.Task;

            if (!_loadedAssets.TryGetValue(id, out var asset))
            {
                _loadedAssets.Add(id, new GameObjectAssetHandle(handle, 1));
            }
            else
            {
                asset.AddInstance();
            }

            if (typeof(MonoBehaviour).IsAssignableFrom(typeof(T)))
            {
                var script = handle.Result.GetComponent<T>();
                return script;
            }
            
            return handle.Result as T;
        }

        public async UniTask<T> Load<T>(string id) where T : Object
        {
            if (_loadedAssets.ContainsKey(id))
            {
                Log.Error($"Asset {id} is already loaded");
                return null;
            }

            var handle = UnityEngine.AddressableAssets.Addressables.LoadAssetAsync<T>(id);
            await handle.Task;
            
            _loadedAssets.Add(id, new ObjectAssetHandle<T>(handle, 0));
            
            return handle.Result;
        }

        public void Destroy<T>(IAsset<T> asset) where T : Object
        {
            if (!_loadedAssets.TryGetValue(asset.Id, out var loadedAsset))
            {
                Log.Error($"Asset {asset.Id} is not loaded");
                return;
            }
            
            loadedAsset.RemoveInstance();
            
            Object.Destroy(asset.Instance);

            if (_loadedAssets[asset.Id].UnloadRequested)
            {
                _loadedAssets.Remove(asset.Id);
                Unload(asset);
            }
        }

        public void Unload<T>(IAsset<T> asset) where T : Object
        {
            if (_loadedAssets.TryGetValue(asset.Id, out var handle))
            {
                if (handle.Count > 0)
                {
                    Log.Warning($"Unloading asset {asset.Id}, while {handle.Count} instances persist. Unexpected behavior may follow");
                }
            }
            else
            {
                return;
            }
            
            handle.Release();
            _loadedAssets.Remove(asset.Id);
        }
    }
}