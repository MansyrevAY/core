using Core.Resources;
using System;
using System.Collections.Generic;
using Core.Networking.Core;
using Core.Infra.Lifetime;
using Cysharp.Threading.Tasks;
using Fusion;
using Fusion.Sockets;
using R3;


namespace Core.Networking.Fusion
{
    public partial class NetworkService : DisposableBehaviour<NetworkService.Model>, INetworkRunnerCallbacks
    {
        public sealed class Model { }
        public SceneRef? InitialScene { get; set; }
        private const int EmptyRoomTtlMilliseconds = 60_000;
        private NetworkRunner _runner => _session?.Runner;
        private string _connectedRegion;
        private LocalPlayerIdentity _identity;
        public ReconnectStore Reconnect { get; private set; }
        public LobbyDirectory Directory { get; private set; }
        private readonly ReactiveProperty<ConnectionState> _connectionState = new(global::Core.Networking.Fusion.ConnectionState.Disconnected);
        private readonly ReactiveProperty<PlayerRef[]> _roomPlayers = new(Array.Empty<PlayerRef>());
        private readonly ReactiveProperty<bool> _isSessionOwner = new(false);
        private readonly Subject<bool> _onUnexpectedDisconnect = new();
        private readonly Subject<string> _onConnectionError = new();
        private readonly Subject<NetworkEvent> _events = new();
        public ReadOnlyReactiveProperty<ConnectionState> ConnectionState => _connectionState;
        public ReadOnlyReactiveProperty<PlayerRef[]> RoomPlayers => _roomPlayers;
        public ReadOnlyReactiveProperty<bool> IsSessionOwner => _isSessionOwner;
        public Observable<NetworkEvent> Events => _events;
        public Observable<bool> OnUnexpectedDisconnect => _onUnexpectedDisconnect;
        public Observable<string> OnConnectionError => _onConnectionError;
        public NetworkRunner Runner => _runner;
        public NetworkSceneLoader SceneLoader => _session?.Scenes;

        public IDisposable RegisterInputSource(NetworkRunner runner, IPlayerInputSource source)
        {
            if (!AcceptCallback(runner))
            {
                throw new InvalidOperationException("Runner is not current");
            }

            if (_session.Input.Current != null)
            {
                throw new InvalidOperationException("Release the previous input binding before registering another source");
            }

            return _session.Input.Register(source);
        }

        private void Update()
        {
            if (HasSessionOwnershipChanged())
            {
                PublishRoster();
            }
        }

        void INetworkRunnerCallbacks.OnPlayerJoined(NetworkRunner runner, PlayerRef player)
        {
            if (!AcceptCallback(runner))
            {
                return;
            }

            PublishRoster();
        }

        void INetworkRunnerCallbacks.OnPlayerLeft(NetworkRunner runner, PlayerRef player)
        {
            if (!AcceptCallback(runner))
            {
                return;
            }

            PublishRoster();
        }

        void INetworkRunnerCallbacks.OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> list)
        {
            if (!AcceptCallback(runner))
            {
                return;
            }

            var region = runner.LobbyInfo.Region;
            Directory.Update(Generation, region, list);
        }

        void INetworkRunnerCallbacks.OnSceneLoadDone(NetworkRunner runner)
        {
        }

        void INetworkRunnerCallbacks.OnSceneLoadStart(NetworkRunner runner)
        {
        }

        void INetworkRunnerCallbacks.OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason)
        {
            if (!AcceptCallback(runner))
            {
                return;
            }

            var wasInRoom = _connectionState.Value == global::Core.Networking.Fusion.ConnectionState.InRoom;
            if (!wasInRoom)
            {
                return;
            }

            PublishPhase(ConnectionPhase.Disconnected);
            _onUnexpectedDisconnect.OnNext(false);
        }

        void INetworkRunnerCallbacks.OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
        {
            if (!AcceptCallback(runner))
            {
                return;
            }

            var wasInRoom = _connectionState.Value == global::Core.Networking.Fusion.ConnectionState.InRoom;
            if (!wasInRoom)
            {
                return;
            }

            PublishPhase(ConnectionPhase.Disconnected);
            _onUnexpectedDisconnect.OnNext(false);
        }

        void INetworkRunnerCallbacks.OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress,
            NetConnectFailedReason reason)
        {
            if (!AcceptCallback(runner))
            {
                return;
            }

            if (Operations.IsBusy)
            {
                return;
            }

            PublishPhase(ConnectionPhase.Disconnected);
            var value = ConnectFailedReasonToString(reason);
            _onConnectionError.OnNext(value);
        }

        private static string ConnectFailedReasonToString(NetConnectFailedReason reason) => reason switch
        {
            NetConnectFailedReason.Timeout => "No response from server",
            NetConnectFailedReason.ServerFull => "Server is full",
            NetConnectFailedReason.ServerRefused => "Server refused the connection",
            _ => $"Failed to connect ({reason})",
        };

        void INetworkRunnerCallbacks.OnConnectedToServer(NetworkRunner runner)
        {
        }

        void INetworkRunnerCallbacks.OnInput(NetworkRunner runner, NetworkInput input)
        {
            if (!AcceptCallback(runner))
            {
                return;
            }

            var source = _session.Input.Current;
            var data = default(NetworkInputData);
            if (source != null)
            {
                source.TryCollect(out data);
            }

            input.Set(data);
        }

        void INetworkRunnerCallbacks.OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input)
        {
        }

        void INetworkRunnerCallbacks.OnConnectRequest(NetworkRunner runner,
            NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token)
        {
        }

        void INetworkRunnerCallbacks.OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message)
        {
        }

        void INetworkRunnerCallbacks.OnCustomAuthenticationResponse(NetworkRunner runner,
            Dictionary<string, object> data)
        {
        }

        void INetworkRunnerCallbacks.OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken)
        {
        }

        void INetworkRunnerCallbacks.OnObjectEnterAOI(NetworkRunner runner, NetworkObject networkObject, PlayerRef player)
        {
        }

        void INetworkRunnerCallbacks.OnObjectExitAOI(NetworkRunner runner, NetworkObject networkObject, PlayerRef player)
        {
        }

        void INetworkRunnerCallbacks.OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key,
            ReadOnlySpan<byte> data)
        {
        }

        void INetworkRunnerCallbacks.OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key,
            float progress)
        {
        }

        protected override void OnDestroy()
        {
            _disposed = true;
            Operations.Dispose();
            var session = _session;
            if (session != null)
            {
                _epochs.Retire(session.Runner, session.Generation);
                session.StopOnOwnerDestroyedAsync().Forget();
            }

            base.OnDestroy();
            _events.Dispose();
            _snapshot.Dispose();
            _connectionState.Dispose();
            _roomPlayers.Dispose();
            _isSessionOwner.Dispose();
            _onUnexpectedDisconnect.Dispose();
            _onConnectionError.Dispose();

        }

        private bool HasSessionOwnershipChanged()
        {
            return _snapshot.CurrentValue.Phase == ConnectionPhase.InRoom && AcceptCallback(_runner) && _snapshot.CurrentValue.IsOwner != _runner.IsLocalPlayerSessionOwner();
        }
    }
}
