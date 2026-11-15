using System;
using System.Collections.Generic;
using Core.Infra.Logging;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Core.Services.Asset.Resource
{
    public class ResourcesAssetService : IAssetService
    {
        private readonly Dictionary<string, ResourceAssetHandle> _handles = new();

        public async UniTask<GameObject> Instantiate(string id)
        {
            var handle = await GetGameObjectHandle(id);

            return InstantiateFrom(handle, id);
        }

        public GameObject Instantiate<T>(IInstance<T> instance) where T : Object
        {
            if (!_handles.TryGetValue(instance.Id, out var handle))
            {
                throw new ArgumentOutOfRangeException($"{nameof(instance.Id)}", $"Handle for asset {instance.Id} not found");
            }

            return InstantiateFrom(handle, instance.Id);
        }

        public async UniTask<T> Instantiate<T>(string id) where T : MonoBehaviour
        {
            var handle = await GetGameObjectHandle(id);
            var instance = InstantiateFrom(handle, id);
            var script = instance.GetComponent<T>();

            if (!script)
            {
                handle.RemoveInstance();
                Object.Destroy(instance);

                throw new NullReferenceException($"GameObject {id} is missing {typeof(T).Name}");
            }

            return script;
        }

        public async UniTask<T> Load<T>(string id) where T : Object
        {
            if (_handles.TryGetValue(id, out var handle))
            {
                return handle.Asset as T;
            }

            var asset = await Resources.LoadAsync<T>(id);
            EnsureLoaded(asset, id);

            if (_handles.TryGetValue(id, out handle))
            {
                return handle.Asset as T;
            }

            _handles.Add(id, new ResourceAssetHandle(asset, 1));

            return asset as T;
        }

        public void Destroy<T>(IInstance<T> instance) where T : Object
        {
            if (!_handles.TryGetValue(instance.Id, out var handle))
            {
                throw new ArgumentOutOfRangeException(nameof(instance.Id), $"No handle found for asset {instance.Id}");
            }

            handle.RemoveInstance();
            Object.Destroy(instance.Value is Component component ? component.gameObject : instance.Value);

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
            handle.Release();
        }

        private async UniTask<ResourceAssetHandle> GetGameObjectHandle(string id)
        {
            if (_handles.TryGetValue(id, out var handle))
            {
                return handle;
            }

            var asset = await Resources.LoadAsync<GameObject>(id);
            EnsureLoaded(asset, id);

            if (_handles.TryGetValue(id, out handle))
            {
                return handle;
            }

            handle = new ResourceAssetHandle(asset, 0);
            _handles.Add(id, handle);

            return handle;
        }

        private static GameObject InstantiateFrom(ResourceAssetHandle handle, string id)
        {
            if (handle.Asset is not GameObject prefab)
            {
                throw new InvalidOperationException($"Asset {id} is not a GameObject and can't be instantiated");
            }

            handle.AddInstance();

            return Object.Instantiate(prefab);
        }

        private static void EnsureLoaded(Object asset, string id)
        {
            if (!asset)
            {
                throw new InvalidOperationException($"Failed to load resource {id}");
            }
        }
    }
}
