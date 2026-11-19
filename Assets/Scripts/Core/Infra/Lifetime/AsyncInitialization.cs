using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using R3;

namespace Core.Infra.Lifetime
{
    public sealed class AsyncInitialization : IDisposable
    {
        public DisposableBag Resources;
        private readonly UniTaskCompletionSource<IDisposable> _completion = new();
        private CancellationTokenSource _cancellation;
        private Action _onRelease;
        private bool _started;
        public bool IsRunning { get; private set; }
        public bool IsReleased { get; private set; }
        public bool IsReady => _started && !IsRunning && !IsReleased;
        public CancellationToken Token => _cancellation?.Token ?? CancellationToken.None;

        public UniTask<IDisposable> Run(Func<UniTask> initialize, CancellationToken token, Action onRelease)
        {
            if (_started)
            {
                return _completion.Task;
            }

            _started = true;
            IsRunning = true;
            _onRelease = onRelease;
            _cancellation = CancellationTokenSource.CreateLinkedTokenSource(token);
            Execute(initialize).Forget();
            return _completion.Task;
        }

        private async UniTask Execute(Func<UniTask> initialize)
        {
            Exception failure = null;
            try
            {
                Token.ThrowIfCancellationRequested();
                await initialize();
                Token.ThrowIfCancellationRequested();
                if (IsReleased)
                {
                    throw new OperationCanceledException();
                }
            }
            catch (Exception exception)
            {
                failure = exception;
                try
                {
                    Dispose();
                }
                catch (Exception cleanup)
                {
                    failure = new AggregateException(failure, cleanup);
                }
            }
            finally
            {
                IsRunning = false;
                if (IsReleased)
                {
                    try
                    {
                        Resources.Clear();
                    }
                    catch (Exception cleanup)
                    {
                        failure = failure == null ? cleanup : new AggregateException(failure, cleanup);
                    }
                    finally
                    {
                        _cancellation?.Dispose();
                    }
                }
            }

            if (failure is OperationCanceledException cancelled)
            {
                _completion.TrySetCanceled(cancelled.CancellationToken);
            }
            else if (failure != null)
            {
                _completion.TrySetException(failure);
            }
            else
            {
                _completion.TrySetResult(this);
            }
        }

        public void Dispose()
        {
            if (IsReleased)
            {
                return;
            }

            IsReleased = true;
            Exception failure = null;

            void Cleanup(Action release)
            {
                try
                {
                    release();
                }
                catch (Exception error)
                {
                    failure = failure == null ? error : new AggregateException(failure, error);
                }
            }

            Cleanup(() => _cancellation?.Cancel());
            Cleanup(() => Resources.Clear());
            Cleanup(() => _onRelease?.Invoke());
            if (!IsRunning)
            {
                Cleanup(() => _cancellation?.Dispose());
            }

            if (failure != null)
            {
                throw failure;
            }
        }
    }
}
