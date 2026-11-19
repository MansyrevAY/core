using Core.Networking.Players;
using Fusion;
using UnityEngine;

namespace Game.Demo
{
    [RequireComponent(typeof(PlayerAvatar))]
    public sealed class DemoAvatar : NetworkBehaviour
    {
        private PlayerAvatar _avatar;
        private Vector2 _movement;
        private TickTimer _inputExpiry;

        public override void Spawned()
        {
            _avatar = GetComponent<PlayerAvatar>();
        }

        [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
        public void RPC_Move(Vector2 movement, RpcInfo info = default)
        {
            var source = info.Source == PlayerRef.None ? Runner.LocalPlayer : info.Source;
            if (source == _avatar.Player && _avatar.IsReady && !_avatar.IsDisconnected)
            {
                _movement = Vector2.ClampMagnitude(movement, 1);
                _inputExpiry = TickTimer.CreateFromSeconds(Runner, 0.25f);
            }
        }

        public override void FixedUpdateNetwork()
        {
            if (!Object.HasStateAuthority || _avatar.IsDisconnected || _inputExpiry.ExpiredOrNotRunning(Runner))
            {
                return;
            }
            var movement = new Vector3(_movement.x, 0, _movement.y);
            transform.position += movement * (4f * Runner.DeltaTime);
        }
    }
}
