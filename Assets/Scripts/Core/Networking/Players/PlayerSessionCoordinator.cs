using System;
using System.Threading;
using Core.Networking.Core;
using Core.Networking.Fusion;
using Core.Resources;
using Cysharp.Threading.Tasks;
using Fusion;
using R3;
using UnityEngine;

namespace Core.Networking.Players
{
    public sealed class PlayerSessionCoordinator : IRunnerScopeInstaller, IDisposable
    {
        private readonly NetworkService _network;
        private readonly LocalPlayerIdentity _identity;
        private readonly SceneRef _scene;
        private readonly Func<PlayerAvatar, CancellationToken, UniTask<IDisposable>> _initialize;
        private readonly ReactiveProperty<PlayerSessionContext> _current = new(null);
        public ReadOnlyReactiveProperty<PlayerSessionContext> Current => _current;

        public PlayerSessionCoordinator(NetworkService network, LocalPlayerIdentity identity, SceneRef scene,
            Func<PlayerAvatar, CancellationToken, UniTask<IDisposable>> initialize)
        {
            _network = network;
            _identity = identity;
            _scene = scene;
            _initialize = initialize;
        }

        public IDisposable Install(NetworkRunner runner, RunnerGeneration generation)
        {
            var resources = new ResourceScope();
            var context = runner.gameObject.AddComponent<PlayerSessionContext>();
            var players = resources.Own(new PlayerService(runner, generation, _network));
            context.Configure(players, generation);
            resources.Own(context.Bootstrap);
            resources.Own(context.Spawning);
            _current.Value = context;
            resources.Own(Disposable.Create(() =>
            {
                if (ReferenceEquals(_current.CurrentValue, context))
                {
                    _current.Value = null;
                }
                if (context)
                {
                    UnityEngine.Object.Destroy(context);
                }
            }));
            return resources;
        }

        public async UniTask PrepareRoomAsync(NetworkRunner runner, RunnerGeneration generation,
            CreateSessionRequest creation, CancellationToken cancellationToken)
        {
            var context = _current.CurrentValue;
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            using var timer = timeout.CancelAfterSlim(TimeSpan.FromSeconds(45), DelayType.Realtime);
            await _network.SceneLoader.EnsureSceneRegisteredAsync(generation, _scene, timeout.Token);
            await UniTask.WaitUntil(() => context && context.Director, cancellationToken: timeout.Token);
            var result = await context.Spawning.EnsureSpawnAsync(context.Director, runner.LocalPlayer, _identity.Id, timeout.Token);
            if (!result.IsSuccess)
            {
                throw new InvalidOperationException($"Avatar spawn failed: {result.Failure}");
            }
            await UniTask.WaitUntil(() => context.Players.TryGet(runner.LocalPlayer, out _), cancellationToken: timeout.Token);
            context.Players.TryGet(runner.LocalPlayer, out var avatar);
            await context.Bootstrap.EnsureReadyAsync(avatar, _initialize, timeout.Token);
        }

        public void Dispose()
        {
            _current.Dispose();
        }

        public UniTask PrepareLeaveAsync(NetworkRunner runner, RunnerGeneration generation)
        {
            var context = _current.CurrentValue;
            if (context && context.Generation == generation)
            {
                context.Bootstrap.Dispose();
            }
            return UniTask.CompletedTask;
        }
    }
}
