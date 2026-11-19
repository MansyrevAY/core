using System.Linq;
using Unity.PlayMode.Editor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Toolbars;
using UnityEngine;

namespace Game.Editor
{
    [InitializeOnLoad]
    public static class BootstrapLoadMenu
    {
        private const string PREVIOUS_START_SCENE_KEY = "Core.Bootstrap.PreviousStartScene";
        private const string LAUNCHED_FROM_BOOTSTRAP_KEY = "Core.Bootstrap.Launched";

        static BootstrapLoadMenu()
        {
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
        }

        [MainToolbarElement("Core/StartPlay", defaultDockPosition = MainToolbarDockPosition.Middle)]
        public static MainToolbarElement StartPlay()
        {
            var icon = EditorGUIUtility.IconContent("PlayButton").image as Texture2D;
            var content = new MainToolbarContent("root", icon, "Play from the first enabled build scene");
            return new MainToolbarButton(content, PlayFromBootstrap);
        }

        [MenuItem("Tools/Core/Play from bootstrap")]
        public static void PlayFromBootstrap()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating ||
                PlayModeScenarioManager.State != PlayModeScenarioState.Idle)
            {
                return;
            }

            var firstScene = EditorBuildSettings.scenes.FirstOrDefault(scene => scene.enabled);
            var startScene = firstScene == null ? null : AssetDatabase.LoadAssetAtPath<SceneAsset>(firstScene.path);
            if (!startScene)
            {
                Debug.LogError("Add a bootstrap scene to the enabled build scenes before Play.");
                return;
            }
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            SessionState.SetString(PREVIOUS_START_SCENE_KEY, AssetDatabase.GetAssetPath(EditorSceneManager.playModeStartScene));
            SessionState.SetBool(LAUNCHED_FROM_BOOTSTRAP_KEY, true);
            EditorSceneManager.playModeStartScene = startScene;
            PlayModeScenarioManager.Start();
        }

        private static void OnPlayModeChanged(PlayModeStateChange state)
        {
            if (state != PlayModeStateChange.EnteredEditMode || !SessionState.GetBool(LAUNCHED_FROM_BOOTSTRAP_KEY, false))
            {
                return;
            }

            var previousPath = SessionState.GetString(PREVIOUS_START_SCENE_KEY, string.Empty);
            SessionState.EraseString(PREVIOUS_START_SCENE_KEY);
            SessionState.EraseBool(LAUNCHED_FROM_BOOTSTRAP_KEY);
            EditorSceneManager.playModeStartScene = string.IsNullOrEmpty(previousPath)
                ? null
                : AssetDatabase.LoadAssetAtPath<SceneAsset>(previousPath);
        }
    }
}
