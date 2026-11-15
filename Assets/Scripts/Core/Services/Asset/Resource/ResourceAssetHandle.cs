using UnityEngine;

namespace Core.Services.Asset.Resource
{
    public class ResourceAssetHandle : IAssetHandle
    {
        public int Count { get; private set; }
        public bool UnloadAtZero { get; set; }
        public bool UnloadRequested => UnloadAtZero && Count == 0;
        public Object Asset { get; }

        public ResourceAssetHandle(Object asset, int count, bool unloadAtZero = false)
        {
            Asset = asset;
            Count = count;
            UnloadAtZero = unloadAtZero;
        }

        public void AddInstance()
        {
            Count++;
        }

        public void RemoveInstance()
        {
            Count--;
            Count = Mathf.Max(0, Count);
        }

        public void Release()
        {
            if (Asset is GameObject or Component)
            {
                return;
            }

            Resources.UnloadAsset(Asset);
        }
    }
}
