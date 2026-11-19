using System;
using System.Threading;
using System.Threading.Tasks;

namespace Core.Networking.Core
{
    public sealed class NetworkOperationContext
    {
        private readonly CancellationTokenSource _cancellation;
        public OperationId Id { get; }
        public RunnerGeneration Generation { get; set; }
        public NetworkErrorStage Stage { get; set; } = NetworkErrorStage.Admission;
        public CancellationToken CancellationToken => _cancellation.Token;
        public LeaveMode LeaveMode { get; internal set; }
        internal bool CancellationCallbackFailed { get; private set; }

        internal NetworkOperationContext(OperationId id, RunnerGeneration generation,
            CancellationTokenSource cancellation)
        {
            Id = id;
            Generation = generation;
            _cancellation = cancellation;
        }

        internal void Cancel()
        {
            try
            {
                _cancellation.Cancel();
            }
            catch (AggregateException)
            {
                CancellationCallbackFailed = true;
            }
        }

        internal void Dispose() => _cancellation.Dispose();

        public OperationResult Complete(OperationStatus status, NetworkError error = null) =>
            new(status, Id, Generation, error);

        public OperationResult Fail(NetworkErrorCode code, string reason)
        {
            var status = OperationStatus.Failed;
            var networkError = new NetworkError(Stage, code, reason);
            return Complete(status, networkError);
        }
    }

    public sealed class NetworkOperationGate : IDisposable
    {
        private sealed class Entry
        {
            public readonly NetworkOperationContext Context;
            public readonly TaskCompletionSource<OperationResult> Completion = new();

            public Entry(NetworkOperationContext context)
            {
                Context = context;
            }
        }

        private readonly Func<RunnerGeneration> _generation;
        private long _nextId;
        private Entry _active;
        private Entry _leave;
        private bool _disposed;

        public bool IsBusy => _active != null;
        public bool IsLeaving => _leave != null;
        public OperationId ActiveOperationId => _active?.Context.Id ?? default;

        public NetworkOperationGate(Func<RunnerGeneration> generation)
        {
            _generation = generation ?? throw new ArgumentNullException(nameof(generation));
        }

        public Task<OperationResult> Run(Func<NetworkOperationContext, Task<OperationResult>> body,
            CancellationToken cancellationToken = default)
        {
            var id = new OperationId(checked(++_nextId));
            if (_disposed)
            {
                var result = Disposed(id);
                return Task.FromResult(result);
            }

            if (_active != null)
            {
                var status = OperationStatus.Busy;
                var generation = _generation();
                var operationResult = new OperationResult(status, id, generation);
                return Task.FromResult(operationResult);
            }
            var linkedTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            var entry = NewEntry(id, linkedTokenSource);
            _active = entry;
            _ = ExecuteAsync(entry, body, null);
            return entry.Completion.Task;
        }

        public Task<OperationResult> Leave(LeaveMode mode, Func<NetworkOperationContext, Task<OperationResult>> body,
            CancellationToken cancellationToken = default)
        {
            if (_disposed)
            {
                var operationId = new OperationId(checked(++_nextId));
                var result = Disposed(operationId);
                return Task.FromResult(result);
            }

            if (_leave != null)
            {
                if (ShouldForgetSession(mode, ref cancellationToken))
                {
                    _leave.Context.LeaveMode = mode;
                }

                return ObserveLeaveAsync(_leave, cancellationToken);
            }

            var id = new OperationId(checked(++_nextId));
            if (cancellationToken.IsCancellationRequested)
            {
                var status = OperationStatus.Cancelled;
                var generation = _generation();
                var operationResult = new OperationResult(status, id, generation);
                return Task.FromResult(operationResult);
            }

            var previous = _active;
            var cancellationTokenSource = new CancellationTokenSource();
            var entry = NewEntry(id, cancellationTokenSource);
            entry.Context.LeaveMode = mode;
            _leave = entry;
            _active = entry;
            previous?.Context.Cancel();
            _ = ExecuteAsync(entry, body, previous);
            return ObserveLeaveAsync(entry, cancellationToken);
        }

        private Entry NewEntry(OperationId id, CancellationTokenSource cancellation)
        {
            var generation = _generation();
            var networkOperationContext = new NetworkOperationContext(id, generation, cancellation);
            return new(networkOperationContext);
        }
        private OperationResult Disposed(OperationId id)
        {
            var status = OperationStatus.Rejected;
            var generation = _generation();
            var networkError = new NetworkError(NetworkErrorStage.Admission, NetworkErrorCode.Disposed, "Service disposed");
            return new(status, id, generation, networkError);
        }
        private async Task ExecuteAsync(Entry entry, Func<NetworkOperationContext, Task<OperationResult>> body,
            Entry previous)
        {
            var context = entry.Context;
            OperationResult result;
            try
            {
                if (previous != null)
                {
                    await previous.Completion.Task;
                }

                context.CancellationToken.ThrowIfCancellationRequested();
                result = await body(context);
            }
            catch (OperationCanceledException)
            {
                result = context.Complete(OperationStatus.Cancelled);
            }
            catch (TimeoutException)
            {
                result = context.Fail(NetworkErrorCode.Timeout, "Operation deadline exceeded");
            }
            catch (Exception exception)
            {
                result = context.Fail(NetworkErrorCode.Unexpected, exception.Message);
            }
            finally
            {
                if (ReferenceEquals(_active, entry))
                {
                    _active = null;
                }

                if (ReferenceEquals(_leave, entry))
                {
                    _leave = null;
                }

                context.Dispose();
            }

            if (context.CancellationCallbackFailed)
            {
                result = context.Fail(NetworkErrorCode.Unexpected,
                "Cancellation callback failed; operation cleanup observed");
            }

            entry.Completion.TrySetResult(result);
        }

        private static async Task<OperationResult> ObserveLeaveAsync(Entry entry, CancellationToken cancellationToken)
        {
            if (!cancellationToken.CanBeCanceled)
            {
                return await entry.Completion.Task;
            }

            var cancelled = new TaskCompletionSource<bool>();
            using (cancellationToken.Register(() => cancelled.TrySetResult(true)))
            {
                var completed = await Task.WhenAny(entry.Completion.Task, cancelled.Task);
                return completed == entry.Completion.Task
                    ? await entry.Completion.Task
                    : entry.Context.Complete(OperationStatus.Cancelled);
            }
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            _active?.Context.Cancel();
        }

        private bool ShouldForgetSession(LeaveMode mode, ref CancellationToken cancellationToken)
        {
            return mode == LeaveMode.ForgetSession && !cancellationToken.IsCancellationRequested;
        }
    }
}
