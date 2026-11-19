using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Core.UI.Windows;
using Game.Demo;
using System.Threading;
using Core.Networking.Core;
using Core.Networking.Fusion;
using Core.Networking.Players;
using Cysharp.Threading.Tasks;
using Fusion;
using Fusion.Photon.Realtime;
using NUnit.Framework;
using R3;
using UnityEditor.SceneManagement;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;

namespace Core.Tests.Integration
{
    public sealed class SessionLifecycleTests
    {
        private readonly List<GameObject> _roots = new();
        private SceneSetup[] _scenes;
        private string _originalAppId;
        private InputSettings.EditorInputBehaviorInPlayMode _oldEditorInput;
        private InputSettings.BackgroundBehavior _oldBackground;
        private bool _oldPlayOptionsEnabled;
        private EnterPlayModeOptions _oldPlayOptions;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            _oldEditorInput = InputSystem.settings.editorInputBehaviorInPlayMode;
            _oldBackground = InputSystem.settings.backgroundBehavior;
            InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            _scenes = EditorSceneManager.GetSceneManagerSetup();
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            _oldPlayOptionsEnabled = EditorSettings.enterPlayModeOptionsEnabled;
            _oldPlayOptions = EditorSettings.enterPlayModeOptions;
            EditorSettings.enterPlayModeOptionsEnabled = true;
            EditorSettings.enterPlayModeOptions = EnterPlayModeOptions.DisableDomainReload;
            yield return new EnterPlayMode(false);
            _originalAppId = PhotonAppSettings.Global.AppSettings.AppIdFusion;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            foreach (var root in _roots)
            {
                if (root)
                {
                    var leave = root.GetComponent<NetworkService>().LeaveSessionAsync(LeaveMode.ForgetSession).AsTask();
                    while (!leave.IsCompleted)
                    {
                        yield return null;
                    }
                    UnityEngine.Object.Destroy(root);
                }
            }
            _roots.Clear();
            PhotonAppSettings.Global.AppSettings.AppIdFusion = _originalAppId;
            yield return null;
            if (EditorApplication.isPlaying)
            {
                yield return new ExitPlayMode();
            }
            InputSystem.settings.editorInputBehaviorInPlayMode = _oldEditorInput;
            InputSystem.settings.backgroundBehavior = _oldBackground;
            EditorSettings.enterPlayModeOptionsEnabled = _oldPlayOptionsEnabled;
            EditorSettings.enterPlayModeOptions = _oldPlayOptions;
            if (_scenes.Length > 0)
            {
                EditorSceneManager.RestoreSceneManagerSetup(_scenes);
            }
            else
            {
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            }
        }

        [UnityTest]
        public IEnumerator SoloSessionCanLeaveAndStartWithFreshRunner()
        {
            return UniTask.ToCoroutine(async () =>
            {
                var released = 0;
                var network = await CreatePeer("solo", (_, _) =>
                    UniTask.FromResult<IDisposable>(Disposable.Create(() => released++)));
                var first = await network.CreateSessionAsync(new CreateSessionRequest("solo", isSolo: true));
                Assert.That(first.IsSuccess, Is.True, first.Error?.Reason);
                var runner = network.Runner;
                var generation = network.Generation;
                var context = runner.GetComponent<PlayerSessionContext>();
                Assert.That((bool)context.Bootstrap.LocalPlayer.CurrentValue.IsReady, Is.True);
                Assert.That(context.Players.Avatars.Length, Is.EqualTo(1));
                var leave = await network.LeaveSessionAsync(LeaveMode.ForgetSession);
                Assert.That(leave.IsSuccess, Is.True, leave.Error?.Reason);
                Assert.That(context.Players.IsDisposed, Is.True);
                Assert.That(released, Is.EqualTo(1));
                Assert.That(network.Runner, Is.Null);
                var second = await network.CreateSessionAsync(new CreateSessionRequest("again", isSolo: true));
                Assert.That(second.IsSuccess, Is.True, second.Error?.Reason);
                Assert.That(network.Generation.Value, Is.GreaterThan(generation.Value));
                Assert.That(network.Runner, Is.Not.SameAs(runner));
                Assert.That(released, Is.EqualTo(1));
                await network.LeaveSessionAsync(LeaveMode.ForgetSession);
                Assert.That(released, Is.EqualTo(2));
            });
        }

        [UnityTest]
        public IEnumerator CancelDuringLocalInitializationReleasesSession()
        {
            return UniTask.ToCoroutine(async () =>
            {
                var entered = new UniTaskCompletionSource();
                var network = await CreatePeer("cancel", async (_, token) =>
                {
                    entered.TrySetResult();
                    await UniTask.Delay(60000, cancellationToken: token);
                    return Disposable.Empty;
                });
                using var cancellation = new CancellationTokenSource();
                var pending = network.CreateSessionAsync(new CreateSessionRequest("cancel", isSolo: true), cancellation.Token).AsTask();
                await entered.Task.Timeout(TimeSpan.FromSeconds(40));
                cancellation.Cancel();
                var result = await pending;
                Assert.That(result.Status, Is.EqualTo(OperationStatus.Cancelled));
                Assert.That(network.Runner, Is.Null);
                Assert.That(network.Snapshot.CurrentValue.Phase, Is.EqualTo(ConnectionPhase.Disconnected));
            });
        }

        [UnityTest]
        public IEnumerator LocalInitializationFailureCanRetryWithFreshRunner()
        {
            return UniTask.ToCoroutine(async () =>
            {
                var fail = true;
                PlayerSessionContext failedContext = null;
                var network = await CreatePeer("retry", (avatar, _) =>
                {
                    if (fail)
                    {
                        failedContext = avatar.Runner.GetComponent<PlayerSessionContext>();
                        throw new InvalidOperationException("Local initialization failed");
                    }
                    return UniTask.FromResult<IDisposable>(Disposable.Empty);
                });
                LogAssert.Expect(LogType.Exception,
                    new System.Text.RegularExpressions.Regex("InvalidOperationException: Local initialization failed"));
                var first = await network.CreateSessionAsync(new CreateSessionRequest("failure", isSolo: true));
                Assert.That(first.Status, Is.EqualTo(OperationStatus.Failed));
                Assert.That(network.Runner, Is.Null);
                Assert.That(failedContext.Players.IsDisposed, Is.True);
                Assert.That(network.Snapshot.CurrentValue.Phase, Is.EqualTo(ConnectionPhase.Disconnected));
                var generation = network.Generation;
                fail = false;
                var retry = await network.CreateSessionAsync(new CreateSessionRequest("retry", isSolo: true));
                Assert.That(retry.IsSuccess, Is.True, retry.Error?.Reason);
                Assert.That(network.Generation.Value, Is.GreaterThan(generation.Value));
                Assert.That((bool)network.Runner.GetComponent<PlayerSessionContext>().Bootstrap.LocalPlayer.CurrentValue.IsReady, Is.True);
            });
        }

        [UnityTest]
        public IEnumerator CancelAfterInitializationReturnsReleasesLateLease()
        {
            return UniTask.ToCoroutine(async () =>
            {
                var entered = new UniTaskCompletionSource();
                var release = new UniTaskCompletionSource();
                var released = 0;
                var network = await CreatePeer("late", async (_, _) =>
                {
                    entered.TrySetResult();
                    await release.Task;
                    return Disposable.Create(() => released++);
                });
                using var cancellation = new CancellationTokenSource();
                var pending = network.CreateSessionAsync(new CreateSessionRequest("late", isSolo: true), cancellation.Token).AsTask();
                try
                {
                    await entered.Task.Timeout(TimeSpan.FromSeconds(40));
                    cancellation.Cancel();
                }
                finally
                {
                    release.TrySetResult();
                }
                var result = await pending;
                Assert.That(result.Status, Is.EqualTo(OperationStatus.Cancelled));
                Assert.That(released, Is.EqualTo(1));
                Assert.That(network.Runner, Is.Null);
                await network.LeaveSessionAsync(LeaveMode.ForgetSession);
                Assert.That(released, Is.EqualTo(1));
            });
        }

        [UnityTest, Category("PhotonOnline")]
        public IEnumerator TwoPeersReconnectToExistingAvatarAndSurviveOwnerLeaving()
        {
            return UniTask.ToCoroutine(async () =>
            {
                var appId = Environment.GetEnvironmentVariable("CORE_PHOTON_APP_ID");
                if (string.IsNullOrWhiteSpace(appId))
                {
                    Assert.Ignore("Set CORE_PHOTON_APP_ID to run the two-peer Photon test.");
                }
                PhotonAppSettings.Global.AppSettings.AppIdFusion = appId;
                var first = await CreatePeer("alice");
                var second = await CreatePeer("bob");
                var created = await first.CreateSessionAsync(new CreateSessionRequest(Guid.NewGuid().ToString("N"), playerCount: 4, saveSession: false));
                Assert.That(created.IsSuccess, Is.True, created.Error?.Reason);
                var locator = first.Snapshot.CurrentValue.Session;
                var joined = await second.JoinSessionAsync(locator);
                Assert.That(joined.IsSuccess, Is.True, joined.Error?.Reason);
                var secondContext = second.Runner.GetComponent<PlayerSessionContext>();
                var avatarId = secondContext.Bootstrap.LocalPlayer.CurrentValue.Object.Id;
                await second.LeaveSessionAsync(LeaveMode.KeepReconnect);
                var firstContext = first.Runner.GetComponent<PlayerSessionContext>();
                await UniTask.WaitUntil(() => firstContext.Players.Avatars.Length == 2 &&
                    Array.Exists(firstContext.Players.Avatars, record => record.Avatar.IsDisconnected)).Timeout(TimeSpan.FromSeconds(15));
                var rejoined = await second.JoinSessionAsync(locator);
                Assert.That(rejoined.IsSuccess, Is.True, rejoined.Error?.Reason);
                secondContext = second.Runner.GetComponent<PlayerSessionContext>();
                Assert.That(secondContext.Bootstrap.LocalPlayer.CurrentValue.Object.Id, Is.EqualTo(avatarId));
                Assert.That(secondContext.Players.Avatars.Length, Is.EqualTo(2));
                await first.LeaveSessionAsync(LeaveMode.ForgetSession);
                await UniTask.WaitUntil(() => secondContext.Director.Object.HasStateAuthority).Timeout(TimeSpan.FromSeconds(15));
                Assert.That((bool)secondContext.Bootstrap.LocalPlayer.CurrentValue.IsReady, Is.True);
                Assert.That(second.Runner.IsRunning, Is.True);
            });
        }

        [UnityTest]
        public IEnumerator DemoWindowsCanStartSoloPauseAndReturnToMenu()
        {
            return UniTask.ToCoroutine(async () =>
            {
                var keyboard = InputSystem.AddDevice<Keyboard>();
                var mouse = InputSystem.AddDevice<Mouse>();
                var client = new GameObject("DemoClient", typeof(Game.GameClient));
                try
                {
                    await UniTask.WaitUntil(() => UnityEngine.Object.FindFirstObjectByType<WindowsService>()).Timeout(TimeSpan.FromSeconds(15));
                    var windows = UnityEngine.Object.FindFirstObjectByType<WindowsService>();
                    await UniTask.WaitUntil(() => windows.CurrentWindow.CurrentValue is MenuWindow).Timeout(TimeSpan.FromSeconds(15));
                    var start = windows.CurrentWindow.CurrentValue.GetComponentsInChildren<UnityEngine.UI.Button>()
                        .Single(button => button.name == "Solo smoke test");
                    await ClickButtonAsync(mouse, start);
                    await UniTask.WaitUntil(() => !windows.CurrentWindow.CurrentValue).Timeout(TimeSpan.FromSeconds(45));
                    var network = UnityEngine.Object.FindFirstObjectByType<NetworkService>();
                    Assert.That(network.Snapshot.CurrentValue.Phase, Is.EqualTo(ConnectionPhase.InRoom));
                    var avatar = network.Runner.GetComponent<PlayerSessionContext>().Bootstrap.LocalPlayer.CurrentValue;
                    Assert.That((bool)avatar.IsReady, Is.True);
                    await UniTask.DelayFrame(3);
                    InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Escape));
                    await UniTask.WaitUntil(() => windows.CurrentWindow.CurrentValue is PauseWindow).Timeout(TimeSpan.FromSeconds(5));
                    InputSystem.QueueStateEvent(keyboard, new KeyboardState());
                    var leaveButton = windows.CurrentWindow.CurrentValue.GetComponentsInChildren<UnityEngine.UI.Button>()
                        .Single(button => button.name == "Leave room");
                    await ClickButtonAsync(mouse, leaveButton);
                    await UniTask.WaitUntil(() => windows.CurrentWindow.CurrentValue is MenuWindow).Timeout(TimeSpan.FromSeconds(15));
                    Assert.That(network.Runner, Is.Null);
                }
                finally
                {
                    UnityEngine.Object.Destroy(client);
                    InputSystem.RemoveDevice(keyboard);
                    InputSystem.RemoveDevice(mouse);
                }
                await UniTask.DelayFrame(3);
            });
        }

        [UnityTest]
        public IEnumerator StoppingPlayModeReleasesDemoInputReferencesWithoutErrors()
        {
            new GameObject("DemoClient", typeof(Game.GameClient));
            UnityEngine.InputSystem.InputActionReference[] references = null;
            yield return UniTask.ToCoroutine(async () =>
            {
                await UniTask.WaitUntil(() => UnityEngine.Object.FindAnyObjectByType<WindowsService>()).Timeout(TimeSpan.FromSeconds(15));
                var windows = UnityEngine.Object.FindAnyObjectByType<WindowsService>();
                await UniTask.WaitUntil(() => windows.CurrentWindow.CurrentValue is MenuWindow).Timeout(TimeSpan.FromSeconds(15));
                var actions = windows.Input.Actions;
                references = UnityEngine.Resources.FindObjectsOfTypeAll<InputActionReference>()
                    .Where(reference => reference.action?.actionMap?.asset == actions).ToArray();
                Assert.That(references.Length, Is.GreaterThan(0));
            });
            yield return new ExitPlayMode();
            var deadline = EditorApplication.timeSinceStartup + 10;
            while (references.Any(reference => reference) && EditorApplication.timeSinceStartup < deadline)
            {
                yield return null;
            }
            Assert.That(references.All(reference => !reference), Is.True,
                "Demo input references must release even when async cleanup completes in Edit Mode");
            LogAssert.NoUnexpectedReceived();
        }

        private static async UniTask ClickButtonAsync(Mouse mouse, UnityEngine.UI.Button button)
        {
            await UniTask.DelayFrame(2);
            Canvas.ForceUpdateCanvases();
            var rect = (RectTransform)button.transform;
            var position = RectTransformUtility.WorldToScreenPoint(null, rect.TransformPoint(rect.rect.center));
            var eventSystem = UnityEngine.EventSystems.EventSystem.current;
            var pointer = new UnityEngine.EventSystems.PointerEventData(eventSystem) { position = position };
            var hits = new List<UnityEngine.EventSystems.RaycastResult>();
            eventSystem.RaycastAll(pointer, hits);
            Assert.That(hits.Count, Is.GreaterThan(0), $"No UI raycast hit at {button.name}");
            Assert.That(UnityEngine.EventSystems.ExecuteEvents.GetEventHandler<UnityEngine.EventSystems.IPointerClickHandler>(hits[0].gameObject),
                Is.SameAs(button.gameObject), $"Pointer does not reach {button.name}");
            InputSystem.QueueStateEvent(mouse, new MouseState { position = position });
            await UniTask.DelayFrame(2);
            InputSystem.QueueStateEvent(mouse, new MouseState { position = position }.WithButton(MouseButton.Left));
            await UniTask.DelayFrame(2);
            InputSystem.QueueStateEvent(mouse, new MouseState { position = position });
            await UniTask.DelayFrame(2);
        }

        private async UniTask<NetworkService> CreatePeer(string identity,
            Func<PlayerAvatar, CancellationToken, UniTask<IDisposable>> initialize = null)
        {
            var root = new GameObject(identity);
            _roots.Add(root);
            UnityEngine.Object.DontDestroyOnLoad(root);
            var network = root.AddComponent<NetworkService>();
            var localIdentity = new LocalPlayerIdentity(identity);
            var coordinator = new PlayerSessionCoordinator(network, localIdentity, SceneRef.FromIndex(1),
                initialize ?? ((_, _) => UniTask.FromResult<IDisposable>(Disposable.Empty)));
            network.Configure(coordinator, new LobbyDirectory(), new ReconnectStore(TimeSpan.FromMinutes(5)), localIdentity);
            network.InitialScene = SceneRef.FromIndex(1);
            await network.Init(new NetworkService.Model());
            return network;
        }
    }
}
