using System.Collections.Generic;
using Core.Infra.Service;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Core.Services.Asset
{
    public class ResourcesAssetService : IAssetService
    {
        private readonly Dictionary<string, Object> _assets = new();
        
        public void Init(IServiceProvider provider)
        {
        }

        public async UniTask<T> Instantiate<T>(string id) where T : Object
        {
            if (!_assets.TryGetValue(id, out var asset))
            {
                asset = await Load<T>(id);
            }

            if (asset is MonoBehaviour behaviour)
            {
                var instance = Object.Instantiate(behaviour.gameObject);
                return instance.GetComponent<T>();
            }
            
            return Object.Instantiate(asset) as T;
        }
        
        public UniTask<T> Load<T>(string id) where T : Object
        {
            if (_assets.TryGetValue(id, out var asset))
            {
                return UniTask.FromResult(asset as T);
            }
            
            asset = Resources.Load<T>(id);
            
            _assets[id] = asset;
            return UniTask.FromResult((T) asset);
        }

        public void Destroy(Object asset)
        {
            Object.Destroy(asset);
        }

        public void Unload(GameObject asset)
        {
        }
    }
}