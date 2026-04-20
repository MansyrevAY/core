using System;

namespace Core.Services.Asset.Addressables
{
    public struct AssetHandle
    {
        public int Count { get; private set; }
        public readonly string Id;
        public bool UnloadAtZero;
        
        public bool UnloadRequested => UnloadAtZero && Count == 0;

        public AssetHandle(string id, int count)
        {
            Count = count;
            Id = id;
            UnloadAtZero = true;
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
    }
}