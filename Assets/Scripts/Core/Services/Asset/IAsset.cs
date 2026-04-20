using UnityEngine;

namespace Core.Services.Asset
{
    public interface IAsset<out T> where T : Object
    {
        public string Id { get; }
        public T Instance { get; }
    }
}