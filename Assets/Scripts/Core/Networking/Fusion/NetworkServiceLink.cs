using Core.Resources;
using Fusion;
using UnityEngine;

namespace Core.Networking.Fusion
{
    public class NetworkServiceLink : MonoBehaviour
    {
        public NetworkService Service { get; private set; }

        public void SetService(NetworkService service) => Service = service;
    }
}
