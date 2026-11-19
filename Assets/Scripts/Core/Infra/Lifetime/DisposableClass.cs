using System;
using R3;

namespace Core.Infra.Lifetime
{
    public class DisposableClass : IDisposable
    {
        protected DisposableBag Disposables;

        public IDisposable Init()
        {
            Disposables = new DisposableBag();

            OnInit();

            return Disposables;
        }

        protected virtual void OnInit()
        {
        }

        protected virtual void OnDisposed()
        {
        }

        public void Dispose()
        {
            OnDisposed();
            Disposables.Clear();
        }
    }
}
