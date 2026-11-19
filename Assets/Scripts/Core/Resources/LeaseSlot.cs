using System;

namespace Core.Resources
{
    public sealed class LeaseSlot<T> : IDisposable where T : class
    {
        private long _version;
        private bool _disposed;
        public T Current { get; private set; }

        public IDisposable Register(T value)
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(LeaseSlot<T>));
            }

            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            var version = checked(++_version);
            Current = value;
            return new Lease(this, version);
        }

        public void Dispose()
        {
            _disposed = true;
            Current = null;
        }

        private sealed class Lease : IDisposable
        {
            private LeaseSlot<T> _owner;
            private readonly long _version;
            public Lease(LeaseSlot<T> owner, long version) { _owner = owner; _version = version; }
            public void Dispose()
            {
                if (_owner == null)
                {
                    return;
                }

                if (_owner._version == _version)
                {
                    _owner.Current = null;
                }

                _owner = null;
            }
        }
    }
}
