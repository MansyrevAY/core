using System;
using Core.Infra.Log;
using UnityEngine.ResourceManagement.AsyncOperations;
using Object = UnityEngine.Object;

namespace Core.Services.Asset.Addressables
{
    public class ObjectAssetHandle<T> : IAssetHandle where T : Object
    {
        public int Count { get; private set; }
        public bool UnloadAtZero { get; set; }
        public bool UnloadRequested => UnloadAtZero && Count == 0;

        private AsyncOperationHandle<T> _handle;

        public ObjectAssetHandle(AsyncOperationHandle<T> handle, int count, bool unloadAtZero = false)
        {
            _handle = handle;
            Count = count;
            UnloadAtZero = unloadAtZero;
            Asset = handle.Result;
        }
        
        public void AddInstance()
        {
            Count++;
        }

        public void RemoveInstance()
        {
            Count--;
            Count = Math.Max(Count, 0);
        }

        public void Release()
        {
            if (Count > 0)
            {
                Log.Error($"Can't release asset handle {_handle.DebugName}, {Count} instances persist");
                return;
            }
            
            UnityEngine.AddressableAssets.Addressables.Release(_handle);
        }

        public Object Asset { get; }
    }
}