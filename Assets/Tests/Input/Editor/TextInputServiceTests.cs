using System;
using Core.Input.TextInput;
using NUnit.Framework;

namespace Core.Tests.Input
{
    public sealed class TextInputServiceTests
    {
        private TextInputService _service;
        private readonly TextInputRequest _request = new("Nickname", "original", 32);

        [SetUp]
        public void SetUp() => _service = new TextInputService();

        [TearDown]
        public void TearDown() => _service.Dispose();

        [Test]
        public void NoAvailableProviderLeavesPhysicalKeyboardEditingAvailable()
        {
            var called = false;
            _service.Register(new Provider { Available = false });
            Assert.That(_service.Open(_request, _ => called = true), Is.Null);
            Assert.That(_service.IsOpen, Is.False);
            Assert.That(called, Is.False);
        }

        [Test]
        public void HighestAvailablePriorityOpensFirst()
        {
            var fallback = new Provider();
            var platform = new Provider();
            _service.Register(fallback, -100);
            _service.Register(new Provider { Available = false }, 200);
            _service.Register(platform, 100);
            _service.Open(_request, _ => { });
            Assert.That(platform.Opened, Is.EqualTo(1));
            Assert.That(fallback.Opened, Is.Zero);
        }

        [Test]
        public void DeclinedOpeningFallsBackToTheNextProvider()
        {
            var fallback = new Provider();
            _service.Register(fallback);
            _service.Register(new Provider { Decline = true }, 100);
            _service.Open(_request, _ => { });
            Assert.That(fallback.Opened, Is.EqualTo(1));
            Assert.That(_service.IsOpen, Is.True);
        }

        [Test]
        public void ProviderFailureIsReportedAndFallsBack()
        {
            var errors = 0;
            var fallback = new Provider();
            _service.ProviderFailed += _ => errors++;
            _service.Register(fallback);
            _service.Register(new Provider { Throw = true }, 100);
            _service.Open(_request, _ => { });
            Assert.That(errors, Is.EqualTo(1));
            Assert.That(fallback.Opened, Is.EqualTo(1));
        }

        [Test]
        public void SubmissionCompletesOnceAndClosesProvider()
        {
            var provider = new Provider();
            var count = 0;
            var text = "";
            _service.Register(provider);
            _service.Open(_request, result => { count++; text = result.Text; });
            provider.Complete(TextInputResult.Accept("Игрок 日本語"));
            provider.Complete(TextInputResult.Cancelled);
            Assert.That(count, Is.EqualTo(1));
            Assert.That(text, Is.EqualTo("Игрок 日本語"));
            Assert.That(provider.Closed, Is.EqualTo(1));
            Assert.That(_service.IsOpen, Is.False);
        }

        [Test]
        public void CancellingDoesNotSubmitDraftText()
        {
            var provider = new Provider();
            TextInputResult result = default;
            var count = 0;
            _service.Register(provider);
            var handle = _service.Open(_request, value => { result = value; count++; });
            _service.Cancel();
            handle.Dispose();
            Assert.That(result.Submitted, Is.False);
            Assert.That(count, Is.EqualTo(1));
            Assert.That(provider.Closed, Is.EqualTo(1));
        }

        [Test]
        public void SynchronousCompletionDisposesReturnedProviderLease()
        {
            var provider = new Provider { Synchronous = true };
            var count = 0;
            _service.Register(provider);
            _service.Open(_request, _ => count++);
            Assert.That(count, Is.EqualTo(1));
            Assert.That(provider.Closed, Is.EqualTo(1));
            Assert.That(_service.IsOpen, Is.False);
        }

        [Test]
        public void OldHandleCannotCloseReplacementSession()
        {
            var provider = new Provider();
            _service.Register(provider);
            var old = _service.Open(_request, _ => { });
            provider.Complete(TextInputResult.Accept("first"));
            _service.Open(_request, _ => { });
            old.Dispose();
            Assert.That(_service.IsOpen, Is.True);
            Assert.That(provider.Closed, Is.EqualTo(1));
        }

        [Test]
        public void RemovingActiveProviderCancelsItsSession()
        {
            var provider = new Provider();
            var count = 0;
            var registration = _service.Register(provider);
            _service.Open(_request, _ => count++);
            registration.Dispose();
            Assert.That(_service.IsOpen, Is.False);
            Assert.That(count, Is.EqualTo(1));
            Assert.That(provider.Closed, Is.EqualTo(1));
        }

        [Test]
        public void BusyServiceKeepsExistingSession()
        {
            var provider = new Provider();
            _service.Register(provider);
            _service.Open(_request, _ => { });
            Assert.That(_service.Open(_request, _ => Assert.Fail("Busy request completed")), Is.Null);
            Assert.That(provider.Opened, Is.EqualTo(1));
        }

        [Test]
        public void CharacterLimitDoesNotLeaveHalfOfASurrogatePair()
        {
            var provider = new Provider();
            var text = "";
            _service.Register(provider);
            _service.Open(new TextInputRequest("", "", 2), result => text = result.Text);
            provider.Complete(TextInputResult.Accept("a\U0001F600b"));
            Assert.That(text, Is.EqualTo("a"));
        }

        [Test]
        public void DisposeCancelsBeforeRejectingNewRequests()
        {
            var provider = new Provider();
            var count = 0;
            _service.Register(provider);
            _service.Open(_request, _ => count++);
            _service.Dispose();
            Assert.That(count, Is.EqualTo(1));
            Assert.That(provider.Closed, Is.EqualTo(1));
            Assert.Throws<ObjectDisposedException>(() => _service.Open(_request, _ => { }));
        }

        private sealed class Provider : ITextInputProvider
        {
            public bool Available = true;
            public bool Decline;
            public bool Throw;
            public bool Synchronous;
            public int Opened;
            public int Closed;
            public Action<TextInputResult> Complete;

            public bool IsAvailable(TextInputRequest request) => Available;

            public IDisposable Open(TextInputRequest request, Action<TextInputResult> completed)
            {
                Opened++;
                if (Throw) throw new InvalidOperationException("Platform keyboard failed");
                if (Decline) return null;
                Complete = completed;
                if (Synchronous) completed(TextInputResult.Accept("instant"));
                return new Lease(() => Closed++);
            }
        }

        private sealed class Lease : IDisposable
        {
            private Action _dispose;
            public Lease(Action dispose) => _dispose = dispose;
            public void Dispose()
            {
                var action = _dispose;
                _dispose = null;
                action?.Invoke();
            }
        }
    }
}
