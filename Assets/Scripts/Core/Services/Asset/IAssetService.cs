using Core.Infra.Service;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Core.Services.Asset
{
    public interface IAssetService : IService
    {
        UniTask<GameObject> Instantiate(string id);
        GameObject Instantiate<T>(IInstance<T> instance) where T: Object;
        UniTask<T> Instantiate<T>(string id) where T : MonoBehaviour;
        
        UniTask<T> Load<T>(string id) where T : Object;
        void Destroy<T>(IInstance<T> instance) where T : Object;
        void Unload<T>(IInstance<T> instance) where T : Object;
    }
}