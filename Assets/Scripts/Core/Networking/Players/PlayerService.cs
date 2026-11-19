using System;
using System.Linq;
using System.Collections.Generic;
using Core.Infra.Lifetime;
using Core.Networking.Core;
using Core.Networking.Fusion;
using Fusion;
using R3;
using AvatarIndex = Core.Networking.Core.AvatarRegistry<Fusion.NetworkId, Fusion.PlayerRef, Core.Networking.Players.PlayerAvatar>;

namespace Core.Networking.Players
{
    public sealed class PlayerService : IDisposable
    {
        private readonly Dictionary<NetworkId, AvatarIndex.Registration> _registrations = new();
        private readonly AvatarIndex _avatars = new(PlayerRef.None);
        private readonly IDisposable _networkSubscription;
        private readonly Subject<PlayerAvatar> _changed = new();
        public NetworkRunner Runner { get; }
        public RunnerGeneration Generation { get; }
        public Observable<PlayerAvatar> Changed => _changed;
        public AvatarIndex.Record[] Avatars => _avatars.Records.ToArray();
        public PlayerRef[] Participants { get; private set; } = Array.Empty<PlayerRef>();
        public bool IsDisposed { get; private set; }

        public PlayerService(NetworkRunner runner, RunnerGeneration generation, NetworkService network)
        {
            Runner = runner;
            Generation = generation;
            _avatars.Changed += OnAvatarChanged;
            _networkSubscription = network.Snapshot.SafeSubscribe(snapshot =>
            {
                if (snapshot.Generation == generation)
                {
                    Participants = snapshot.Peers.ToArray();
                }
            });
        }

        public IDisposable Register(PlayerAvatar avatar)
        {
            var id = avatar.Object.Id;
            _registrations[id] = _avatars.Register(id, avatar.Player, avatar, avatar.Identity, avatar.IsDisconnected);
            return Disposable.Create(() =>
            {
                if (_registrations.Remove(id, out var registration))
                {
                    registration.Dispose();
                }
            });
        }

        public void Refresh(PlayerAvatar avatar)
        {
            if (!IsDisposed && _registrations.TryGetValue(avatar.Object.Id, out var registration))
            {
                _registrations[avatar.Object.Id] = _avatars.Rebind(registration, avatar.Player, avatar.Identity, avatar.IsDisconnected);
            }
        }

        public bool TryGet(PlayerRef player, out PlayerAvatar avatar)
        {
            avatar = null;
            if (!_avatars.TryGetByPlayer(player, out var record) || record.IsDisconnected)
            {
                return false;
            }
            avatar = record.Avatar;
            return avatar;
        }

        public PlayerAvatar FindIdentity(long identity)
        {
            return _avatars.Records.FirstOrDefault(record => record.Avatar && record.Avatar.Identity == identity)?.Avatar;
        }

        public void Dispose()
        {
            if (IsDisposed)
            {
                return;
            }
            IsDisposed = true;
            _networkSubscription.Dispose();
            _avatars.Changed -= OnAvatarChanged;
            _avatars.Dispose();
            _changed.Dispose();
        }

        private void OnAvatarChanged(AvatarChangeKind kind, AvatarIndex.Record record)
        {
            _changed.OnNext(record.Avatar);
        }
    }
}
