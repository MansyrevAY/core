using Core.Resources;
using System;
using System.Threading.Tasks;
using Core.Networking.Core;
using Cysharp.Threading.Tasks;
using Fusion;
using R3;
using UnityEngine;

namespace Core.Networking.Fusion
{
    internal sealed class FusionRunnerSession
    {
        private readonly GameObject _gameObject;
        private readonly INetworkRunnerCallbacks _callbacks;
        private bool _callbacksRegistered;
        private DisposableBag _resources = new();
        private readonly ShutdownBarrier _shutdown;

        public NetworkRunner Runner { get; }
        public FusionSceneManager SceneManager { get; }
        public NetworkSceneLoader Scenes { get; }
        public RunnerGeneration Generation { get; }
        public LeaseSlot<IPlayerInputSource> Input { get; } = new();
        public bool IsStopping => _shutdown.IsStarted;
        public bool IsCleaned => _shutdown.IsCleaned;
        public const int ShutdownTimeoutSeconds = 20;

        public FusionRunnerSession(NetworkService service, RunnerGeneration generation)
        {
            Generation = generation;
            _callbacks = service;
            _gameObject = new GameObject($"NetworkRunner G{generation}");
            UnityEngine.Object.DontDestroyOnLoad(_gameObject);
            Runner = _gameObject.AddComponent<NetworkRunner>();
            SceneManager = _gameObject.AddComponent<FusionSceneManager>();
            Scenes = new NetworkSceneLoader(Runner, SceneManager, generation);
            _gameObject.AddComponent<NetworkServiceLink>().SetService(service);
            _shutdown = new ShutdownBarrier(
                async () =>
                {
                    if (Runner)
                    {
                        await Runner.Shutdown(destroyGameObject: false);
                    }

                    if (SceneManager)
                    {
                        await SceneManager.UnloadOwnedScenesAsync();
                    }
                },
                deadlineToken =>
                {
                    var delayTimeSpan = TimeSpan.FromSeconds(ShutdownTimeoutSeconds);
                    return UniTask.Delay(delayTimeSpan, ignoreTimeScale: true, cancellationToken: deadlineToken).AsTask();
                },
                ReleaseAfterShutdown,
                exception =>
                {
                    var message = $"[NetworkService] G{Generation} late shutdown failed: {exception.GetType().Name}";
                    Debug.LogError(message);
                });
        }

        public void Own(IDisposable resource) => _resources.Add(resource);

        public void ActivateCallbacks()
        {
            if (_callbacksRegistered)
            {
                throw new InvalidOperationException("Callbacks already registered");
            }

            Runner.AddCallbacks(_callbacks);
            _callbacksRegistered = true;
        }

        public Task ShutdownAsync()
        {
            Scenes.Dispose();
            return _shutdown.StopAsync();
        }

        private void ReleaseAfterShutdown()
        {
            if (IsCleaned)
            {
                return;
            }

            if (ShouldRemoveRunnerCallbacks())
            {
                Runner.RemoveCallbacks(_callbacks);
            }

            Scenes.Dispose();
            Input.Dispose();
            _resources.Clear();
            if (_gameObject)
            {
                UnityEngine.Object.Destroy(_gameObject);
            }
        }

        public async UniTask StopOnOwnerDestroyedAsync()
        {
            try
            {
                await ShutdownAsync();
            }
            catch (Exception exception)
            {
                var message = $"[NetworkService] G{Generation} owner destroyed during shutdown: {exception.GetType().Name}";
                Debug.LogWarning(message);
            }
        }

        private bool ShouldRemoveRunnerCallbacks()
        {
            return Runner && _callbacksRegistered;
        }
    }
}
