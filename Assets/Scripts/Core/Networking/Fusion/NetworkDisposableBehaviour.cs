using Core.Resources;
using Core.Infra.Lifetime;
using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Fusion;
using R3;

namespace Core.Networking.Fusion
{
    public abstract class NetworkDisposableBehaviour : NetworkBehaviour
    {
        private AsyncInitialization _initialization = new();
        protected ref DisposableBag Disposables => ref _initialization.Resources;
        private DisposableBag _networkDisposables;
        private SharedAuthorityLease _sharedAuthorityLease;
        private CancellationTokenSource _lifetimeCancellation = new();
        protected CancellationToken InitializationToken => _initialization.Token;
        public bool IsSpawned { get; private set; }

        public override void Spawned()
        {
            base.Spawned();
            if (_lifetimeCancellation.IsCancellationRequested)
            {
                _lifetimeCancellation = new CancellationTokenSource();
            }

            IsSpawned = true;
            Init().Forget();
        }

        public async UniTask<IDisposable> Init(CancellationToken cancellationToken = default)
        {
            using var linked = CancellationTokenSource.CreateLinkedTokenSource(
                cancellationToken, destroyCancellationToken, _lifetimeCancellation.Token);
            await NetworkSpawnWait.Until(this, () => IsSpawned, linked.Token);
            if (ShouldRenewInitialization())
            {
                _initialization = new AsyncInitialization();
            }

            await _initialization.Run(async () =>
            {
                await OnInit();
                ThrowIfInitializationInvalid();
            }, linked.Token, OnDeinit);
            return Disposable.Empty;
        }

        protected void ThrowIfInitializationInvalid()
        {
            InitializationToken.ThrowIfCancellationRequested();
            if (IsInitializationLifetimeEnded())
            {
                throw new OperationCanceledException("Network lifetime ended during initialization");
            }
        }

        protected virtual UniTask OnInit() => UniTask.CompletedTask;

        protected virtual void OnDeinit()
        {
        }

        protected void StartSharedAuthorityLease(Action onAcquired = null, Action onLost = null,
            Func<bool> canClaim = null)
        {
            _sharedAuthorityLease ??= new SharedAuthorityLease(this, onAcquired, onLost, canClaim);
            _sharedAuthorityLease.Start(ref _networkDisposables);
        }

        protected void NotifySharedAuthorityChanged() => _sharedAuthorityLease?.NotifyFusionAuthorityChanged();

        public override void Despawned(NetworkRunner runner, bool hasState)
        {
            IsSpawned = false;
            OnDestroy();
        }

        protected virtual void OnDestroy()
        {
            IsSpawned = false;
            try
            {
                _lifetimeCancellation.Cancel();
            }
            finally
            {
                try
                {
                    _initialization.Dispose();
                }
                finally
                {
                    _sharedAuthorityLease?.Dispose();
                    _sharedAuthorityLease = null;
                    _networkDisposables.Clear();
                }
            }
        }

        private bool ShouldRenewInitialization()
        {
            return _initialization.IsReleased && !_initialization.IsRunning;
        }

        private bool IsInitializationLifetimeEnded()
        {
            return !IsSpawned || !this;
        }
    }

    public abstract class NetworkDisposableBehaviour<T> : NetworkBehaviour
    {
        protected DisposableBag NetworkDisposables;
        private AsyncInitialization _initialization = new();
        protected ref DisposableBag Disposables => ref _initialization.Resources;
        private SharedAuthorityLease _sharedAuthorityLease;
        private CancellationTokenSource _lifetimeCancellation = new();
        protected T ActiveModel { get; private set; }
        protected CancellationToken LifetimeToken => _lifetimeCancellation.Token;
        protected CancellationToken InitializationToken => _initialization.Token;
        public bool IsSpawned { get; private set; }
        public bool IsInitialized => _initialization.IsReady;

        public override void Spawned()
        {
            base.Spawned();
            if (_lifetimeCancellation.IsCancellationRequested)
            {
                _lifetimeCancellation = new CancellationTokenSource();
            }

            IsSpawned = true;
        }

        public async UniTask<IDisposable> Init(T model, CancellationToken cancellationToken = default)
        {
            using var linked = CancellationTokenSource.CreateLinkedTokenSource(
                cancellationToken, destroyCancellationToken, LifetimeToken);
            await NetworkSpawnWait.Until(this, () => IsSpawned, linked.Token);
            if (ShouldRenewInitialization())
            {
                _initialization = new AsyncInitialization();
            }

            return await _initialization.Run(async () =>
            {
                ActiveModel = model;
                await OnInit();
                ThrowIfInitializationInvalid();
            }, linked.Token, OnDeinit);
        }

        protected void ThrowIfInitializationInvalid()
        {
            InitializationToken.ThrowIfCancellationRequested();
            if (IsInitializationLifetimeEnded())
            {
                throw new OperationCanceledException("Network lifetime ended during initialization");
            }
        }

        protected virtual UniTask OnInit() => UniTask.CompletedTask;

        protected virtual void OnDeinit()
        {
        }

        public override void Despawned(NetworkRunner runner, bool hasState)
        {
            IsSpawned = false;
            try
            {
                _lifetimeCancellation.Cancel();
            }
            finally
            {
                try
                {
                    _initialization.Dispose();
                }
                finally
                {
                    _sharedAuthorityLease?.Dispose();
                    _sharedAuthorityLease = null;
                    NetworkDisposables.Clear();
                }
            }
        }

        protected void StartSharedAuthorityLease(Action onAcquired = null, Action onLost = null,
            Func<bool> canClaim = null)
        {
            _sharedAuthorityLease ??= new SharedAuthorityLease(this, onAcquired, onLost, canClaim);
            _sharedAuthorityLease.Start(ref NetworkDisposables);
        }

        protected void NotifySharedAuthorityChanged() => _sharedAuthorityLease?.NotifyFusionAuthorityChanged();

        private bool ShouldRenewInitialization()
        {
            return _initialization.IsReleased && !_initialization.IsRunning;
        }

        private bool IsInitializationLifetimeEnded()
        {
            return !IsSpawned || !this;
        }
    }

    internal static class NetworkSpawnWait
    {
        private static readonly TimeSpan WaitTimeout = TimeSpan.FromSeconds(30);

        public static async UniTask Until(NetworkBehaviour behaviour, Func<bool> isSpawned, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            if (isSpawned()) return;
            using var deadline = CancellationTokenSource.CreateLinkedTokenSource(token);
            using var timer = deadline.CancelAfterSlim(WaitTimeout, DelayType.Realtime);
            try
            {
                await UniTask.WaitUntil(isSpawned, cancellationToken: deadline.Token);
                token.ThrowIfCancellationRequested();
            }
            catch (OperationCanceledException) when (!token.IsCancellationRequested)
            {
                var message = $"[{behaviour.GetType().Name}] Spawned was not observed within 30 seconds";
                throw new TimeoutException(message);
            }
        }
    }
}
