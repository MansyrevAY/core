using Core.Resources;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Core.Networking.Core;
using Core.Infra.Lifetime;
using Cysharp.Threading.Tasks;
using Fusion;
using Fusion.Photon.Realtime;
using R3;

namespace Core.Networking.Fusion
{
    public partial class NetworkService
    {
        private readonly RunnerEpoch<NetworkRunner> _epochs = new();
        private IRunnerScopeInstaller _scopeInstaller;

        public void Configure(IRunnerScopeInstaller installer, LobbyDirectory directory, ReconnectStore reconnect,
            LocalPlayerIdentity identity)
        {
            if (IsConfigurationBlocked())
            {
                throw new InvalidOperationException("Scope composition must precede network initialization");
            }

            _scopeInstaller = installer ?? throw new ArgumentNullException(nameof(installer));
            Directory = directory ?? throw new ArgumentNullException(nameof(directory));
            Reconnect = reconnect ?? throw new ArgumentNullException(nameof(reconnect));
            _identity = identity ?? throw new ArgumentNullException(nameof(identity));
        }

        private readonly ReactiveProperty<NetworkSnapshot> _snapshot = CreateInitialSnapshot();

        private static ReactiveProperty<NetworkSnapshot> CreateInitialSnapshot()
        {
            var snapshot = new NetworkSnapshot(default, ConnectionPhase.Disconnected);
            return new ReactiveProperty<NetworkSnapshot>(snapshot);
        }

        private FusionRunnerSession _session;
        private NetworkOperationGate _operations;
        private bool _disposed;
        private CancellationToken _applicationExit;

        private NetworkOperationGate Operations => _operations ??= new NetworkOperationGate(() => _epochs.Generation);
        public ReadOnlyReactiveProperty<NetworkSnapshot> Snapshot => _snapshot;
        public RunnerGeneration Generation => _epochs.Generation;

        protected override UniTask OnInit()
        {
            _applicationExit = UnityEngine.Application.exitCancellationToken;
            Reconnect.Restore();
            return UniTask.CompletedTask;
        }

        public async UniTask<OperationResult> ConnectToLobbyAsync(CancellationToken cancellationToken = default)
        {
            await UniTask.SwitchToMainThread();
            return await Operations.Run(context => ConnectLobbyCoreAsync(context, false).AsTask(), cancellationToken)
                .AsUniTask();
        }

        public async UniTask<OperationResult> CreateSessionAsync(CreateSessionRequest request,
            CancellationToken cancellationToken = default)
        {
            await UniTask.SwitchToMainThread();
            return await Operations.Run(context => JoinCoreAsync(context, request, null, false).AsTask(),
                    cancellationToken)
                .AsUniTask();
        }

        public async UniTask<OperationResult> JoinSessionAsync(SessionLocator locator,
            CancellationToken cancellationToken = default)
        {
            await UniTask.SwitchToMainThread();
            return await Operations.Run(context => JoinCoreAsync(context, null, locator, false).AsTask(),
                    cancellationToken)
                .AsUniTask();
        }

        public async UniTask<OperationResult> JoinRandomSessionAsync(CancellationToken cancellationToken = default)
        {
            await UniTask.SwitchToMainThread();
            return await Operations.Run(context => JoinCoreAsync(context, null, null, true).AsTask(), cancellationToken)
                .AsUniTask();
        }

        public async UniTask<OperationResult> RefreshLobbyAsync(CancellationToken cancellationToken = default)
        {
            await UniTask.SwitchToMainThread();
            return await Operations.Run(context => ConnectLobbyCoreAsync(context, true).AsTask(), cancellationToken)
                .AsUniTask();
        }

        public async UniTask<OperationResult> LeaveSessionAsync(LeaveMode mode,
            CancellationToken cancellationToken = default)
        {
            await UniTask.SwitchToMainThread();
            return await Operations.Leave(mode, context => LeaveCoreAsync(context).AsTask(), cancellationToken)
                .AsUniTask();
        }

        private OperationResult Reject(NetworkOperationContext context,
            NetworkErrorCode code = NetworkErrorCode.InvalidState)
        {
            var status = OperationStatus.Rejected;
            var networkError = new NetworkError(NetworkErrorStage.Admission, code,
                "Command is not valid in the current state");
            return context.Complete(status, networkError);
        }

        private async UniTask<OperationResult> ConnectLobbyCoreAsync(NetworkOperationContext context, bool refresh)
        {
            var phase = _snapshot.CurrentValue.Phase;
            if (IsLobbyConnectionCurrent(refresh, phase))
            {
                return context.Complete(OperationStatus.Success);
            }

            if (IsLobbyConnectionRejected(refresh, phase))
            {
                return Reject(context);
            }

            if (IsPreviousRunnerCleanupPending())
            {
                return context.Fail(NetworkErrorCode.ShutdownFailed, "Previous runner cleanup is not confirmed");
            }

            return await WithCleanupAsync(context, async () =>
            {
                PublishPhase(ConnectionPhase.ConnectingLobby);
                await StopCurrentRunnerAsync(context);
                context.CancellationToken.ThrowIfCancellationRequested();
                context.Stage = NetworkErrorStage.Regions;
                await Directory.FetchRegionsAsync(context.CancellationToken);
                context.CancellationToken.ThrowIfCancellationRequested();
                var scope = CreateRunner(context);
                context.Stage = NetworkErrorStage.Lobby;
                var settings = PhotonAppSettings.Global.AppSettings.GetCopy();
                settings.FixedRegion = string.Empty;
                var result = await scope.Runner.JoinSessionLobby(SessionLobby.Shared, customAppSettings: settings,
                    cancellationToken: context.CancellationToken).AsUniTask();
                context.CancellationToken.ThrowIfCancellationRequested();
                EnsureCurrent(scope);
                if (!result.Ok)
                {
                    var code = NetworkErrorCode.Transport;
                    var reasonText = result.ShutdownReason.ToString();
                    return context.Fail(code, reasonText);
                }

                _connectedRegion = scope.Runner.LobbyInfo.Region;
                PublishPhase(ConnectionPhase.Lobby);
                context.CancellationToken.ThrowIfCancellationRequested();
                return context.Complete(OperationStatus.Success);
            });
        }

        private async UniTask<OperationResult> JoinCoreAsync(NetworkOperationContext context,
            CreateSessionRequest create, SessionLocator locator, bool random)
        {
            var phase = _snapshot.CurrentValue.Phase;
            if (IsRoomJoinRejected(phase))
            {
                return Reject(context);
            }

            if (IsSessionNameMissing(create, locator, random))
            {
                return Reject(context, NetworkErrorCode.InvalidRequest);
            }

            return await WithCleanupAsync(context, async () =>
            {
                var region = !string.IsNullOrWhiteSpace(locator?.Region) ? locator.Region
                    : !string.IsNullOrWhiteSpace(_connectedRegion) ? _connectedRegion : Directory.BestRegion;
                PublishPhase(ConnectionPhase.JoiningRoom);
                await StopCurrentRunnerAsync(context);
                context.CancellationToken.ThrowIfCancellationRequested();
                var scope = CreateRunner(context);
                var settings = PhotonAppSettings.Global.AppSettings.GetCopy();
                settings.FixedRegion = region ?? string.Empty;
                settings.emptyRoomTtl = EmptyRoomTtlMilliseconds;
                var isSolo = create?.IsSolo == true;
                var sceneInfo = new NetworkSceneInfo();
                if (InitialScene.HasValue)
                {
                    sceneInfo.AddSceneRef(InitialScene.Value, UnityEngine.SceneManagement.LoadSceneMode.Additive);
                }

                var arguments = new StartGameArgs
                {
                    GameMode = isSolo ? GameMode.Single : GameMode.Shared,
                    SessionName = create?.Name ?? locator?.Name ?? string.Empty,
                    EnableClientSessionCreation = create != null,
                    SceneManager = scope.SceneManager,
                    Scene = sceneInfo,
                    ConnectionToken = _identity.GetConnectionToken(),
                    DisableNATPunchthrough = create == null,
                    CustomPhotonAppSettings = settings,
                    StartGameCancellationToken = context.CancellationToken
                };
                if (create != null && !isSolo)
                {
                    arguments.SessionProperties = new Dictionary<string, SessionProperty>
                    {
                        { "region", region ?? string.Empty },
                        { LobbyDirectory.ConfirmedPlayersPropertyKey, 0 }
                    };
                    arguments.PlayerCount = create.PlayerCount;
                    arguments.IsVisible = create.IsVisible;
                }

                context.Stage = NetworkErrorStage.Join;
                var result = await scope.Runner.StartGame(arguments).AsUniTask();
                context.CancellationToken.ThrowIfCancellationRequested();
                EnsureCurrent(scope);
                if (!result.Ok)
                {
                    var code = NetworkErrorCode.Transport;
                    var reasonText = result.ShutdownReason.ToString();
                    return context.Fail(code, reasonText);
                }

                var actual = isSolo
                    ? new SessionLocator(create.Name, string.Empty)
                    : new SessionLocator(scope.Runner.SessionInfo.Name, scope.Runner.SessionInfo.Region);
                PublishRoster();
                context.Stage = NetworkErrorStage.Admission;
                try
                {
                    await _scopeInstaller.PrepareRoomAsync(scope.Runner, scope.Generation, create,
                        context.CancellationToken);
                }
                catch (TimeoutException error)
                {
                    return context.Fail(NetworkErrorCode.Timeout, error.Message);
                }

                context.CancellationToken.ThrowIfCancellationRequested();
                EnsureCurrent(scope);

                _connectedRegion = actual.Region;
                var generation = scope.Generation;
                var phase2 = ConnectionPhase.InRoom;
                var localPlayer = scope.Runner.LocalPlayer;
                var isOwner = scope.Runner.IsLocalPlayerSessionOwner();
                var networkSnapshot = new NetworkSnapshot(generation, phase2, actual, localPlayer, isOwner,
                    scope.Runner.ActivePlayers);
                Publish(networkSnapshot, true);
                context.CancellationToken.ThrowIfCancellationRequested();
                Reconnect.Connected(actual, create != null ? create.SaveSession && !create.IsSolo : !random);
                return context.Complete(OperationStatus.Success);
            });
        }

        private async UniTask<OperationResult> LeaveCoreAsync(NetworkOperationContext context)
        {
            context.Generation = _epochs.Generation;
            Reconnect.ApplyLeave(context.LeaveMode);
            if (_snapshot.CurrentValue.Phase == ConnectionPhase.Lobby)
            {
                return context.Complete(OperationStatus.Success);
            }

            if (IsPreviousRunnerCleanupPending())
            {
                return context.Fail(NetworkErrorCode.ShutdownFailed, "Previous runner cleanup is not confirmed");
            }

            var result = await WithCleanupAsync(context, async () =>
            {
                if (_scopeInstaller != null)
                {
                    await _scopeInstaller.PrepareLeaveAsync(_runner, Generation);
                }

                PublishPhase(ConnectionPhase.Disconnecting);
                await StopCurrentRunnerAsync(context);
                context.CancellationToken.ThrowIfCancellationRequested();
                Reconnect.ApplyLeave(context.LeaveMode);
                Directory.Reset(Generation);
                PublishPhase(ConnectionPhase.Disconnected);
                return context.Complete(OperationStatus.Success);
            });
            if (!_disposed)
            {
                Reconnect.ApplyLeave(context.LeaveMode);
            }

            return result;
        }

        private async UniTask<OperationResult> WithCleanupAsync(NetworkOperationContext context,
            Func<UniTask<OperationResult>> action)
        {
            OperationResult result;
            try
            {
                result = await action();
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
                UnityEngine.Debug.LogException(exception);
                result = context.Fail(NetworkErrorCode.Unexpected, exception.Message);
            }

            if (result.IsSuccess)
            {
                return result;
            }

            try
            {
                await StopCurrentRunnerAsync(context);
                PublishPhase(ConnectionPhase.Disconnected);
            }
            catch (Exception exception)
            {
                result = context.Fail(
                    exception is TimeoutException ? NetworkErrorCode.Timeout : NetworkErrorCode.ShutdownFailed,
                    exception.Message);
                PublishPhase(ConnectionPhase.Faulted);
            }

            if (ShouldPublishConnectionError(result))
            {
                var value = new NetworkOperationException(result).Message;
                _onConnectionError.OnNext(value);
            }

            return result;
        }

        private FusionRunnerSession CreateRunner(NetworkOperationContext context)
        {
            context.CancellationToken.ThrowIfCancellationRequested();
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(NetworkService));
            }

            if (_session != null)
            {
                throw new InvalidOperationException("Runner cleanup required before creation");
            }
            var runnerGeneration = new RunnerGeneration(checked(_epochs.Generation.Value + 1));
            var scope = new FusionRunnerSession(this, runnerGeneration);
            _session = scope;
            context.Generation = _epochs.Activate(scope.Runner);
            if (_scopeInstaller == null)
            {
                throw new InvalidOperationException("Runner scope installer is missing");
            }
            var resource = _scopeInstaller.Install(scope.Runner, scope.Generation);
            scope.Own(resource);
            scope.ActivateCallbacks();
            Directory.Reset(scope.Generation);
            PublishPhase(_snapshot.CurrentValue.Phase);
            return scope;
        }

        private async UniTask StopCurrentRunnerAsync(NetworkOperationContext context)
        {
            var scope = _session;
            if (scope == null)
            {
                return;
            }

            context.Generation = scope.Generation;
            context.Stage = NetworkErrorStage.Shutdown;
            _epochs.Retire(scope.Runner, scope.Generation);
            if (!scope.IsCleaned)
            {
                await scope.ShutdownAsync().AsUniTask();
            }

            if (!scope.IsCleaned)
            {
                throw new InvalidOperationException("Runner cleanup not confirmed");
            }

            if (ReferenceEquals(_session, scope))
            {
                _session = null;
            }

            await UniTask.NextFrame();
        }

        private bool AcceptCallback(NetworkRunner runner) => !_disposed && !_applicationExit.IsCancellationRequested
                                                                        && _session != null && !_session.IsStopping
                                                                        && _epochs.IsCurrent(runner,
                                                                            _session.Generation);

        private void EnsureCurrent(FusionRunnerSession scope)
        {
            if (!AcceptCallback(scope.Runner))
            {
                throw new OperationCanceledException("Runner generation retired");
            }
        }

        private void PublishPhase(ConnectionPhase phase)
        {
            var networkSnapshot = new NetworkSnapshot(_epochs.Generation, phase);
            Publish(networkSnapshot);
        }

        private void PublishRoster()
        {
            if (!AcceptCallback(_runner))
            {
                return;
            }

            var current = _snapshot.CurrentValue;
            if (IsRosterPublicationBlocked(current))
            {
                return;
            }

            var generation = _session.Generation;
            var phase = current.Phase;
            var session = current.Session;
            var localPlayer = _runner.LocalPlayer;
            var isOwner = _runner.IsLocalPlayerSessionOwner();
            var networkSnapshot =
                new NetworkSnapshot(generation, phase, session, localPlayer, isOwner, _runner.ActivePlayers);
            Publish(networkSnapshot, true);
        }

        private void Publish(NetworkSnapshot snapshot, bool publishParticipants = false)
        {
            if (_disposed)
            {
                return;
            }

            var previous = _snapshot.CurrentValue;
            var operationId = Operations.ActiveOperationId;
            _snapshot.Value = snapshot;
            if (!ReferenceEquals(_snapshot.CurrentValue, snapshot))
            {
                return;
            }

            _roomPlayers.Value = snapshot.Peers.ToArray();
            if (!ReferenceEquals(_snapshot.CurrentValue, snapshot))
            {
                return;
            }

            _isSessionOwner.Value = snapshot.IsOwner;
            if (!ReferenceEquals(_snapshot.CurrentValue, snapshot))
            {
                return;
            }

            _connectionState.Value = snapshot.Phase switch
            {
                ConnectionPhase.ConnectingLobby or ConnectionPhase.Disconnecting => global::Core.Networking.Fusion.ConnectionState.Connecting,
                ConnectionPhase.Lobby => global::Core.Networking.Fusion.ConnectionState.Lobby,
                ConnectionPhase.JoiningRoom => global::Core.Networking.Fusion.ConnectionState.Joining,
                ConnectionPhase.InRoom => global::Core.Networking.Fusion.ConnectionState.InRoom,
                _ => global::Core.Networking.Fusion.ConnectionState.Disconnected
            };
            if (!ReferenceEquals(_snapshot.CurrentValue, snapshot))
            {
                return;
            }

            if (HasConnectionPhaseChanged(snapshot, previous))
            {
                Emit(NetworkEventKind.PhaseChanged);
            }

            if (previous.IsOwner != snapshot.IsOwner)
            {
                Emit(NetworkEventKind.OwnerChanged);
            }

            if (publishParticipants)
            {
                IEnumerable<PlayerRef> previous2 = previous.Generation == snapshot.Generation
                    ? previous.Peers
                    : Array.Empty<PlayerRef>();
                var delta = new RosterDelta<PlayerRef>(previous2, snapshot.Peers);
                foreach (var player in delta.Left)
                {
                    Emit(NetworkEventKind.ParticipantLeft, player);
                }

                foreach (var player in delta.Joined)
                {
                    Emit(NetworkEventKind.ParticipantJoined, player);
                }
            }

            void Emit(NetworkEventKind kind, PlayerRef player = default)
            {
                if (!ReferenceEquals(_snapshot.CurrentValue, snapshot))
                {
                    return;
                }
                var networkEvent = new NetworkEvent(kind, snapshot, operationId, player);
                _events.OnNext(networkEvent);
            }
        }

        private bool IsConfigurationBlocked()
        {
            return _session != null || Operations.IsBusy || _disposed;
        }

        private bool IsLobbyConnectionCurrent(bool refresh, ConnectionPhase phase)
        {
            return !refresh && phase == ConnectionPhase.Lobby;
        }

        private bool IsLobbyConnectionRejected(bool refresh, ConnectionPhase phase)
        {
            return refresh
                ? phase != ConnectionPhase.Lobby
                : phase != ConnectionPhase.Disconnected && phase != ConnectionPhase.Faulted;
        }

        private bool IsPreviousRunnerCleanupPending()
        {
            return _session != null && _session.IsStopping && !_session.IsCleaned;
        }

        private bool IsRoomJoinRejected(ConnectionPhase phase)
        {
            return phase != ConnectionPhase.Lobby && phase != ConnectionPhase.Disconnected;
        }

        private bool IsSessionNameMissing(CreateSessionRequest create, SessionLocator locator, bool random)
        {
            return !random && string.IsNullOrWhiteSpace(create?.Name ?? locator?.Name);
        }

        private bool ShouldPublishConnectionError(OperationResult result)
        {
            return !_disposed && result.Status == OperationStatus.Failed;
        }

        private bool IsRosterPublicationBlocked(NetworkSnapshot current)
        {
            return current.Phase != ConnectionPhase.InRoom && current.Phase != ConnectionPhase.JoiningRoom;
        }

        private bool HasConnectionPhaseChanged(NetworkSnapshot snapshot, NetworkSnapshot previous)
        {
            return previous.Phase != snapshot.Phase || previous.Generation != snapshot.Generation;
        }
    }
}
