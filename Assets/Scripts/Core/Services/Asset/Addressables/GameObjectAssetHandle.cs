using UnityEngine;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Core.Services.Asset.Addressables
{
    public class GameObjectAssetHandle : ObjectAssetHandle<GameObject>
    {
        public GameObjectAssetHandle(AsyncOperationHandle<GameObject> handle, int count, bool unloadAtZero = false) : base(handle, count, unloadAtZero)
        {
        }
    }
}