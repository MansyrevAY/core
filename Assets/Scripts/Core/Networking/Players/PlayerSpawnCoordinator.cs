using System;
using System.Threading;
using System.Threading.Tasks;
using Core.Networking.Core;
using Cysharp.Threading.Tasks;
using Fusion;

namespace Core.Networking.Players
{
    public interface IPlayerSpawnPort
    {
        UniTask<SpawnAdmissionRecord> ExecuteSpawnAsync(SpawnRequest request, CancellationToken cancellationToken);
    }

    public sealed class PlayerSpawnCoordinator : IDisposable
    {
        private readonly CancellationTokenSource _lifetime = new();
        private TaskCompletionSource<SpawnAdmissionRecord> _attempt;
        private bool _disposed;
        public SpawnRequest CurrentRequest { get; private set; }
        public SpawnAdmissionRecord LastResult { get; private set; }

        public UniTask<SpawnAdmissionRecord> EnsureSpawnAsync(IPlayerSpawnPort port, PlayerRef player, long identity,
            CancellationToken cancellationToken)
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(PlayerSpawnCoordinator));
            }

            if (CanReuseSpawnAttempt())
            {
                return _attempt.Task.AsUniTask();
            }

            var completion = new TaskCompletionSource<SpawnAdmissionRecord>();
            _attempt = completion;
            var idText = Guid.NewGuid().ToString("N");
            CurrentRequest = new SpawnRequest(idText, player.RawEncoded, identity);
            RunAsync(port, CurrentRequest, completion, cancellationToken).Forget();
            return completion.Task.AsUniTask();
        }

        private async UniTask RunAsync(IPlayerSpawnPort port, SpawnRequest request,
            TaskCompletionSource<SpawnAdmissionRecord> completion, CancellationToken callerToken)
        {
            using var deadline = CancellationTokenSource.CreateLinkedTokenSource(_lifetime.Token, callerToken);
            var delay = TimeSpan.FromSeconds(30);
            deadline.CancelAfter(delay);
            SpawnAdmissionRecord result;
            try { result = await port.ExecuteSpawnAsync(request, deadline.Token); }
            catch (OperationCanceledException)
            {
                result = Failure(request, _lifetime.IsCancellationRequested || callerToken.IsCancellationRequested
                    ? SpawnFailure.Cancelled : SpawnFailure.Timeout);
            }
            catch (Exception exception)
            {
                UnityEngine.Debug.LogException(exception);
                result = Failure(request, SpawnFailure.InvalidBinding);
            }
            LastResult = result;
            completion.TrySetResult(result);
        }

        private static SpawnAdmissionRecord Failure(SpawnRequest request, SpawnFailure failure) =>
            new(request, SpawnRequestStage.Failed, SpawnOutcome.Failed, failure, 0, 0, false);

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            _lifetime.Cancel();
            _lifetime.Dispose();
        }

        private bool CanReuseSpawnAttempt()
        {
            return _attempt != null && (!_attempt.Task.IsCompleted || LastResult.IsSuccess);
        }
    }
}
