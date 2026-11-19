using Core.Resources;
using System;
using System.Collections.Generic;
using Core.Networking.Core;
using NUnit.Framework;

namespace Core.Tests.Networking
{
    public sealed class ResourceScopeTests
    {
        private sealed class Resource : IDisposable
        {
            private readonly Action _dispose;
            public Resource(Action dispose) => _dispose = dispose;
            public void Dispose() => _dispose();
        }

        [Test]
        public void T15_PartialAttemptReleasesCompleteObjectsInReverseOrderExactlyOnce()
        {
            var released = new List<string>();
            var scope = new ResourceScope();
            var resource = new Resource(() => released.Add("hud"));
            scope.Own(resource);
            var resource2 = new Resource(() => released.Add("controller"));
            scope.Own(resource2);
            scope.Dispose();
            scope.Dispose();
            CollectionAssert.AreEqual(new[] { "controller", "hud" }, released);
            Assert.That(scope.Count, Is.Zero);
        }

        [Test]
        public void T15_CleanupFailureDoesNotLeakOtherAttemptResources()
        {
            var released = 0;
            var scope = new ResourceScope();
            var resource = new Resource(() => released++);
            scope.Own(resource);
            var resource2 = new Resource(() => throw new InvalidOperationException("injected cleanup failure"));
            scope.Own(resource2);
            var resource3 = new Resource(() => released++);
            scope.Own(resource3);
            Assert.Throws<AggregateException>(() => scope.Dispose());
            var expression = Is.EqualTo(2);
            Assert.That(released, expression);
            Assert.That(scope.Count, Is.Zero);
            Assert.DoesNotThrow(() => scope.Dispose());
        }

        [Test]
        public void T16_LateResourceIsReleasedAndCannotReviveCancelledAttempt()
        {
            var released = 0;
            var scope = new ResourceScope();
            scope.Dispose();
            Assert.Throws<ObjectDisposedException>(() =>
            {
                var resource = new Resource(() => released++);
                scope.Own(resource);
            });
            var expression = Is.EqualTo(1);
            Assert.That(released, expression);
            Assert.That(scope.Count, Is.Zero);
        }

        [Test]
        public void T15_RetryOwnsAnIndependentSetAfterRollback()
        {
            var live = 0;
            var first = new ResourceScope();
            live++;
            var resource = new Resource(() => live--);
            first.Own(resource);
            first.Dispose();
            using var retry = new ResourceScope();
            live++;
            var resource2 = new Resource(() => live--);
            retry.Own(resource2);
            first.Dispose();
            var expression = Is.EqualTo(1);
            Assert.That(live, expression);
            var actual2 = retry.Count;
            var expression2 = Is.EqualTo(1);
            Assert.That(actual2, expression2);
        }
    }
}
