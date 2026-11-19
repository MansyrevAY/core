using System;
using System.Collections.Generic;

namespace Core.Networking.Core
{
    public enum SpawnOutcome
    {
        None,
        SpawnedNew,
        RestoredExisting,
        AlreadyBound,
        Rejected,
        Failed
    }

    public enum SpawnRequestStage
    {
        Empty,
        Waiting,
        AuthorizedNew,
        AuthorizedRestore,
        Completed,
        Rejected,
        Failed
    }

    public enum SpawnFailure
    {
        None,
        InvalidRequest,
        InvalidSource,
        IdentityInUse,
        ReservationInUse,
        MatchFinished,
        RegistryNotReady,
        Timeout,
        AuthorityChanged,
        InvalidBinding,
        Capacity,
        Cancelled
    }

    public enum ReplicatedMatchPhase
    {
        Unknown,
        Preparing,
        Running,
        Finished
    }

    public readonly struct SpawnRequest
    {
        public string Id { get; }
        public int Player { get; }
        public long Identity { get; }
        public bool IsValid => Player > 0 && Identity != 0 && Guid.TryParseExact(Id, "N", out _);

        public SpawnRequest(string id, int player, long identity)
        {
            Id = id;
            Player = player;
            Identity = identity;
        }

        public bool HasSameKey(SpawnRequest other) =>
            Player == other.Player && string.Equals(Id, other.Id, StringComparison.Ordinal);
    }

    public readonly struct SpawnAdmissionRecord
    {
        public SpawnRequest Request { get; }
        public SpawnRequestStage Stage { get; }
        public SpawnOutcome Outcome { get; }
        public SpawnFailure Failure { get; }
        public uint AvatarId { get; }
        public int AuthorityEpoch { get; }
        public bool HoldsReservation { get; }

        public bool IsTerminal => Stage == SpawnRequestStage.Completed || Stage == SpawnRequestStage.Rejected ||
                                  Stage == SpawnRequestStage.Failed;

        public bool IsSuccess => Stage == SpawnRequestStage.Completed;

        public SpawnAdmissionRecord(SpawnRequest request, SpawnRequestStage stage, SpawnOutcome outcome,
            SpawnFailure failure, uint avatarId, int authorityEpoch, bool holdsReservation)
        {
            Request = request;
            Stage = stage;
            Outcome = outcome;
            Failure = failure;
            AvatarId = avatarId;
            AuthorityEpoch = authorityEpoch;
            HoldsReservation = holdsReservation;
        }
    }

    public readonly struct SpawnAdmissionFacts
    {
        public bool PlayerActive { get; }
        public bool RegistryReady { get; }
        public ReplicatedMatchPhase Phase { get; }
        public bool IdentityConflict { get; }
        public bool IdentityWasPresent { get; }
        public uint BoundAvatarId { get; }
        public uint RestorableAvatarId { get; }

        public SpawnAdmissionFacts(bool playerActive, bool registryReady, ReplicatedMatchPhase phase,
            bool identityConflict = false, bool identityWasPresent = false, uint boundAvatarId = 0,
            uint restorableAvatarId = 0)
        {
            PlayerActive = playerActive;
            RegistryReady = registryReady;
            Phase = phase;
            IdentityConflict = identityConflict;
            IdentityWasPresent = identityWasPresent;
            BoundAvatarId = boundAvatarId;
            RestorableAvatarId = restorableAvatarId;
        }
    }

    public interface ISpawnAdmissionStore
    {
        IReadOnlyList<SpawnAdmissionRecord> ReadRequests();
        bool TryWrite(SpawnAdmissionRecord record);
        bool TryRegisterAvatar(uint avatarId, long identity);
    }

    public sealed class SpawnAdmission
    {
        private readonly ISpawnAdmissionStore _store;

        public SpawnAdmission(ISpawnAdmissionStore store) =>
            _store = store ?? throw new ArgumentNullException(nameof(store));

        public bool TryGet(SpawnRequest request, out SpawnAdmissionRecord record)
        {
            foreach (var existing in _store.ReadRequests())
            {
                if (existing.Request.HasSameKey(request))
                {
                    record = existing;
                    return true;
                }
            }

            record = default;
            return false;
        }

        public SpawnAdmissionRecord Begin(SpawnRequest request, SpawnAdmissionFacts facts, int authorityEpoch)
        {
            if (!request.IsValid)
            {
                return Rejected(request, SpawnFailure.InvalidRequest, authorityEpoch);
            }

            if (TryGet(request, out var existing))
            {
                if (existing.Request.Identity != request.Identity)
                {
                    return Rejected(request, SpawnFailure.InvalidRequest, authorityEpoch);
                }

                if (existing.Stage != SpawnRequestStage.Waiting)
                {
                    return existing;
                }
            }

            var result = GetAdmissionResult(request, facts, authorityEpoch);
            return WriteOrCapacity(result);
        }

        private SpawnAdmissionRecord GetAdmissionResult(SpawnRequest request, SpawnAdmissionFacts facts,
            int authorityEpoch)
        {
            SpawnAdmissionRecord result;
            if (!facts.PlayerActive)
            {
                result = Rejected(request, SpawnFailure.InvalidSource, authorityEpoch);
            }
            else if (IsRegistryPending(facts))
            {
                result = new SpawnAdmissionRecord(request, SpawnRequestStage.Waiting, SpawnOutcome.None,
                    SpawnFailure.RegistryNotReady, 0, authorityEpoch, false);
            }
            else if (facts.IdentityConflict)
            {
                result = Rejected(request, SpawnFailure.IdentityInUse, authorityEpoch);
            }
            else if (HasConflictingReservation(request, facts.RestorableAvatarId))
            {
                result = Rejected(request, SpawnFailure.ReservationInUse, authorityEpoch);
            }
            else if (facts.BoundAvatarId != 0)
            {
                result = new SpawnAdmissionRecord(request, SpawnRequestStage.Completed, SpawnOutcome.AlreadyBound,
                    SpawnFailure.None, facts.BoundAvatarId, authorityEpoch, false);
            }
            else if (IsFinishedMatchRestore(facts))
            {
                result = Rejected(request, SpawnFailure.MatchFinished, authorityEpoch);
            }
            else
            {
                result = new SpawnAdmissionRecord(request,
                    facts.RestorableAvatarId == 0
                        ? SpawnRequestStage.AuthorizedNew
                        : SpawnRequestStage.AuthorizedRestore,
                    SpawnOutcome.None, SpawnFailure.None, facts.RestorableAvatarId, authorityEpoch, true);
            }

            return result;
        }

        private bool HasConflictingReservation(SpawnRequest request, uint avatarId)
        {
            foreach (var record in _store.ReadRequests())
            {
                if (IsReservationIrrelevant(request, record))
                {
                    continue;
                }

                if (IsReservationConflicting(request, avatarId, record))
                {
                    return true;
                }
            }

            return false;
        }

        public SpawnAdmissionRecord Confirm(SpawnRequest request, uint avatarId, bool bindingVerified,
            bool originVerified)
        {
            if (CannotFindMatchingRequest(request, out var record))
            {
                return Rejected(request, SpawnFailure.InvalidRequest, 0);
            }

            if (record.IsTerminal)
            {
                return record;
            }

            var restoring = record.Stage == SpawnRequestStage.AuthorizedRestore;
            if (IsSpawnBindingInvalid(avatarId, bindingVerified, originVerified, record, restoring))
            {
                return Fail(request, SpawnFailure.InvalidBinding);
            }

            if (!_store.TryRegisterAvatar(avatarId, request.Identity))
            {
                return Fail(request, SpawnFailure.Capacity);
            }

            var spawnAdmissionRecord = new SpawnAdmissionRecord(request, SpawnRequestStage.Completed,
                restoring ? SpawnOutcome.RestoredExisting : SpawnOutcome.SpawnedNew, SpawnFailure.None, avatarId,
                record.AuthorityEpoch, false);
            return WriteOrCapacity(spawnAdmissionRecord);
        }

        public SpawnAdmissionRecord Fail(SpawnRequest request, SpawnFailure failure)
        {
            if (!TryGet(request, out var record))
            {
                return Rejected(request, SpawnFailure.InvalidRequest, 0);
            }

            if (record.IsTerminal)
            {
                return record;
            }

            var spawnAdmissionRecord = new SpawnAdmissionRecord(record.Request, SpawnRequestStage.Failed,
                SpawnOutcome.Failed, failure, record.AvatarId, record.AuthorityEpoch, record.HoldsReservation);
            return WriteOrCapacity(spawnAdmissionRecord);
        }

        public void AuthorityChanged(int currentEpoch)
        {
            foreach (var record in _store.ReadRequests())
            {
                if (IsRequestFromPreviousAuthority(currentEpoch, record))
                {
                    Fail(record.Request, SpawnFailure.AuthorityChanged);
                }
            }
        }

        public SpawnAdmissionRecord AbortBeforeAction(SpawnRequest request)
        {
            if (!request.IsValid)
            {
                return Rejected(request, SpawnFailure.InvalidRequest, 0);
            }

            if (!TryGet(request, out var record))
            {
                var spawnAdmissionRecord = new SpawnAdmissionRecord(request, SpawnRequestStage.Failed,
                    SpawnOutcome.Failed, SpawnFailure.Cancelled, 0, 0, false);
                return WriteOrCapacity(spawnAdmissionRecord);
            }

            if (record.Request.Identity != request.Identity)
            {
                return Rejected(request, SpawnFailure.InvalidRequest, record.AuthorityEpoch);
            }

            if (!record.IsTerminal)
            {
                Fail(request, SpawnFailure.Cancelled);
            }

            ReleaseAfterProvenAbort(request, true);
            TryGet(request, out record);
            return record;
        }

        public bool ReleaseAfterProvenAbort(SpawnRequest request, bool abortVerified)
        {
            if (CannotReleaseReservation(request, abortVerified, out var record))
            {
                return false;
            }
            var spawnAdmissionRecord = new SpawnAdmissionRecord(record.Request, record.Stage, record.Outcome,
                record.Failure, record.AvatarId, record.AuthorityEpoch, false);
            return _store.TryWrite(spawnAdmissionRecord);
        }

        public bool ReconcileFailedObject(SpawnRequest request, uint avatarId, bool bindingVerified,
            bool originVerified)
        {
            if (CannotReconcileFailedObject(request, avatarId, bindingVerified, originVerified, out var record))
            {
                return false;
            }

            if (!_store.TryRegisterAvatar(avatarId, request.Identity))
            {
                return false;
            }
            var spawnAdmissionRecord = new SpawnAdmissionRecord(record.Request, record.Stage, record.Outcome,
                record.Failure, avatarId, record.AuthorityEpoch, false);
            return _store.TryWrite(spawnAdmissionRecord);
        }

        private SpawnAdmissionRecord WriteOrCapacity(SpawnAdmissionRecord record) => _store.TryWrite(record)
            ? record
            : new SpawnAdmissionRecord(record.Request, SpawnRequestStage.Failed, SpawnOutcome.Failed,
                SpawnFailure.Capacity,
                record.AvatarId, record.AuthorityEpoch, record.HoldsReservation);

        private static SpawnAdmissionRecord Rejected(SpawnRequest request, SpawnFailure failure, int epoch) =>
            new(request, SpawnRequestStage.Rejected, SpawnOutcome.Rejected, failure, 0, epoch, false);

        private bool IsRegistryPending(SpawnAdmissionFacts facts)
        {
            return facts.Phase == ReplicatedMatchPhase.Unknown || !facts.RegistryReady;
        }

        private bool IsFinishedMatchRestore(SpawnAdmissionFacts facts)
        {
            return facts.Phase == ReplicatedMatchPhase.Finished &&
                   (facts.IdentityWasPresent || facts.RestorableAvatarId != 0);
        }

        private bool IsReservationIrrelevant(SpawnRequest request, SpawnAdmissionRecord record)
        {
            return !record.HoldsReservation || record.Request.HasSameKey(request);
        }

        private bool IsReservationConflicting(SpawnRequest request, uint avatarId, SpawnAdmissionRecord record)
        {
            return record.Request.Identity == request.Identity || (avatarId != 0 && record.AvatarId == avatarId);
        }

        private bool CannotFindMatchingRequest(SpawnRequest request, out SpawnAdmissionRecord record)
        {
            record = default;
            return !TryGet(request, out record) || record.Request.Identity != request.Identity;
        }

        private bool IsSpawnBindingInvalid(uint avatarId, bool bindingVerified, bool originVerified,
            SpawnAdmissionRecord record, bool restoring)
        {
            return !bindingVerified || avatarId == 0 ||
                   (!restoring && record.Stage != SpawnRequestStage.AuthorizedNew) ||
                   (restoring ? record.AvatarId != avatarId : !originVerified);
        }

        private bool IsRequestFromPreviousAuthority(int currentEpoch, SpawnAdmissionRecord record)
        {
            return !record.IsTerminal && record.AuthorityEpoch != currentEpoch;
        }

        private bool CannotReleaseReservation(SpawnRequest request, bool abortVerified, out SpawnAdmissionRecord record)
        {
            record = default;
            return !abortVerified || !TryGet(request, out record) || record.Request.Identity != request.Identity ||
                   record.Stage != SpawnRequestStage.Failed;
        }

        private bool CannotReconcileFailedObject(SpawnRequest request, uint avatarId, bool bindingVerified,
            bool originVerified, out SpawnAdmissionRecord record)
        {
            record = default;
            return !TryGet(request, out record) || record.Request.Identity != request.Identity ||
                   record.Stage != SpawnRequestStage.Failed || !record.HoldsReservation || avatarId == 0 ||
                   !bindingVerified || (record.AvatarId == 0 ? !originVerified : record.AvatarId != avatarId);
        }
    }
}
