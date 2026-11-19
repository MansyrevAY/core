using Core.Resources;
using System;
using System.Threading;
using Core.Networking.Core;
using Core.Infra.Lifetime;
using Cysharp.Threading.Tasks;
using Fusion;
using R3;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Core.Networking.Fusion
{
    public sealed class SceneLoadSnapshot
    {
        public OperationId OperationId { get; }
        public RunnerGeneration Generation { get; }
        public SceneRef Target { get; }
        public float Progress { get; }
        public SceneRegistrationStatus Status { get; }
        internal SceneLoadSnapshot(SceneRegistrationOperation<SceneRef> operation)
        {
            OperationId = operation.Id;
            Generation = operation.Generation;
            Target = operation.Target;
            Progress = operation.Progress;
            Status = operation.Status;
        }
    }

    public sealed class NetworkSceneLoader : IDisposable
    {
        public const float RegistrationTimeoutSeconds = 45f;
        private readonly NetworkRunner _runner;
        private readonly FusionSceneManager _manager;
        private readonly RunnerGeneration _generation;
        private readonly CancellationTokenSource _lifetime = new();
        private readonly ReactiveProperty<SceneLoadSnapshot> _state = new(null);
        private DisposableBag _subscriptions;
        private Attempt _attempt;
        private long _nextId;
        private bool _disposed;
        public ReadOnlyReactiveProperty<SceneLoadSnapshot> State => _state;

        private sealed class Attempt
        {
            public readonly SceneRegistrationOperation<SceneRef> Operation;
            public readonly UniTaskCompletionSource<SceneRegistration> Registered = new();
            public readonly UniTaskCompletionSource<SceneRegistration> Completion = new();
            public bool Running = true;
            public Attempt(SceneRegistrationOperation<SceneRef> operation) => Operation = operation;
        }

        internal NetworkSceneLoader(NetworkRunner runner, FusionSceneManager manager, RunnerGeneration generation)
        {
            _runner = runner;
            _manager = manager;
            _generation = generation;
            manager.OnLoadProgress.SafeSubscribe(progress =>
            {
                if (TryObserveSceneProgress(progress))
                {
                    Publish(_attempt);
                }
            }).AddTo(ref _subscriptions);
            manager.OnRegistered.SafeSubscribe(ObserveRegistration).AddTo(ref _subscriptions);
            manager.OnLoadFailed.SafeSubscribe(failure =>
            {
                if (IsCurrentSceneFailure(failure))
                {
                    _attempt.Registered.TrySetException(failure.Error);
                }
            }).AddTo(ref _subscriptions);
        }

        public bool TryGetRegistration(SceneRef sceneRef, out SceneRegistration registration)
        {
            registration = default;
            return Accept(_runner) && _manager.TryGetRegistration(sceneRef, out registration);
        }

        public UniTask<SceneRegistration> EnsureSceneRegisteredAsync(RunnerGeneration generation, SceneRef target,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (IsSceneRunnerRetired(generation))
            {
                var operationCanceledException = new OperationCanceledException("Scene runner retired");
                return UniTask.FromException<SceneRegistration>(operationCanceledException);
            }

            if (!target.IsValid)
            {
                var argumentException = new ArgumentException("Invalid scene");
                return UniTask.FromException<SceneRegistration>(argumentException);
            }

            if (_attempt?.Running == true)
            {
                if (_attempt.Operation.Target != target)
                {
                    var invalidOperationException = new InvalidOperationException("Another scene is pending");
                    return UniTask.FromException<SceneRegistration>(invalidOperationException);
                }

                return _attempt.Completion.Task;
            }
            var operationId = new OperationId(++_nextId);
            var sceneRegistrationOperation = new SceneRegistrationOperation<SceneRef>(operationId, generation, target);
            var attempt = new Attempt(sceneRegistrationOperation);
            _attempt = attempt;
            Publish(attempt);
            ExecuteAsync(attempt, cancellationToken).Forget(Debug.LogException);
            return attempt.Completion.Task;
        }

        private async UniTask ExecuteAsync(Attempt attempt, CancellationToken caller)
        {
            Exception failure = null;
            SceneRegistration result = default;
            using (var deadline = CancellationTokenSource.CreateLinkedTokenSource(caller, _lifetime.Token))
            {
                var delayTimeSpan = TimeSpan.FromSeconds(RegistrationTimeoutSeconds);
                using (var timer = deadline.CancelAfterSlim(delayTimeSpan, DelayType.Realtime))
                using (deadline.Token.Register(() => attempt.Registered.TrySetCanceled(deadline.Token)))
                {
                    try
                    {
                        deadline.Token.ThrowIfCancellationRequested();
                        if (_manager.TryGetRegistration(attempt.Operation.Target, out var registered))
                        {
                            ObserveRegistration(registered);
                        }
                        else if (ShouldScheduleScene(attempt))
                        {
                            _runner.LoadScene(attempt.Operation.Target, LoadSceneMode.Additive);
                        }

                        result = await attempt.Registered.Task;
                        deadline.Token.ThrowIfCancellationRequested();
                        if (IsRegistrationStale(result))
                        {
                            throw new OperationCanceledException("Registration is no longer current");
                        }

                        attempt.Operation.ObserveRegistration(_generation, result.SceneRef);
                    }
                    catch (OperationCanceledException error)
                    {
                        failure = !caller.IsCancellationRequested && !_lifetime.IsCancellationRequested ? new TimeoutException($"Scene {attempt.Operation.Target} registration exceeded {RegistrationTimeoutSeconds}s", error) : error;
                    }
                    catch (Exception error)
                    {
                        failure = error;
                    }
                }
            }
            attempt.Running = false;
            if (failure != null)
            {
                attempt.Operation.Stop(failure is not OperationCanceledException);
            }

            Publish(attempt);
            if (failure is OperationCanceledException cancelled)
            {
                attempt.Completion.TrySetCanceled(cancelled.CancellationToken);
            }
            else if (failure != null)
            {
                attempt.Completion.TrySetException(failure);
            }
            else
            {
                attempt.Completion.TrySetResult(result);
            }
        }

        private bool IsScheduled(SceneRef target)
        {
            if (!_runner.TryGetSceneInfo(out var info))
            {
                return false;
            }

            for (var index = 0; index < info.SceneCount; index++)
            {
                if (info.Scenes[index] == target)
                {
                    return true;
                }
            }

            return false;
        }

        private void ObserveRegistration(SceneRegistration registration)
        {
            if (IsExpectedRegistration(registration))
            {
                _attempt.Registered.TrySetResult(registration);
            }
        }

        private bool Accept(NetworkRunner runner) => !_disposed && ReferenceEquals(runner, _runner) &&
            _runner && _runner.IsRunning;

        private void Publish(Attempt attempt)
        {
            if (CanPublishAttempt(attempt))
            {
                _state.Value = new SceneLoadSnapshot(attempt.Operation);
            }
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            _lifetime.Cancel();
            _subscriptions.Clear();
            _state.Dispose();
            _lifetime.Dispose();
        }

        private bool TryObserveSceneProgress(SceneProgress progress)
        {
            return Accept(progress.Runner) && _attempt != null && _attempt.Operation.ObserveProgress(_generation, progress.SceneRef, progress.Progress);
        }

        private bool IsCurrentSceneFailure(SceneLoadFailure failure)
        {
            return Accept(failure.Runner) && _attempt?.Operation.IsWaiting == true && _attempt.Operation.Target == failure.SceneRef;
        }

        private bool IsSceneRunnerRetired(RunnerGeneration generation)
        {
            return !Accept(_runner) || generation != _generation;
        }

        private bool ShouldScheduleScene(Attempt attempt)
        {
            return _runner.IsSceneAuthority && !IsScheduled(attempt.Operation.Target);
        }

        private bool IsRegistrationStale(SceneRegistration result)
        {
            return !Accept(result.Runner) || !_manager.TryGetRegistration(result.SceneRef, out var current) || current.LoadId != result.LoadId;
        }

        private bool IsExpectedRegistration(SceneRegistration registration)
        {
            return Accept(registration.Runner) && _attempt?.Operation.IsWaiting == true && _attempt.Operation.Target == registration.SceneRef;
        }

        private bool CanPublishAttempt(Attempt attempt)
        {
            return !_disposed && ReferenceEquals(_attempt, attempt);
        }
    }
}
