using Core.Resources;
using System;
using Core.Networking.Core;
using NUnit.Framework;

namespace Core.Tests.Networking
{
    public sealed class AvatarRegistryTests
    {
        [Test]
        public void T07_ObservedDisconnectDoesNotReportAConnectionRebind()
        {
            using var registry = new AvatarRegistry<int, int, object>(0);
            var id = 11;
            var player = 1;
            var objectValue = new object();
            var old = registry.Register(id, player, objectValue, 0, false);
            var changes = new System.Collections.Generic.List<AvatarChangeKind>();
            registry.Changed += (kind, _) => changes.Add(kind);
            using var current = registry.Rebind(old, 1, 100, true);
            old.Dispose();
            CollectionAssert.AreEqual(new[] { AvatarChangeKind.ObservedStateChanged }, changes);
            var actual = registry.TryGetByPlayer(1, out var record);
            Assert.That(actual, Is.True);
            Assert.That(record.IsDisconnected, Is.True);
            Assert.That(current.IsCurrent, Is.True);
        }

        [Test]
        public void T06_IdenticalIdsInDifferentScopesNeverResolveAcrossScopes()
        {
            using var first = new AvatarRegistry<int, int, object>(0);
            using var second = new AvatarRegistry<int, int, object>(0);
            var firstAvatar = new object();
            var secondAvatar = new object();
            using var old = first.Register(11, 1, firstAvatar, 100, false);
            using var current = second.Register(11, 1, secondAvatar, 100, false);
            first.Dispose();
            old.Dispose();
            var actual = first.TryGetById(11, out _);
            Assert.That(actual, Is.False);
            var actual2 = second.TryGetByPlayer(1, out var found);
            Assert.That(actual2, Is.True);
            var actual3 = found.Avatar;
            var expression = Is.SameAs(secondAvatar);
            Assert.That(actual3, expression);
            Assert.That(current.IsCurrent, Is.True);
        }

        [Test]
        public void T07_RebindRemovesOldPlayerBeforeEventAndInvalidatesOldLease()
        {
            using var registry = new AvatarRegistry<int, int, object>(0);
            var avatar = new object();
            var old = registry.Register(11, 1, avatar, 100, true);
            var events = 0;
            registry.Changed += (kind, record) =>
            {
                if (kind != AvatarChangeKind.Rebound)
                {
                    return;
                }

                events++;
                var actual = registry.TryGetByPlayer(1, out _);
                Assert.That(actual, Is.False);
                var actual2 = registry.TryGetByPlayer(2, out var found);
                Assert.That(actual2, Is.True);
                var expression = Is.SameAs(record);
                Assert.That(found, expression);
            };
            var current = registry.Rebind(old, 2, 100, false);
            old.Dispose();
            var expression2 = Is.EqualTo(1);
            Assert.That(events, expression2);
            Assert.That(old.IsCurrent, Is.False);
            var actual5 = registry.Count;
            var expression3 = Is.EqualTo(1);
            Assert.That(actual5, expression3);
            current.Dispose();
            var actual6 = registry.TryGetByPlayer(2, out _);
            Assert.That(actual6, Is.False);
            Assert.That(registry.Count, Is.Zero);
        }

        [Test]
        public void T07_FailedConflictingRebindLeavesBothOriginalBindingsIntact()
        {
            using var registry = new AvatarRegistry<int, int, object>(0);
            var id = 11;
            var player = 1;
            var objectValue = new object();
            using var firstRegistration = registry.Register(id, player, objectValue, 100, false);
            var id2 = 12;
            var player2 = 2;
            var objectValue2 = new object();
            using var secondRegistration = registry.Register(id2, player2, objectValue2, 200, false);
            Assert.Throws<InvalidOperationException>(() => registry.Rebind(firstRegistration, 2, 100, false));
            Assert.That(firstRegistration.IsCurrent && secondRegistration.IsCurrent, Is.True);
            var actual = registry.TryGetByPlayer(1, out var first);
            Assert.That(actual, Is.True);
            var actual2 = first.Id;
            var expression = Is.EqualTo(11);
            Assert.That(actual2, expression);
            var actual3 = registry.TryGetByPlayer(2, out var second);
            Assert.That(actual3, Is.True);
            var actual4 = second.Id;
            var expression2 = Is.EqualTo(12);
            Assert.That(actual4, expression2);
        }

        [Test]
        public void T08_LateRegistrationIsDiscoverableAndRepeatedRegistrationHasNoSideEffects()
        {
            using var registry = new AvatarRegistry<int, int, object>(0);
            var avatar = new object();
            var events = 0;
            registry.Changed += (_, _) => events++;
            var actual = registry.TryGetByPlayer(3, out _);
            Assert.That(actual, Is.False);
            var lease = registry.Register(15, 3, avatar, 0, false);
            var actual2 = registry.Register(15, 3, avatar, 0, false);
            var expression = Is.SameAs(lease);
            Assert.That(actual2, expression);
            var actual3 = registry.TryGetByPlayer(3, out var found);
            Assert.That(actual3, Is.True);
            var actual4 = found.Avatar;
            var expression2 = Is.SameAs(avatar);
            Assert.That(actual4, expression2);
            var expression3 = Is.EqualTo(1);
            Assert.That(events, expression3);
            Assert.Throws<InvalidOperationException>(() =>
                        {
                            var id = 15;
                            var player = 4;
                            var objectValue = new object();
                            registry.Register(id, player, objectValue, 0, false);
                        });
        }

        [Test]
        public void T08_UnknownPlayersAndTokensDoNotMergeAvatarsAndSnapshotsStayImmutable()
        {
            using var registry = new AvatarRegistry<int, int, object>(0);
            var id = 1;
            var player = 0;
            var objectValue = new object();
            var firstRegistration = registry.Register(id, player, objectValue, 0, false);
            var id2 = 2;
            var player2 = 0;
            var objectValue2 = new object();
            using var secondRegistration = registry.Register(id2, player2, objectValue2, 0, false);
            var snapshot = registry.Records;
            var actual = registry.TryGetByPlayer(0, out _);
            Assert.That(actual, Is.False);
            var actual2 = snapshot.Count;
            var expression = Is.EqualTo(2);
            Assert.That(actual2, expression);
            firstRegistration.Dispose();
            var actual3 = snapshot.Count;
            var expression2 = Is.EqualTo(2);
            Assert.That(actual3, expression2);
            var actual4 = registry.Count;
            var expression3 = Is.EqualTo(1);
            Assert.That(actual4, expression3);
            Assert.Throws<NotSupportedException>(() =>
                            ((System.Collections.Generic.IList<AvatarRegistry<int, int, object>.Record>)snapshot).Clear());
        }
    }
}
