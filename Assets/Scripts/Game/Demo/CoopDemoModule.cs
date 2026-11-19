using Fusion.Photon.Realtime;
using System;
using System.Collections.Generic;
using System.Threading;
using Core.Infra.Lifetime;
using Core.Infra.Module;
using Core.Input;
using Core.Networking.Core;
using Core.Networking.Fusion;
using Core.Networking.Players;
using Core.Resources;
using Core.UI;
using Core.UI.Windows;
using Cysharp.Threading.Tasks;
using Fusion;
using R3;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace Game.Demo
{
    public sealed class CoopDemoModule : IModule, IPlayerInputSource
    {
        private readonly ResourceScope _resources = new();
        private readonly CancellationTokenSource _lifetime = new();
        private CancellationTokenSource _operation;
        private GameObject _root;
        private NetworkService _network;
        private PlayerSessionCoordinator _players;
        private LocalInputService _input;
        private WindowsService _windows;
        private TextMeshProUGUI _status;
        private string _roomName = "Coop demo";
        private string _message = string.Empty;
        private PlayerAvatar _localAvatar;
        private float _nextMovement;
        private bool _stopping;

        public void Start()
        {
            InitializeAsync().Forget(error =>
            {
                Debug.LogException(error);
                Dispose();
            });
        }

        public void Dispose()
        {
            if (_stopping)
            {
                return;
            }
            _stopping = true;
            StopAsync().Forget(Debug.LogException);
        }

        private async UniTask StopAsync()
        {
            _lifetime.Cancel();
            _operation?.Cancel();
            try
            {
                if (_network)
                {
                    await _network.LeaveSessionAsync(LeaveMode.ForgetSession);
                }
            }
            finally
            {
                if (_root)
                {
                    _root.SetActive(false);
                }
                try
                {
                    _resources.Dispose();
                }
                finally
                {
                    if (_root)
                    {
                        UnityEngine.Object.Destroy(_root);
                    }
                    _lifetime.Dispose();
                }
            }
        }

        public bool TryCollect(out NetworkInputData data)
        {
            data = new NetworkInputData
            {
                MoveInput = _input.Move,
                Paused = !_input.GameplayEnabled
            };
            return _localAvatar;
        }

        private async UniTask InitializeAsync()
        {
            _root = new GameObject("CoopDemo");
            UnityEngine.Object.DontDestroyOnLoad(_root);
            _network = _root.AddComponent<NetworkService>();
            var actions = InputActionsFactory.Create();
            _input = _resources.Own(new LocalInputService(actions));
            UnityEngine.Object.Destroy(actions);
            var identity = new LocalPlayerIdentity(CommandLine("--core-client-id"));
            _players = new PlayerSessionCoordinator(_network, identity, SceneRef.FromIndex(1), InitializeLocalPlayerAsync);
            _resources.Own(_players);
            var directory = _resources.Own(new LobbyDirectory());
            var reconnect = new ReconnectStore(TimeSpan.FromMinutes(10));
            _network.Configure(_players, directory, reconnect, identity);
            _network.InitialScene = SceneRef.FromIndex(1);
            _resources.Own(await _network.Init(new NetworkService.Model(), _lifetime.Token));
            CreateInterface();
            _resources.Own(await _windows.Init(new WindowsService.Model(), _lifetime.Token));
            _resources.Own(_network.OnUnexpectedDisconnect.SafeSubscribe(_ => ShowConnection("Connection lost")));
            _resources.Own(_windows.CurrentWindow.SafeSubscribe(window =>
            {
                if (_localAvatar)
                {
                    _input.SetGameplayBlocked(_localAvatar, window);
                }
            }));
            _resources.Own(Observable.EveryUpdate().SafeSubscribe(_ => Tick()));
            var appId = Environment.GetEnvironmentVariable("CORE_PHOTON_APP_ID") ?? PlayerPrefs.GetString("Core.PhotonAppId", string.Empty);
            if (!string.IsNullOrWhiteSpace(appId))
            {
                PhotonAppSettings.Global.AppSettings.AppIdFusion = appId;
            }
            ShowMenu();
        }

        private UniTask<IDisposable> InitializeLocalPlayerAsync(PlayerAvatar avatar, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            var scope = new ResourceScope();
            try
            {
                scope.Own(_input.BindPlayer(avatar));
                scope.Own(_network.RegisterInputSource(avatar.Runner, this));
                _localAvatar = avatar;
                scope.Own(Disposable.Create(() => _localAvatar = null));
                return UniTask.FromResult<IDisposable>(scope);
            }
            catch
            {
                scope.Dispose();
                throw;
            }
        }

        private void CreateInterface()
        {
            var canvasObject = new GameObject("Interface", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(_root.transform, false);
            canvasObject.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280, 800);
            _windows = canvasObject.AddComponent<WindowsService>();
            var templates = new List<WindowBase>
            {
                Template<MenuWindow>(canvasObject.transform), Template<RoomsWindow>(canvasObject.transform),
                Template<LoadingWindow>(canvasObject.transform), Template<PauseWindow>(canvasObject.transform),
                Template<ConnectionWindow>(canvasObject.transform)
            };
            _windows.Configure(canvasObject.transform, templates);
            var eventObject = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            eventObject.transform.SetParent(_root.transform, false);
            _windows.ConfigureInput(_input);
            var module = eventObject.GetComponent<InputSystemUIInputModule>();
            module.actionsAsset = _input.Actions;
            module.point = InputReference(GameInputAction.Point);
            module.leftClick = InputReference(GameInputAction.Click);
            module.move = InputReference(GameInputAction.Navigate);
            module.submit = InputReference(GameInputAction.Submit);
            module.cancel = InputReference(GameInputAction.Cancel);
            _status = DemoUi.Label(canvasObject.transform, string.Empty, 80);
            var rect = (RectTransform)_status.transform;
            rect.anchorMin = new Vector2(0, 1);
            rect.anchorMax = new Vector2(1, 1);
            rect.pivot = new Vector2(0.5f, 1);
            rect.sizeDelta = new Vector2(-40, 85);
            rect.anchoredPosition = new Vector2(0, -15);
        }

        private InputActionReference InputReference(GameInputAction action)
        {
            var reference = InputActionReference.Create(_input.GetAction(action));
            _resources.Own(Disposable.Create(() =>
            {
                if (Application.isPlaying)
                {
                    UnityEngine.Object.Destroy(reference);
                }
                else
                {
                    UnityEngine.Object.DestroyImmediate(reference);
                }
            }));
            return reference;
        }

        private T Template<T>(Transform parent) where T : WindowBase
        {
            var instance = new GameObject(typeof(T).Name, typeof(RectTransform), typeof(Canvas), typeof(GraphicRaycaster));
            instance.SetActive(false);
            instance.transform.SetParent(parent, false);
            DemoUi.Stretch((RectTransform)instance.transform);
            return instance.AddComponent<T>();
        }

        private void ShowMenu()
        {
            _windows.Open(new MenuWindow.Model
            {
                Title = "Coop foundation",
                Build = content =>
                {
                    DemoUi.Label(content, "Photon Fusion App ID", 28);
                    var appId = DemoUi.Field(content, PhotonAppSettings.Global.AppSettings.AppIdFusion);
                    DemoUi.Label(content, "Room name", 28);
                    var room = DemoUi.Field(content, _roomName);
                    DemoUi.Button(content, "Create room", () =>
                    {
                        SaveSettings(appId.text, room.text);
                        RunAsync(token => _network.CreateSessionAsync(new CreateSessionRequest(_roomName, playerCount: 8), token), EnterWorld).Forget();
                    });
                    DemoUi.Button(content, "Find rooms", () =>
                    {
                        SaveSettings(appId.text, room.text);
                        RunAsync(_network.ConnectToLobbyAsync, ShowRooms).Forget();
                    });
                    DemoUi.Button(content, "Solo smoke test", () => RunAsync(token =>
                        _network.CreateSessionAsync(new CreateSessionRequest("Offline", isSolo: true), token), EnterWorld).Forget());
                    if (_network.Reconnect.Current?.IsComplete == true)
                    {
                        DemoUi.Button(content, "Reconnect", () => ReconnectAsync().Forget());
                    }
                    DemoUi.Label(content, _message, 70);
                }
            });
        }

        private void ShowRooms()
        {
            _windows.Open(new RoomsWindow.Model
            {
                Title = "Rooms",
                Back = () => LeaveAsync().Forget(),
                Build = content =>
                {
                    foreach (var session in _network.Directory.Sessions.CurrentValue)
                    {
                        if (!session.IsOpen)
                        {
                            continue;
                        }
                        var target = new SessionLocator(session.Name, session.Region);
                        DemoUi.Button(content, $"{session.Name} ({session.PlayerCount}/{session.MaxPlayers})", () =>
                            RunAsync(token => _network.JoinSessionAsync(target, token), EnterWorld).Forget());
                    }
                    DemoUi.Button(content, "Refresh", () => RunAsync(_network.RefreshLobbyAsync, ShowRooms).Forget());
                    DemoUi.Button(content, "Back", () => LeaveAsync().Forget());
                }
            });
        }

        private async UniTask RunAsync(Func<CancellationToken, UniTask<OperationResult>> operation, Action completed)
        {
            if (_operation != null)
            {
                return;
            }
            using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(_lifetime.Token);
            _operation = cancellation;
            _windows.Open(new LoadingWindow.Model
            {
                Title = "Please wait",
                Back = cancellation.Cancel,
                Build = content =>
                {
                    DemoUi.Label(content, "Connecting, loading the scene and preparing your avatar...", 100);
                    DemoUi.Button(content, "Cancel", cancellation.Cancel);
                }
            });
            try
            {
                var result = await operation(cancellation.Token);
                if (_lifetime.IsCancellationRequested)
                {
                    return;
                }
                if (result.IsSuccess)
                {
                    _message = string.Empty;
                    completed();
                }
                else
                {
                    _message = result.Status == OperationStatus.Cancelled ? "Cancelled" : new NetworkOperationException(result).Message;
                    ShowMenu();
                }
            }
            catch (OperationCanceledException)
            {
                if (!_lifetime.IsCancellationRequested)
                {
                    ShowMenu();
                }
            }
            finally
            {
                _operation = null;
            }
        }

        private void EnterWorld()
        {
            _windows.CloseAll();
            _input.SetGameplayBlocked(_localAvatar, false);
        }

        private void ShowPause()
        {
            _windows.Open(new PauseWindow.Model
            {
                Title = "Pause",
                Back = _windows.Close,
                Build = content =>
                {
                    DemoUi.Button(content, "Continue", _windows.Close);
                    DemoUi.Button(content, "Disconnect and keep avatar", () => LeaveAsync(true).Forget());
                    DemoUi.Button(content, "Leave room", () => LeaveAsync().Forget());
                }
            }, false);
        }

        private async UniTask LeaveAsync(bool keep = false)
        {
            _operation?.Cancel();
            var mode = keep ? LeaveMode.KeepReconnect : LeaveMode.ForgetSession;
            var result = await _network.LeaveSessionAsync(mode, _lifetime.Token);
            if (_lifetime.IsCancellationRequested)
            {
                return;
            }
            _message = result.IsSuccess ? string.Empty : new NetworkOperationException(result).Message;
            ShowMenu();
        }

        private async UniTask ReconnectAsync()
        {
            var target = _network.Reconnect.Current;
            if (target == null)
            {
                return;
            }
            await _network.LeaveSessionAsync(LeaveMode.KeepReconnect, _lifetime.Token);
            await RunAsync(token => _network.JoinSessionAsync(target, token), EnterWorld);
        }

        private void ShowConnection(string message)
        {
            _windows.Open(new ConnectionWindow.Model
            {
                Title = message,
                Build = content =>
                {
                    DemoUi.Button(content, "Reconnect", () => ReconnectAsync().Forget());
                    DemoUi.Button(content, "Main menu", () => LeaveAsync().Forget());
                }
            });
        }

        private void Tick()
        {
            if (_network.Snapshot.CurrentValue.Phase == ConnectionPhase.InRoom && !_windows.CurrentWindow.CurrentValue &&
                _input.WasPressed(GameInputAction.Pause))
            {
                ShowPause();
            }
            if (_localAvatar && Time.unscaledTime >= _nextMovement)
            {
                _nextMovement = Time.unscaledTime + 0.05f;
                _localAvatar.GetComponent<DemoAvatar>().RPC_Move(_input.Move);
            }
            var snapshot = _network.Snapshot.CurrentValue;
            var count = _players.Current.CurrentValue ? _players.Current.CurrentValue.Players.Avatars.Length : 0;
            _status.text = $"{snapshot.Phase} | Peers: {snapshot.Peers.Count} | Avatars: {count} | Generation: {snapshot.Generation}\nWASD / left stick: move | Esc / Start: pause";
        }

        private void SaveSettings(string appId, string room)
        {
            PhotonAppSettings.Global.AppSettings.AppIdFusion = appId.Trim();
            PlayerPrefs.SetString("Core.PhotonAppId", appId.Trim());
            PlayerPrefs.Save();
            _roomName = room.Trim();
        }

        private static string CommandLine(string key)
        {
            var args = Environment.GetCommandLineArgs();
            var index = Array.IndexOf(args, key);
            return index >= 0 && index + 1 < args.Length ? args[index + 1] : null;
        }
    }
}
