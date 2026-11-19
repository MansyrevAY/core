using Core.Resources;
using Fusion;
using UnityEngine;

namespace Core.Networking.Fusion
{
    public struct NetworkInputData : INetworkInput
    {
        public Vector2 MoveInput;
        public float LookYaw;
        public NetworkBool Jump;
        public NetworkBool Sprint;
        public NetworkBool Crouch;
        public NetworkBool Paused;
    }
}
