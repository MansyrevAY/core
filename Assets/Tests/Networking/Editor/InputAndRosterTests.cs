using Core.Resources;
using System;
using Core.Networking.Core;
using NUnit.Framework;

namespace Core.Tests.Networking
{
    public sealed class InputAndRosterTests
    {
        [Test]
        public void T03_InputReplacementIsExclusiveAndOldDisposalCannotRemoveIt()
        {
            using var slot = new LeaseSlot<Action>();
            var firstCalls = 0;
            var secondCalls = 0;
            var old = slot.Register(() => firstCalls++);
            var current = slot.Register(() => secondCalls++);
            old.Dispose();
            for (var index = 0; index < 100; index++)
            {
                slot.Current();
            }

            Assert.That(firstCalls, Is.Zero);
            var expression = Is.EqualTo(100);
            Assert.That(secondCalls, expression);
            current.Dispose();
            Assert.That(slot.Current, Is.Null);
        }

        [Test]
        public void T03_InputScopesAreIsolatedAndDisposedScopeCannotRegister()
        {
            var first = new LeaseSlot<object>();
            using var second = new LeaseSlot<object>();
            var objectValue = new object();
            var old = first.Register(objectValue);
            var source = new object();
            using var current = second.Register(source);
            first.Dispose();
            old.Dispose();
            Assert.That(first.Current, Is.Null);
            var actual = second.Current;
            var expression = Is.SameAs(source);
            Assert.That(actual, expression);
            Assert.Throws<ObjectDisposedException>(() =>
                        {
                            var objectValue2 = new object();
                            first.Register(objectValue2);
                        });
        }

        [Test]
        public void T05_RepeatedAndReorderedRosterDoesNotReplayParticipants()
        {
            var initial = new[] { 2, 1, 2 };
            var previous = Array.Empty<int>();
            var first = new RosterDelta<int>(previous, initial);
            CollectionAssert.AreEquivalent(new[] { 1, 2 }, first.Joined);
            var repeated = new RosterDelta<int>(initial, new[] { 1, 2 });
            Assert.That(repeated.Joined, Is.Empty);
            Assert.That(repeated.Left, Is.Empty);
            var reconnect = new RosterDelta<int>(initial, new[] { 1, 3, 3 });
            CollectionAssert.AreEqual(new[] { 2 }, reconnect.Left);
            CollectionAssert.AreEqual(new[] { 3 }, reconnect.Joined);
        }

        [Test]
        public void T05_RosterDeltaCannotBeMutatedThroughItsInputsOrPublicView()
        {
            var previous = new[] { 1 };
            var current = new[] { 2 };
            var delta = new RosterDelta<int>(previous, current);
            previous[0] = 7;
            current[0] = 8;
            var actual = delta.Joined[0];
            var expression = Is.EqualTo(2);
            Assert.That(actual, expression);
            var actual2 = delta.Left[0];
            var expression2 = Is.EqualTo(1);
            Assert.That(actual2, expression2);
            Assert.Throws<NotSupportedException>(() =>
                            ((System.Collections.Generic.IList<int>)delta.Joined).Add(9));
        }
    }
}
