using System;
using System.Threading;
using System.Threading.Tasks;

namespace Core.Networking.Core {
    public sealed class ShutdownBarrier {
        private readonly Func<Task> _stop;
        private readonly Func<CancellationToken, Task> _deadline;
        private readonly Action _release;
        private readonly Action<Exception> _lateFailure;
        private Task _completion;

        public bool IsStarted { get; private set; }
        public bool IsCleaned { get; private set; }
        public Exception Failure { get; private set; }

        public ShutdownBarrier(Func<Task> stop, Func<CancellationToken, Task> deadline, Action release, Action<Exception> lateFailure = null) {
            _stop = stop ?? throw new ArgumentNullException(nameof(stop));
            _deadline = deadline ?? throw new ArgumentNullException(nameof(deadline));
            _release = release ?? throw new ArgumentNullException(nameof(release));
            _lateFailure = lateFailure;
        }

        public Task StopAsync() {
            if (_completion != null)
            {
                return _completion;
            }

            var completion = new TaskCompletionSource<bool>();
            _completion = completion.Task;
            IsStarted = true;
            _ = RunAsync(completion);
            return _completion;
        }

        private async Task RunAsync(TaskCompletionSource<bool> completion) {
            try {
                var stopping = _stop();
                using var timer = new CancellationTokenSource();
                var deadline = _deadline(timer.Token);
                var finished = await Task.WhenAny(stopping, deadline);
                if (HasShutdownDeadlineElapsed(stopping, finished))
                {
                    Failure = new TimeoutException("Runner shutdown deadline exceeded");
                    completion.TrySetException(Failure);
                    _ = FinishLateAsync(stopping);
                    return;
                }
                timer.Cancel();
                await stopping;
                Release();
                completion.TrySetResult(true);
            }
            catch (Exception exception) { Failure = exception; completion.TrySetException(exception); }
        }

        private async Task FinishLateAsync(Task stopping) {
            try { await stopping; Release(); }
            catch (Exception exception)
            {
                Failure = exception;
                try
                {
                    _lateFailure?.Invoke(exception);
                }
                catch (Exception) { }
            }
        }

        private void Release() {
            if (IsCleaned)
            {
                return;
            }

            _release();
            IsCleaned = true;
        }

        private bool HasShutdownDeadlineElapsed(Task stopping, Task finished)
        {
            return finished != stopping && !stopping.IsCompleted;
        }
    }
}
