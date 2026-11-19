using Core.Resources;
using System;
using System.Collections.Generic;
using Core.Networking.Core;
using NUnit.Framework;

namespace Core.Tests.Networking
{
    public sealed class SpawnAdmissionTests
    {
        private sealed class Store : ISpawnAdmissionStore
        {
            public readonly List<SpawnAdmissionRecord> Records = new();
            public readonly Dictionary<uint, long> Avatars = new();
            public int ManifestWrites;
            public int Capacity = 32;
            public IReadOnlyList<SpawnAdmissionRecord> ReadRequests() => Records.ToArray();

            public bool TryWrite(SpawnAdmissionRecord record)
            {
                var index = Records.FindIndex(existing => existing.Request.HasSameKey(record.Request));
                if (index >= 0)
                {
                    Records[index] = record;
                    return true;
                }

                if (Records.Count >= Capacity)
                {
                    return false;
                }

                Records.Add(record);
                return true;
            }

            public bool TryRegisterAvatar(uint id, long identity)
            {
                ManifestWrites++;
                Avatars[id] = identity;
                return true;
            }
        }

        private static SpawnRequest Request(int player = 1, long identity = 100)
        {
            var idText = Guid.NewGuid().ToString("N");
            return new(idText, player, identity);
        }
        [Test]
        public void T12_VerifiedLateObjectResolvesReservationButNeverChangesFailedOutcome()
        {
            var store = new Store();
            var journal = new SpawnAdmission(store);
            var request = Request();
            var facts = Ready();
            journal.Begin(request, facts, 1);
            journal.AuthorityChanged(2);
            var actual = journal.ReconcileFailedObject(request, 51, true, false);
            Assert.That(actual, Is.False);
            var actual2 = journal.ReconcileFailedObject(request, 51, true, true);
            Assert.That(actual2, Is.True);
            var actual3 = journal.ReconcileFailedObject(request, 51, true, true);
            Assert.That(actual3, Is.False);
            var actual4 = journal.TryGet(request, out var result);
            Assert.That(actual4, Is.True);
            var actual5 = result.Outcome;
            var expression = Is.EqualTo(SpawnOutcome.Failed);
            Assert.That(actual5, expression);
            var actual6 = result.Failure;
            var expression2 = Is.EqualTo(SpawnFailure.AuthorityChanged);
            Assert.That(actual6, expression2);
            Assert.That(result.HoldsReservation, Is.False);
            var actual7 = store.ManifestWrites;
            var expression3 = Is.EqualTo(1);
            Assert.That(actual7, expression3);
            var request3 = Request();
            var spawnAdmissionFacts = new SpawnAdmissionFacts(true, true, ReplicatedMatchPhase.Running, boundAvatarId: 51);
            var actual8 = journal.Begin(request3, spawnAdmissionFacts, 2).Outcome;
            var expression4 = Is.EqualTo(SpawnOutcome.AlreadyBound);
            Assert.That(actual8, expression4);
        }

        [Test]
        public void AbortBeforeGrantPreventsDelayedRequestFromSpawning()
        {
            var store = new Store();
            var journal = new SpawnAdmission(store);
            var request = Request();
            var aborted = journal.AbortBeforeAction(request);
            var actual = aborted.Stage;
            var expression = Is.EqualTo(SpawnRequestStage.Failed);
            Assert.That(actual, expression);
            var facts = Ready();
            var actual2 = journal.Begin(request, facts, 1).Failure;
            var expression2 = Is.EqualTo(SpawnFailure.Cancelled);
            Assert.That(actual2, expression2);
            var actual3 = journal.Confirm(request, 51, true, true).IsSuccess;
            Assert.That(actual3, Is.False);
            Assert.That(store.ManifestWrites, Is.Zero);
        }

        [TestCase(0u)]
        [TestCase(42u)]
        public void ProvenAbortAfterHandoffAllowsRetryWithoutRewritingFailure(uint avatar)
        {
            var store = new Store();
            var journal = new SpawnAdmission(store);
            var request = Request();
            var facts = Ready(avatar);
            journal.Begin(request, facts, 1);
            journal.AuthorityChanged(2);
            var aborted = journal.AbortBeforeAction(request);
            var actual = aborted.Failure;
            var expression = Is.EqualTo(SpawnFailure.AuthorityChanged);
            Assert.That(actual, expression);
            Assert.That(aborted.HoldsReservation, Is.False);
            var request3 = Request();
            var facts2 = Ready(avatar);
            var actual2 = journal.Begin(request3, facts2, 2).HoldsReservation;
            Assert.That(actual2, Is.True);
            Assert.That(store.ManifestWrites, Is.Zero);
        }

        [Test]
        public void AbortWithAnotherIdentityCannotReleaseTheReservation()
        {
            var store = new Store();
            var journal = new SpawnAdmission(store);
            var request = Request();
            var facts = Ready();
            journal.Begin(request, facts, 1);
            journal.Fail(request, SpawnFailure.Cancelled);
            var forged = new SpawnRequest(request.Id, request.Player, request.Identity + 1);
            var actual = journal.AbortBeforeAction(forged).Failure;
            var expression = Is.EqualTo(SpawnFailure.InvalidRequest);
            Assert.That(actual, expression);
            var request3 = Request();
            var facts2 = Ready();
            var actual2 = journal.Begin(request3, facts2, 1).Failure;
            var expression2 = Is.EqualTo(SpawnFailure.ReservationInUse);
            Assert.That(actual2, expression2);
        }

        [Test]
        public void AbortFromAnotherPlayerCannotReleaseTheReservation()
        {
            var store = new Store();
            var journal = new SpawnAdmission(store);
            var request = Request();
            var facts = Ready();
            journal.Begin(request, facts, 1);
            journal.Fail(request, SpawnFailure.Cancelled);
            var spawnRequest = new SpawnRequest(request.Id, 2, request.Identity);
            journal.AbortBeforeAction(spawnRequest);
            var request3 = Request();
            var facts2 = Ready();
            var actual = journal.Begin(request3, facts2, 1).Failure;
            var expression = Is.EqualTo(SpawnFailure.ReservationInUse);
            Assert.That(actual, expression);
        }

        [Test]
        public void DuplicateAbortDoesNotChangeCompletedSpawn()
        {
            var store = new Store();
            var journal = new SpawnAdmission(store);
            var request = Request();
            var facts = Ready();
            journal.Begin(request, facts, 1);
            journal.Confirm(request, 51, true, true);
            var actual = journal.AbortBeforeAction(request).Outcome;
            var expression = Is.EqualTo(SpawnOutcome.SpawnedNew);
            Assert.That(actual, expression);
            var actual2 = journal.AbortBeforeAction(request).AvatarId;
            var expression2 = Is.EqualTo(51);
            Assert.That(actual2, expression2);
            var actual3 = store.ManifestWrites;
            var expression3 = Is.EqualTo(1);
            Assert.That(actual3, expression3);
        }

        private static SpawnAdmissionFacts Ready(uint restored = 0) => new(true, true, ReplicatedMatchPhase.Running,
            identityWasPresent: restored != 0, restorableAvatarId: restored);

        [Test]
        public void T09_RestoreIsTerminalOnlyAfterVerifiedBindingOfTheSameAvatar()
        {
            var store = new Store();
            var journal = new SpawnAdmission(store);
            var request = Request();
            var facts = Ready(42);
            var permission = journal.Begin(request, facts, 1);
            var actual = permission.Stage;
            var expression = Is.EqualTo(SpawnRequestStage.AuthorizedRestore);
            Assert.That(actual, expression);
            var actual2 = permission.Outcome;
            var expression2 = Is.EqualTo(SpawnOutcome.None);
            Assert.That(actual2, expression2);
            Assert.That(permission.IsTerminal, Is.False);
            var complete = journal.Confirm(request, 42, true, false);
            var actual3 = complete.Outcome;
            var expression3 = Is.EqualTo(SpawnOutcome.RestoredExisting);
            Assert.That(actual3, expression3);
            var actual4 = complete.AvatarId;
            var expression4 = Is.EqualTo(42);
            Assert.That(actual4, expression4);
            Assert.That(complete.HoldsReservation, Is.False);
        }

        [Test]
        public void T10_DuplicateRequestsAndConfirmationsHaveOneManifestCommit()
        {
            var store = new Store();
            var journal = new SpawnAdmission(store);
            var request = Request();
            var facts = Ready();
            var first = journal.Begin(request, facts, 1);
            var facts2 = Ready();
            var repeated = journal.Begin(request, facts2, 1);
            var actual = repeated.Stage;
            var expression = Is.EqualTo(first.Stage);
            Assert.That(actual, expression);
            var actual2 = store.Records.Count;
            var expression2 = Is.EqualTo(1);
            Assert.That(actual2, expression2);
            var actual3 = journal.Confirm(request, 51, true, true).Outcome;
            var expression3 = Is.EqualTo(SpawnOutcome.SpawnedNew);
            Assert.That(actual3, expression3);
            var actual4 = journal.Confirm(request, 51, true, true).Outcome;
            var expression4 = Is.EqualTo(SpawnOutcome.SpawnedNew);
            Assert.That(actual4, expression4);
            var facts3 = Ready();
            var actual5 = journal.Begin(request, facts3, 1).Outcome;
            var expression5 = Is.EqualTo(SpawnOutcome.SpawnedNew);
            Assert.That(actual5, expression5);
            var actual6 = store.ManifestWrites;
            var expression6 = Is.EqualTo(1);
            Assert.That(actual6, expression6);
        }

        [Test]
        public void T11_ActiveIdentityConflictCannotGrantOrChangeAnAvatar()
        {
            var store = new Store();
            var journal = new SpawnAdmission(store);
            var request = Request(2);
            var spawnAdmissionFacts = new SpawnAdmissionFacts(true, true, ReplicatedMatchPhase.Running, identityConflict: true);
            var result = journal.Begin(request, spawnAdmissionFacts, 1);
            var actual = result.Outcome;
            var expression = Is.EqualTo(SpawnOutcome.Rejected);
            Assert.That(actual, expression);
            var actual2 = result.Failure;
            var expression2 = Is.EqualTo(SpawnFailure.IdentityInUse);
            Assert.That(actual2, expression2);
            Assert.That(store.ManifestWrites, Is.Zero);
        }

        [TestCase(0u)]
        [TestCase(42u)]
        public void T12_HandoffRetainsUncertainReservationAndDoesNotReturnLateSuccess(uint avatar)
        {
            var store = new Store();
            var previous = new SpawnAdmission(store);
            var request = Request();
            var facts = Ready(avatar);
            previous.Begin(request, facts, 1);
            var nextAuthority = new SpawnAdmission(store);
            nextAuthority.AuthorityChanged(2);
            var failed = nextAuthority.Confirm(request, avatar == 0 ? 51u : avatar, true, true);
            var actual = failed.Outcome;
            var expression = Is.EqualTo(SpawnOutcome.Failed);
            Assert.That(actual, expression);
            var actual2 = failed.Failure;
            var expression2 = Is.EqualTo(SpawnFailure.AuthorityChanged);
            Assert.That(actual2, expression2);
            Assert.That(failed.HoldsReservation, Is.True);
            var request3 = Request();
            var facts2 = Ready(avatar);
            var actual3 = nextAuthority.Begin(request3, facts2, 2).Failure;
            var expression3 = Is.EqualTo(SpawnFailure.ReservationInUse);
            Assert.That(actual3, expression3);
            Assert.That(store.ManifestWrites, Is.Zero);
        }

        [Test]
        public void T12_OnlyVerifiedAbortOfTheExactFailedRequestReleasesItsReservation()
        {
            var store = new Store();
            var journal = new SpawnAdmission(store);
            var request = Request();
            var facts = Ready();
            journal.Begin(request, facts, 1);
            journal.Fail(request, SpawnFailure.Timeout);
            var actual = journal.ReleaseAfterProvenAbort(request, false);
            Assert.That(actual, Is.False);
            var spawnRequest = new SpawnRequest(request.Id, 2, request.Identity);
            var actual2 = journal.ReleaseAfterProvenAbort(spawnRequest, true);
            Assert.That(actual2, Is.False);
            var request3 = Request();
            var facts2 = Ready();
            var actual3 = journal.Begin(request3, facts2, 1).Failure;
            var expression = Is.EqualTo(SpawnFailure.ReservationInUse);
            Assert.That(actual3, expression);
            var actual4 = journal.ReleaseAfterProvenAbort(request, true);
            Assert.That(actual4, Is.True);
            var request4 = Request();
            var facts3 = Ready();
            var actual5 = journal.Begin(request4, facts3, 1).Stage;
            var expression2 = Is.EqualTo(SpawnRequestStage.AuthorizedNew);
            Assert.That(actual5, expression2);
            var actual6 = journal.Confirm(request, 51, true, true).Outcome;
            var expression3 = Is.EqualTo(SpawnOutcome.Failed);
            Assert.That(actual6, expression3);
        }

        [Test]
        public void T13_ReplicatedFinishedRuleRejectsRestoreOnANewAuthority()
        {
            var store = new Store();
            var journal = new SpawnAdmission(store);
            var request = Request();
            var spawnAdmissionFacts = new SpawnAdmissionFacts(true, true, ReplicatedMatchPhase.Finished, identityWasPresent: true, restorableAvatarId: 42);
            var result = journal.Begin(request, spawnAdmissionFacts, 2);
            var actual = result.Failure;
            var expression = Is.EqualTo(SpawnFailure.MatchFinished);
            Assert.That(actual, expression);
        }

        [TestCase(false, ReplicatedMatchPhase.Running)]
        [TestCase(true, ReplicatedMatchPhase.Unknown)]
        public void T14_UnknownRegistryOrPolicyNeverAuthorizesSpawn(bool registryReady, ReplicatedMatchPhase phase)
        {
            var store = new Store();
            var journal = new SpawnAdmission(store);
            var request = Request();
            var spawnAdmissionFacts = new SpawnAdmissionFacts(true, registryReady, phase);
            var pending = journal.Begin(request, spawnAdmissionFacts, 1);
            var actual = pending.Stage;
            var expression = Is.EqualTo(SpawnRequestStage.Waiting);
            Assert.That(actual, expression);
            Assert.That(pending.IsTerminal, Is.False);
            var actual2 = journal.Fail(request, SpawnFailure.Timeout).Outcome;
            var expression2 = Is.EqualTo(SpawnOutcome.Failed);
            Assert.That(actual2, expression2);
            var facts = Ready();
            var actual3 = journal.Begin(request, facts, 1).Outcome;
            var expression3 = Is.EqualTo(SpawnOutcome.Failed);
            Assert.That(actual3, expression3);
        }

        [Test]
        public void T14_ProvenMissingBodyAllowsNewSpawnAndCurrentBindingIsAlreadyBound()
        {
            var store = new Store();
            var journal = new SpawnAdmission(store);
            var request = Request();
            var spawnAdmissionFacts = new SpawnAdmissionFacts(true, true, ReplicatedMatchPhase.Running, identityWasPresent: true);
            var newSpawn = journal.Begin(request, spawnAdmissionFacts, 1);
            var actual = newSpawn.Stage;
            var expression = Is.EqualTo(SpawnRequestStage.AuthorizedNew);
            Assert.That(actual, expression);
            var request2 = Request(2, 200);
            var spawnAdmissionFacts2 = new SpawnAdmissionFacts(true, true, ReplicatedMatchPhase.Running, boundAvatarId: 42);
            var bound = journal.Begin(request2, spawnAdmissionFacts2, 1);
            var actual2 = bound.Outcome;
            var expression2 = Is.EqualTo(SpawnOutcome.AlreadyBound);
            Assert.That(actual2, expression2);
        }

        [Test]
        public void T10_MismatchedOriginOrBindingCannotCommitAnAuthorizedSpawn()
        {
            var store = new Store();
            var journal = new SpawnAdmission(store);
            var request = Request();
            var facts = Ready();
            journal.Begin(request, facts, 1);
            var actual = journal.Confirm(request, 51, true, false).Failure;
            var expression = Is.EqualTo(SpawnFailure.InvalidBinding);
            Assert.That(actual, expression);
            var actual2 = journal.Confirm(request, 51, true, true).Outcome;
            var expression2 = Is.EqualTo(SpawnOutcome.Failed);
            Assert.That(actual2, expression2);
            Assert.That(store.ManifestWrites, Is.Zero);
        }

        [Test]
        public void T10_CapacityAndInvalidRequestsCannotProduceAuthorization()
        {
            var store = new Store
            {
                Capacity = 0
            };
            var journal = new SpawnAdmission(store);
            var request = Request();
            var facts = Ready();
            var actual = journal.Begin(request, facts, 1).Failure;
            var expression = Is.EqualTo(SpawnFailure.Capacity);
            Assert.That(actual, expression);
            var spawnRequest = new SpawnRequest("bad", 1, 100);
            var facts2 = Ready();
            var actual2 = journal.Begin(spawnRequest, facts2, 1).Failure;
            var expression2 = Is.EqualTo(SpawnFailure.InvalidRequest);
            Assert.That(actual2, expression2);
        }
    }
}
