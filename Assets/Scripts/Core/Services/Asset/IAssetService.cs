using Core.Infra.Service;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Core.Services.Asset
{
    public interface IAssetService : IService
    {
        UniTask<GameObject> Instantiate(string id);
        UniTask<T> Instantiate<T>(string id) where T : MonoBehaviour;
        
        UniTask<T> Load<T>(string id) where T : Object;
        void Destroy<T>(IAsset<T> asset) where T : Object;
        void Unload<T>(IAsset<T> asset) where T : Object;
    }
}