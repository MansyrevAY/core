using Core.Resources;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Fusion;
using R3;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Core.Networking.Fusion
{
    public readonly struct SceneProgress
    {
        public readonly NetworkRunner Runner;
        public readonly SceneRef SceneRef;
        public readonly float Progress;

        public SceneProgress(NetworkRunner runner, SceneRef sceneRef, float progress)
        {
            Runner = runner;
            SceneRef = sceneRef;
            Progress = progress;
        }
    }

    public readonly struct SceneRegistration
    {
        public readonly NetworkRunner Runner;
        public readonly SceneRef SceneRef;
        public readonly Scene Scene;
        public readonly string LoadId;

        public SceneRegistration(NetworkRunner runner, SceneRef sceneRef, Scene scene, string loadId)
        {
            Runner = runner;
            SceneRef = sceneRef;
            Scene = scene;
            LoadId = loadId;
        }
    }

    public readonly struct SceneLoadFailure
    {
        public readonly NetworkRunner Runner;
        public readonly SceneRef SceneRef;
        public readonly Exception Error;

        public SceneLoadFailure(NetworkRunner runner, SceneRef sceneRef, Exception error)
        {
            Runner = runner;
            SceneRef = sceneRef;
            Error = error;
        }
    }

    public class FusionSceneManager : NetworkSceneManagerDefault
    {
        private readonly Dictionary<SceneRef, SceneRegistration> _registrations = new();
        private readonly HashSet<Scene> _ownedScenes = new();

#if CORE_ADDRESSABLES
        public string[] AddressableScenePaths { get; set; } = Array.Empty<string>();

        protected override GetAddressableScenesResult GetAddressableScenes()
        {
            return System.Threading.Tasks.Task.FromResult(AddressableScenePaths);
        }
#endif

        internal async UniTask UnloadOwnedScenesAsync()
        {
            foreach (var scene in _ownedScenes.ToArray())
            {
                if (CanUnloadScene(scene))
                {
                    await SceneManager.UnloadSceneAsync(scene);
                }
            }

            _ownedScenes.Clear();
        }

        private readonly Subject<SceneProgress> _progress = new();
        private readonly Subject<SceneRegistration> _registered = new();
        private readonly Subject<SceneLoadFailure> _failed = new();
        public Observable<SceneProgress> OnLoadProgress => _progress;
        public Observable<SceneRegistration> OnRegistered => _registered;
        public Observable<SceneLoadFailure> OnLoadFailed => _failed;

        public bool TryGetRegistration(SceneRef sceneRef, out SceneRegistration registration)
        {
            if (TryGetCurrentRegistration(sceneRef, out registration))
            {
                return true;
            }

            registration = default;
            return false;
        }

        public override NetworkSceneAsyncOp LoadScene(SceneRef sceneRef, NetworkLoadSceneParameters parameters)
        {
            var runner = Runner;
            _registrations.Remove(sceneRef);
            var operation = base.LoadScene(sceneRef, parameters);
            operation.AddOnCompleted(completed =>
            {
                if (ShouldPublishSceneFailure(runner, completed))
                {
                    var sceneLoadFailure = new SceneLoadFailure(runner, sceneRef, completed.Error);
                    _failed.OnNext(sceneLoadFailure);
                }
            });
            return operation;
        }

        protected override IEnumerator OnSceneLoaded(SceneRef sceneRef, Scene scene,
            NetworkLoadSceneParameters sceneParams)
        {
            var runner = Runner;
            _ownedScenes.Add(scene);
            var registration = base.OnSceneLoaded(sceneRef, scene, sceneParams);
            try
            {
                while (registration.MoveNext())
                {
                    yield return registration.Current;
                }
            }
            finally
            {
                (registration as IDisposable)?.Dispose();
            }

            if (IsSceneRunnerUnavailable(runner))
            {
                yield break;
            }

            var actualScene = scene.IsValid() && scene.isLoaded ? scene : MultiPeerScene;
            var loadIdText = sceneParams.LoadId.ToString();
            var record = new SceneRegistration(runner, sceneRef, actualScene, loadIdText);
            _registrations[sceneRef] = record;
            _registered.OnNext(record);
        }

        public override NetworkSceneAsyncOp UnloadScene(SceneRef sceneRef)
        {
            _registrations.Remove(sceneRef);
            return base.UnloadScene(sceneRef);
        }

        public override void Shutdown()
        {
            _registrations.Clear();
            base.Shutdown();
        }

        protected override void OnLoadSceneProgress(SceneRef sceneRef, float progress)
        {
            var sceneProgress = new SceneProgress(Runner, sceneRef, progress);
            _progress.OnNext(sceneProgress);
            base.OnLoadSceneProgress(sceneRef, progress);
        }

        private void OnDestroy()
        {
            _progress.Dispose();
            _registered.Dispose();
            _failed.Dispose();
        }

        private bool CanUnloadScene(Scene scene)
        {
            return scene.IsValid() && scene.isLoaded;
        }

        private bool TryGetCurrentRegistration(SceneRef sceneRef, out SceneRegistration registration)
        {
            registration = default;
            return _registrations.TryGetValue(sceneRef, out registration) &&
                   ReferenceEquals(registration.Runner, Runner) && Runner && registration.Scene.IsValid() &&
                   registration.Scene.isLoaded;
        }

        private bool ShouldPublishSceneFailure(NetworkRunner runner, NetworkSceneAsyncOp completed)
        {
            return completed.Error != null && ReferenceEquals(Runner, runner) && runner;
        }

        private bool IsSceneRunnerUnavailable(NetworkRunner runner)
        {
            return !ReferenceEquals(Runner, runner) || !runner || !runner.IsRunning;
        }
    }
}
