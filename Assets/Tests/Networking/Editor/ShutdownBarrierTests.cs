using Core.Resources;
using System;
using System.Threading.Tasks;
using Core.Networking.Core;
using NUnit.Framework;

namespace Core.Tests.Networking {
    public sealed class ShutdownBarrierTests {
        [Test]
        public async Task T04_TimeoutCannotAuthorizeReplacementAndLateCompletionReleasesOnlyOnce()
        {
            var stopping = new TaskCompletionSource<bool>();
            var deadline = new TaskCompletionSource<bool>();
            var calls = 0;
            var releases = 0;
            var barrier = new ShutdownBarrier(() => { calls++; return stopping.Task; }, _ => deadline.Task, () => releases++);
            var first = barrier.StopAsync();
            var actual = barrier.StopAsync();
            var expression = Is.SameAs(first);
            Assert.That(actual, expression);
            deadline.SetResult(true);
            try { await first; Assert.Fail("Expected timeout"); } catch (TimeoutException) { }
            Assert.That(barrier.IsCleaned, Is.False);
            Assert.That(releases, Is.Zero);
            var expression2 = Is.EqualTo(1);
            Assert.That(calls, expression2);
            var actual3 = barrier.StopAsync();
            var expression3 = Is.SameAs(first);
            Assert.That(actual3, expression3);
            stopping.SetResult(true);
            await Task.Yield();
            Assert.That(barrier.IsCleaned, Is.True);
            var expression4 = Is.EqualTo(1);
            Assert.That(releases, expression4);
            var expression5 = Is.EqualTo(1);
            Assert.That(calls, expression5);
        }

        [Test]
        public async Task T04_ShutdownExceptionLeavesScopeQuarantined()
        {
            var releases = 0;
            var barrier = new ShutdownBarrier(() =>
            {
                var invalidOperationException = new InvalidOperationException("Injected SDK failure");
                return Task.FromException(invalidOperationException);
            },
                _ => new TaskCompletionSource<bool>().Task, () => releases++);
            try { await barrier.StopAsync(); Assert.Fail("Expected failure"); } catch (InvalidOperationException) { }
            Assert.That(barrier.IsCleaned, Is.False);
            Assert.That(releases, Is.Zero);
            var actual = barrier.Failure;
            var expression = Is.TypeOf<InvalidOperationException>();
            Assert.That(actual, expression);
        }

        [Test]
        public async Task T04_ReentrantReleaseSharesAlreadyInstalledCompletion()
        {
            ShutdownBarrier barrier = null;
            Task reentrant = null;
            var calls = 0;
            barrier = new ShutdownBarrier(() => { calls++; return Task.CompletedTask; },
                _ => new TaskCompletionSource<bool>().Task, () => reentrant = barrier.StopAsync());
            var first = barrier.StopAsync();
            await first;
            var expression = Is.SameAs(first);
            Assert.That(reentrant, expression);
            var expression2 = Is.EqualTo(1);
            Assert.That(calls, expression2);
            Assert.That(barrier.IsCleaned, Is.True);
        }

        [Test]
        public async Task T04_LocalReleaseFailureDoesNotClaimCleanupSuccess() {
            var barrier = new ShutdownBarrier(() => Task.CompletedTask,
                _ => new TaskCompletionSource<bool>().Task, () => throw new InvalidOperationException("Injected release failure"));
            try { await barrier.StopAsync(); Assert.Fail("Expected failure"); } catch (InvalidOperationException) { }
            Assert.That(barrier.IsCleaned, Is.False);
        }
    }
}
