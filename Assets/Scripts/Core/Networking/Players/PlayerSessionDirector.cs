using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Core.Networking.Core;
using Core.Networking.Fusion;
using Cysharp.Threading.Tasks;
using Fusion;
using UnityEngine;

namespace Core.Networking.Players
{
    public sealed class PlayerSessionDirector : NetworkBehaviour, ISpawnAdmissionStore, IPlayerSpawnPort, IStateAuthorityChanged
    {
        public const int REQUEST_CAPACITY = 32;
        [SerializeField] private NetworkObject _avatarPrefab;
        [Networked, Capacity(REQUEST_CAPACITY)] private NetworkArray<ReplicatedSpawnRequest> Requests { get; }
        [Networked] private int AuthorityEpoch { get; set; }
        private SpawnAdmission _admission;
        private PlayerSessionContext _context;
        private SpawnAdmission Admission => _admission ??= new SpawnAdmission(this);

        public void Configure(NetworkObject prefab)
        {
            _avatarPrefab = prefab;
        }

        public override void Spawned()
        {
            _context = Runner.GetComponent<PlayerSessionContext>();
            _context.Director = this;
            StateAuthorityChanged();
        }

        public void StateAuthorityChanged()
        {
            if (Object.HasStateAuthority)
            {
                AuthorityEpoch++;
                Admission.AuthorityChanged(AuthorityEpoch);
            }
        }

        public override void Despawned(NetworkRunner runner, bool hasState)
        {
            if (_context && _context.Director == this)
            {
                _context.Director = null;
            }
        }

        public override void FixedUpdateNetwork()
        {
            if (!Object.HasStateAuthority)
            {
                return;
            }
            foreach (var record in _context.Players.Avatars)
            {
                var avatar = record.Avatar;
                if (avatar && !avatar.IsDisconnected && !Runner.ActivePlayers.Contains(avatar.Player))
                {
                    avatar.MarkDisconnected();
                    _context.Players.Refresh(avatar);
                }
            }
        }

        public async UniTask<SpawnAdmissionRecord> ExecuteSpawnAsync(SpawnRequest request, CancellationToken cancellationToken)
        {
            var nextRetry = 0f;
            while (this && Object && Runner.IsRunning)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (Admission.TryGet(request, out var record) && record.IsTerminal)
                {
                    if (record.Failure != SpawnFailure.AuthorityChanged)
                    {
                        return record;
                    }
                    throw new InvalidOperationException("Spawn authority changed; retry the room join");
                }
                if (Time.realtimeSinceStartup >= nextRetry)
                {
                    nextRetry = Time.realtimeSinceStartup + 0.5f;
                    RPC_RequestSpawn(request.Id, request.Identity);
                }
                await UniTask.NextFrame(cancellationToken);
            }
            throw new OperationCanceledException("Spawn session ended");
        }

        [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
        private void RPC_RequestSpawn(string requestId, long identity, RpcInfo info = default)
        {
            var source = info.Source == PlayerRef.None ? Runner.LocalPlayer : info.Source;
            var request = new SpawnRequest(requestId, source.RawEncoded, identity);
            var existing = _context.Players.FindIdentity(identity);
            var conflict = existing && !existing.IsDisconnected && existing.Player != source && Runner.ActivePlayers.Contains(existing.Player);
            _context.Players.TryGet(source, out var bound);
            var facts = new SpawnAdmissionFacts(Runner.ActivePlayers.Contains(source), true,
                ReplicatedMatchPhase.Running, conflict, existing,
                bound ? bound.Object.Id.Raw : 0, existing && !conflict ? existing.Object.Id.Raw : 0);
            var admission = Admission.Begin(request, facts, AuthorityEpoch);
            if (admission.IsTerminal)
            {
                return;
            }
            PlayerAvatar avatar;
            if (admission.Stage == SpawnRequestStage.AuthorizedRestore)
            {
                avatar = existing;
                avatar.Bind(identity, source);
                _context.Players.Refresh(avatar);
            }
            else if (admission.Stage == SpawnRequestStage.AuthorizedNew)
            {
                var position = new Vector3((source.PlayerId % 8) * 2f, 1f, 0f);
                var instance = Runner.Spawn(_avatarPrefab, position, Quaternion.identity,
                    onBeforeSpawned: (_, created) => created.GetComponent<PlayerAvatar>().Bind(identity, source));
                avatar = instance.GetComponent<PlayerAvatar>();
            }
            else
            {
                return;
            }
            Admission.Confirm(request, avatar.Object.Id.Raw, avatar.Player == source, avatar.Identity == identity);
            Runner.SessionInfo.UpdateCustomProperties(new Dictionary<string, SessionProperty>
            {
                [LobbyDirectory.ConfirmedPlayersPropertyKey] = _context.Players.Avatars.Count(record => record.Avatar && !record.Avatar.IsDisconnected)
            });
        }

        public IReadOnlyList<SpawnAdmissionRecord> ReadRequests()
        {
            var records = new List<SpawnAdmissionRecord>();
            for (var index = 0; index < Requests.Length; index++)
            {
                var request = Requests[index];
                if (request.Identity != 0)
                {
                    records.Add(request.Read());
                }
            }
            return records;
        }

        public bool TryWrite(SpawnAdmissionRecord record)
        {
            var target = -1;
            for (var index = 0; index < Requests.Length; index++)
            {
                var current = Requests[index];
                if (current.Read().Request.HasSameKey(record.Request))
                {
                    target = index;
                    break;
                }
                if (target < 0 && (current.Identity == 0 || !Runner.ActivePlayers.Contains(current.Player)))
                {
                    target = index;
                }
            }
            if (target < 0)
            {
                return false;
            }
            Requests.Set(target, ReplicatedSpawnRequest.From(record));
            return true;
        }

        public bool TryRegisterAvatar(uint avatarId, long identity)
        {
            var avatar = _context.Players.FindIdentity(identity);
            return avatar && avatar.Object.Id.Raw == avatarId;
        }
    }
}
