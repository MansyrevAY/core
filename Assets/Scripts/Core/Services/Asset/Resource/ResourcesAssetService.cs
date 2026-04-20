using System;
using System.Collections.Generic;
using Core.Infra.Log;
using Cysharp.Threading.Tasks;
using UnityEngine;
using IServiceProvider = Core.Infra.Service.IServiceProvider;
using Object = UnityEngine.Object;

namespace Core.Services.Asset.Resource
{
    public class ResourcesAssetService : IAssetService
    {
        private readonly Dictionary<string, ResourceAssetHandle> _handles = new();
        
        public void Init(IServiceProvider provider)
        {
        }

        public async UniTask<GameObject> Instantiate(string id)
        {
            var handle = await GetHandle(id);
            handle.AddInstance();
            
            _handles.TryAdd(id, handle);
            
            return Object.Instantiate(handle.Asset) as GameObject;
        }

        public GameObject Instantiate<T>(IInstance<T> instance) where T : Object
        {
            if (!_handles.TryGetValue(instance.Id, out var handle))
            {
                throw new ArgumentOutOfRangeException($"{nameof(instance.Id)}", $"Handle for asset {instance.Id} not found");
            }
            
            handle.AddInstance();
            
            return Object.Instantiate(handle.Asset) as GameObject;
        }

        public async UniTask<T> Instantiate<T>(string id) where T : MonoBehaviour
        {
            var handle = await GetHandle(id);
            handle.AddInstance();
            
            _handles.TryAdd(id, handle);
            
            var instance = Object.Instantiate(handle.Asset) as GameObject;
            var script = instance.GetComponent<T>();

            if (!script)
            {
                throw new NullReferenceException($"GameObject {id} is missing {nameof(T)}");
            }
            
            return script;
        }

        private async UniTask<ResourceAssetHandle> GetHandle(string id)
        {
            if (_handles.TryGetValue(id, out var handle))
            {
                return handle;
            }

            var asset = await Resources.LoadAsync<GameObject>(id);
            handle = new ResourceAssetHandle(asset, 0);
            return handle;
        }
        
        public async UniTask<T> Load<T>(string id) where T : Object
        {
            if (_handles.TryGetValue(id, out var handle))
            {
                return handle.Asset as T;
            }

            var asset = await Resources.LoadAsync<GameObject>(id);
            handle = new ResourceAssetHandle(asset, 1);
            _handles.Add(id, handle);
            
            return handle.Asset as T;
        }

        public void Destroy<T>(IInstance<T> instance) where T : Object
        {
            if (!_handles.TryGetValue(instance.Id, out var handle))
            {
                throw new ArgumentOutOfRangeException(nameof(instance.Id), $"No handle found for asset {instance.Id}");
            }
            
            handle.RemoveInstance();
            Object.Destroy(instance.Value);

            if (handle.UnloadRequested)
            {
                Unload(instance);
            }
        }

        public void Unload<T>(IInstance<T> instance) where T : Object
        {
            if (!_handles.TryGetValue(instance.Id, out var handle))
            {
                throw new ArgumentOutOfRangeException(nameof(instance.Id), $"No handle found for asset {instance.Id}");
            }

            if (handle.Count > 0)
            {
                Log.Warning($"There are {handle.Count} undestroyed instances of {instance.Id}, unexpected behavior might follow");
            }
            
            _handles.Remove(instance.Id);
            Resources.UnloadAsset(instance.Value);
        }
    }
}