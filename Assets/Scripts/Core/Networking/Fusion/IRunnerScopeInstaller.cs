using Core.Resources;
using System;
using Core.Networking.Core;
using Fusion;

namespace Core.Networking.Fusion
{
    public interface IRunnerScopeInstaller
    {
        IDisposable Install(NetworkRunner runner, RunnerGeneration generation);
        Cysharp.Threading.Tasks.UniTask PrepareLeaveAsync(NetworkRunner runner, RunnerGeneration generation);

        Cysharp.Threading.Tasks.UniTask PrepareRoomAsync(NetworkRunner runner, RunnerGeneration generation,
            CreateSessionRequest creation, System.Threading.CancellationToken cancellationToken);
    }
}
