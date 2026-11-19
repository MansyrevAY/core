using System;
using System.Collections.Generic;

namespace Core.Input.TextInput
{
    public sealed class TextInputService : IDisposable
    {
        private readonly List<Registration> _providers = new();
        private Session _active;
        private bool _disposed;

        public event Action SessionChanged;
        public event Action<Exception> ProviderFailed;
        public bool IsOpen => _active != null;

        public IDisposable Register(ITextInputProvider provider, int priority = 0)
        {
            if (_disposed) throw new ObjectDisposedException(nameof(TextInputService));
            if (provider == null) throw new ArgumentNullException(nameof(provider));
            var registration = new Registration(this, provider, priority);
            var index = _providers.FindIndex(item => item.Priority <= priority);
            _providers.Insert(index < 0 ? _providers.Count : index, registration);
            return registration;
        }

        // A null return means unavailable/busy; the caller can keep ordinary physical-keyboard editing.
        public IDisposable Open(TextInputRequest request, Action<TextInputResult> completed)
        {
            if (_disposed) throw new ObjectDisposedException(nameof(TextInputService));
            if (completed == null) throw new ArgumentNullException(nameof(completed));
            if (IsOpen) return null;

            foreach (var registration in _providers.ToArray())
            {
                if (!_providers.Contains(registration)) continue;
                Session session = null;
                try
                {
                    if (!registration.Provider.IsAvailable(request)) continue;
                    session = new Session(this, registration, request, completed);
                    _active = session;
                    SessionChanged?.Invoke();
                    if (session.Finished) return session;
                    var lease = registration.Provider.Open(request, session.Complete);
                    session.Attach(lease);
                    if (lease != null || session.Finished) return session;
                }
                catch (Exception error)
                {
                    ProviderFailed?.Invoke(error);
                    if (session != null && session.Finished) return session;
                }

                session?.Abandon();
            }

            return null;
        }

        public void Cancel()
        {
            _active?.Dispose();
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            Cancel();
            _providers.Clear();
            SessionChanged = null;
            ProviderFailed = null;
        }

        private void Release(Session session)
        {
            if (!ReferenceEquals(_active, session)) return;
            _active = null;
            SessionChanged?.Invoke();
        }

        private sealed class Registration : IDisposable
        {
            private readonly TextInputService _owner;
            public readonly ITextInputProvider Provider;
            public readonly int Priority;

            public Registration(TextInputService owner, ITextInputProvider provider, int priority)
            {
                _owner = owner;
                Provider = provider;
                Priority = priority;
            }

            public void Dispose()
            {
                _owner._providers.Remove(this);
                if (_owner._active?.Registration == this) _owner.Cancel();
            }
        }

        private sealed class Session : IDisposable
        {
            private readonly TextInputService _owner;
            private readonly TextInputRequest _request;
            private readonly Action<TextInputResult> _completed;
            private IDisposable _lease;
            public readonly Registration Registration;
            public bool Finished { get; private set; }

            public Session(TextInputService owner, Registration registration, TextInputRequest request, Action<TextInputResult> completed)
            {
                _owner = owner;
                Registration = registration;
                _request = request;
                _completed = completed;
            }

            public void Attach(IDisposable lease)
            {
                if (Finished) lease?.Dispose();
                else _lease = lease;
            }

            public void Complete(TextInputResult result)
            {
                if (Finished) return;
                Abandon();
                if (result.Submitted && _request.CharacterLimit > 0 && result.Text.Length > _request.CharacterLimit)
                {
                    var length = _request.CharacterLimit;
                    if (char.IsHighSurrogate(result.Text[length - 1])) length--;
                    result = TextInputResult.Accept(result.Text.Substring(0, length));
                }

                _completed(result);
            }

            public void Abandon()
            {
                if (Finished) return;
                Finished = true;
                var lease = _lease;
                _lease = null;
                try
                {
                    lease?.Dispose();
                }
                catch (Exception error)
                {
                    _owner.ProviderFailed?.Invoke(error);
                }
                finally
                {
                    _owner.Release(this);
                }
            }

            public void Dispose() => Complete(TextInputResult.Cancelled);
        }
    }
}
