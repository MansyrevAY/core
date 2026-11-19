using System;
using Cysharp.Threading.Tasks;
using R3;
using UnityEngine;

namespace Core.Infra.Lifetime
{
    public class DisposableBehaviour : MonoBehaviour
    {
        protected DisposableBag Disposables;

        protected virtual void Awake()
        {
            Disposables = new DisposableBag();
            OnInit().Forget();
        }

        protected virtual UniTask OnInit() => UniTask.CompletedTask;

        protected virtual void OnDestroy()
        {
            Disposables.Clear();
        }
    }

    public class DisposableBehaviour<T> : MonoBehaviour
    {
        private AsyncInitialization _initialization = new();
        protected ref DisposableBag Disposables => ref _initialization.Resources;
        protected T ActiveModel { get; private set; }
        protected System.Threading.CancellationToken InitializationToken => _initialization.Token;
        public bool IsInitialized => _initialization.IsReady;

        public async UniTask<IDisposable> Init(T model, System.Threading.CancellationToken token = default)
        {
            using var linked =
                System.Threading.CancellationTokenSource.CreateLinkedTokenSource(token, destroyCancellationToken);
            linked.Token.ThrowIfCancellationRequested();
            if (ShouldRenewInitialization())
            {
                _initialization = new AsyncInitialization();
            }

            return await _initialization.Run(async () =>
            {
                ActiveModel = model;
                await OnInit();
                InitializationToken.ThrowIfCancellationRequested();
            }, linked.Token, OnDeinit);
        }

        protected virtual UniTask OnInit() => UniTask.CompletedTask;

        protected virtual void OnDeinit()
        {
        }

        protected virtual void OnDestroy() => _initialization.Dispose();

        private bool ShouldRenewInitialization()
        {
            return _initialization.IsReleased && !_initialization.IsRunning;
        }
    }
}
