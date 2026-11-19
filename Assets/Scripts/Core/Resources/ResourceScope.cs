using System;
using System.Collections.Generic;

namespace Core.Resources
{

    public sealed class ResourceScope : IDisposable
    {
        private readonly List<IDisposable> _resources = new();
        public int Count => _resources.Count;
        public bool IsDisposed { get; private set; }

        public T Own<T>(T resource) where T : IDisposable
        {
            if (resource == null)
            {
                throw new ArgumentNullException(nameof(resource));
            }

            if (IsDisposed)
            {
                resource.Dispose();
                throw new ObjectDisposedException(nameof(ResourceScope));
            }
            _resources.Add(resource);
            return resource;
        }

        public void Dispose()
        {
            if (IsDisposed)
            {
                return;
            }

            IsDisposed = true;
            List<Exception> failures = null;
            for (var index = _resources.Count - 1; index >= 0; index--)
            {
                try
                {
                    _resources[index].Dispose();
                }
                catch (Exception exception) { (failures ??= new()).Add(exception); }
            }
            _resources.Clear();
            if (failures != null)
            {
                throw new AggregateException(failures);
            }
        }
    }
}
