using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Core.Networking.Core
{
    public enum AvatarChangeKind
    {
        Registered,
        Unregistered,
        Rebound,
        ObservedStateChanged
    }

    public sealed class AvatarRegistry<TId, TPlayer, TAvatar> : IDisposable where TAvatar : class
    {
        public sealed class Record
        {
            public TId Id { get; }
            public TPlayer Player { get; }
            public TAvatar Avatar { get; }
            public long Identity { get; }
            public bool IsDisconnected { get; }
            public long Version { get; }
            internal Registration Lease { get; }

            internal Record(TId id, TPlayer player, TAvatar avatar, long identity, bool disconnected,
                Registration lease)
            {
                Id = id;
                Player = player;
                Avatar = avatar;
                Identity = identity;
                IsDisconnected = disconnected;
                Version = lease.Version;
                Lease = lease;
            }
        }

        public sealed class Registration : IDisposable
        {
            internal readonly AvatarRegistry<TId, TPlayer, TAvatar> Owner;
            public TId Id { get; }
            public long Version { get; }

            internal Registration(AvatarRegistry<TId, TPlayer, TAvatar> owner, TId id, long version)
            {
                Owner = owner;
                Id = id;
                Version = version;
            }

            public bool IsCurrent => Owner.IsCurrent(this);
            public void Dispose() => Owner.Unregister(this);
        }

        private readonly Dictionary<TId, Record> _byId = new();
        private readonly Dictionary<TPlayer, TId> _byPlayer = new();
        private readonly TPlayer _noPlayer;
        private long _version;
        private bool _disposed;
        public event Action<AvatarChangeKind, Record> Changed;
        public int Count => _byId.Count;
        public ReadOnlyCollection<Record> Records => new List<Record>(_byId.Values).AsReadOnly();

        public AvatarRegistry(TPlayer noPlayer) => _noPlayer = noPlayer;

        public Registration Register(TId id, TPlayer player, TAvatar avatar, long identity, bool disconnected)
        {
            ThrowIfDisposed();
            if (avatar == null)
            {
                throw new ArgumentNullException(nameof(avatar));
            }

            if (_byId.TryGetValue(id, out var existing))
            {
                if (!ReferenceEquals(existing.Avatar, avatar))
                {
                    throw new InvalidOperationException("Avatar id is occupied");
                }

                return Rebind(existing.Lease, player, identity, disconnected);
            }

            EnsurePlayerAvailable(id, player);
            var lease = new Registration(this, id, checked(++_version));
            var record = new Record(id, player, avatar, identity, disconnected, lease);
            _byId.Add(id, record);
            Index(record);
            Changed?.Invoke(AvatarChangeKind.Registered, record);
            return lease;
        }

        public Registration Rebind(Registration registration, TPlayer player, long identity, bool disconnected)
        {
            ThrowIfDisposed();
            if (!IsCurrent(registration))
            {
                throw new InvalidOperationException("Avatar registration is stale");
            }

            var previous = _byId[registration.Id];
            if (HasSameAvatarBinding(player, identity, disconnected, previous))
            {
                return registration;
            }

            EnsurePlayerAvailable(previous.Id, player);
            var lease = new Registration(this, previous.Id, checked(++_version));
            var record = new Record(previous.Id, player, previous.Avatar, identity, disconnected, lease);
            Unindex(previous);
            _byId[record.Id] = record;
            Index(record);
            if (Changed != null)
            {
                var changeKind = EqualityComparer<TPlayer>.Default.Equals(previous.Player, player)
                    ? AvatarChangeKind.ObservedStateChanged
                    : AvatarChangeKind.Rebound;
                Changed.Invoke(changeKind, record);
            }

            return lease;
        }

        public bool TryGetById(TId id, out Record record) => _byId.TryGetValue(id, out record);

        public void Unregister(TId id, long version)
        {
            if (TryGetRegistrationVersion(id, version, out var record))
            {
                Unregister(record.Lease);
            }
        }

        public bool TryGetByPlayer(TPlayer player, out Record record)
        {
            record = null;
            return _byPlayer.TryGetValue(player, out var id) && _byId.TryGetValue(id, out record);
        }

        private bool IsCurrent(Registration lease) => !_disposed && lease != null &&
                                                      ReferenceEquals(lease.Owner, this) &&
                                                      _byId.TryGetValue(lease.Id, out var record) &&
                                                      record.Version == lease.Version;

        private void Unregister(Registration lease)
        {
            if (!IsCurrent(lease))
            {
                return;
            }

            var record = _byId[lease.Id];
            Unindex(record);
            _byId.Remove(lease.Id);
            Changed?.Invoke(AvatarChangeKind.Unregistered, record);
        }

        private void EnsurePlayerAvailable(TId id, TPlayer player)
        {
            if (IsPlayerOccupied(id, player))
            {
                throw new InvalidOperationException("Player is already bound to another avatar");
            }
        }

        private void Index(Record record)
        {
            if (!EqualityComparer<TPlayer>.Default.Equals(record.Player, _noPlayer))
            {
                _byPlayer[record.Player] = record.Id;
            }
        }

        private void Unindex(Record record)
        {
            if (IsCurrentPlayerIndex(record))
            {
                _byPlayer.Remove(record.Player);
            }
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(AvatarRegistry<TId, TPlayer, TAvatar>));
            }
        }

        public void Dispose()
        {
            _disposed = true;
            _byId.Clear();
            _byPlayer.Clear();
            Changed = null;
        }

        private bool HasSameAvatarBinding(TPlayer player, long identity, bool disconnected, Record previous)
        {
            return EqualityComparer<TPlayer>.Default.Equals(previous.Player, player) && previous.Identity == identity &&
                   previous.IsDisconnected == disconnected;
        }

        private bool TryGetRegistrationVersion(TId id, long version, out Record record)
        {
            record = default;
            return _byId.TryGetValue(id, out record) && record.Version == version;
        }

        private bool IsPlayerOccupied(TId id, TPlayer player)
        {
            return !EqualityComparer<TPlayer>.Default.Equals(player, _noPlayer) &&
                   _byPlayer.TryGetValue(player, out var occupied) &&
                   !EqualityComparer<TId>.Default.Equals(occupied, id);
        }

        private bool IsCurrentPlayerIndex(Record record)
        {
            return _byPlayer.TryGetValue(record.Player, out var id) &&
                   EqualityComparer<TId>.Default.Equals(id, record.Id);
        }
    }
}
