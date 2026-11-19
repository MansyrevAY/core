using System;
using Core.Networking.Fusion;
using Fusion;
using UnityEngine;

namespace Core.Networking.Players
{
    public class PlayerAvatar : NetworkBehaviour
    {
        [Networked] public long Identity { get; private set; }
        [Networked] public PlayerRef Player { get; private set; }
        [Networked] public NetworkBool IsDisconnected { get; private set; }
        [Networked] public NetworkBool IsReady { get; private set; }
        private PlayerService _players;
        private IDisposable _registration;
        private PlayerRef _observedPlayer;
        private bool _observedDisconnected;

        public override void Spawned()
        {
            _players = Runner.GetComponent<PlayerSessionContext>().Players;
            _registration = _players.Register(this);
        }

        public override void Render()
        {
            if (_observedPlayer == Player && _observedDisconnected == IsDisconnected)
            {
                return;
            }
            _observedPlayer = Player;
            _observedDisconnected = IsDisconnected;
            _players.Refresh(this);
        }

        public override void Despawned(NetworkRunner runner, bool hasState)
        {
            _registration?.Dispose();
            _registration = null;
        }

        public void Bind(long identity, PlayerRef player)
        {
            Identity = identity;
            Player = player;
            IsDisconnected = false;
            IsReady = false;
        }

        public void MarkDisconnected()
        {
            IsDisconnected = true;
            IsReady = false;
            Player = PlayerRef.None;
        }

        [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
        public void RPC_SetReady(RpcInfo info = default)
        {
            var source = info.Source == PlayerRef.None ? Runner.LocalPlayer : info.Source;
            if (source == Player && !IsDisconnected)
            {
                IsReady = true;
            }
        }
    }
}
