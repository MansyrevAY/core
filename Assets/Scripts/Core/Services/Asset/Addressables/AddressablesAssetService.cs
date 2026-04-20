using System;
using System.Collections.Generic;
using Core.Infra.Log;
using Cysharp.Threading.Tasks;
using UnityEngine;
using IServiceProvider = Core.Infra.Service.IServiceProvider;
using Object = UnityEngine.Object;

namespace Core.Services.Asset.Addressables
{
    public class AddressablesAssetService : IAssetService
    {
        private readonly Dictionary<string, IAssetHandle> _cachedHandles = new();
        
        public void Init(IServiceProvider provider)
        {
            
        }

        public async UniTask<GameObject> Instantiate(string id)
        {
            var gameObject = await LoadGameObject(id);
            var instance = Object.Instantiate(gameObject, Vector3.zero, Quaternion.identity);
            
            return instance;
        }

        public async UniTask<T> Instantiate<T>(string id) where T : MonoBehaviour
        {
            var gameObject = await LoadGameObject(id);
            var instance = Object.Instantiate(gameObject, Vector3.zero, Quaternion.identity);
            var script = instance.GetComponent<T>();

            if (!script)
            {
                throw new NullReferenceException($"Asset {id} does not have {nameof(T)} attached");
            }
            
            return script;
        }

        private async UniTask<GameObject> LoadGameObject(string id)
        {
            GameObject asset = null;
            
            if (_cachedHandles.TryGetValue(id, out var cachedHandle))
            {
                cachedHandle.AddInstance();
                asset = cachedHandle.Asset as GameObject;
            }
            else
            {
                var handle = UnityEngine.AddressableAssets.Addressables.LoadAssetAsync<GameObject>(id);
                await handle.Task;
                _cachedHandles.Add(id, new GameObjectAssetHandle(handle, 1));
                asset = handle.Result;
            }

            return asset;
        }

        public async UniTask<T> Load<T>(string id) where T : Object
        {
            if (_cachedHandles.TryGetValue(id, out var cachedHandle))
            {
                Log.Warning($"Asset {id} is already loaded, same instance will be provided");
                return cachedHandle.Asset as T;
            }

            var handle = UnityEngine.AddressableAssets.Addressables.LoadAssetAsync<T>(id);
            await handle.Task;
            
            _cachedHandles.Add(id, new ObjectAssetHandle<T>(handle, 0));
            
            return handle.Result;
        }

        public void Destroy<T>(IAsset<T> asset) where T : Object
        {
            if (!_cachedHandles.TryGetValue(asset.Id, out var loadedAsset))
            {
                Log.Error($"Asset {asset.Id} is not loaded");
                return;
            }
            
            loadedAsset.RemoveInstance();
            
            Object.Destroy(asset.Instance);

            if (_cachedHandles[asset.Id].UnloadRequested)
            {
                _cachedHandles.Remove(asset.Id);
                Unload(asset);
            }
        }

        public void Unload<T>(IAsset<T> asset) where T : Object
        {
            if (_cachedHandles.TryGetValue(asset.Id, out var handle))
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
            _cachedHandles.Remove(asset.Id);
        }
    }
}