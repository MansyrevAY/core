using Fusion.Photon.Realtime;
using System.IO;
using Core.Networking.Players;
using Fusion;
using Fusion.Editor;
using Game.Demo;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace Game.Editor
{
    public static class DemoSetup
    {
        [MenuItem("Tools/Core/Create coop demo")]
        public static void Create()
        {
            Directory.CreateDirectory("Assets/Demo/Prefabs");
            Directory.CreateDirectory("Assets/Demo/Scenes");
            if (!AssetDatabase.LoadAssetAtPath<TMP_Settings>("Assets/TextMesh Pro/Resources/TMP Settings.asset"))
            {
                TMP_PackageResourceImporter.ImportResources(true, false, false);
            }
            var appSettings = AssetDatabase.LoadAssetAtPath<PhotonAppSettings>("Assets/Photon/Fusion/Resources/PhotonAppSettings.asset");
            if (!appSettings)
            {
                appSettings = ScriptableObject.CreateInstance<PhotonAppSettings>();
                Directory.CreateDirectory("Assets/Photon/Fusion/Resources");
                AssetDatabase.CreateAsset(appSettings, "Assets/Photon/Fusion/Resources/PhotonAppSettings.asset");
                AssetDatabase.SetLabels(appSettings, new[] { "FusionDefaultGlobal" });
            }
            var renderer = ScriptableObject.CreateInstance<UniversalRendererData>();
            renderer = SaveAsset(renderer, "Assets/Demo/Renderer.asset");
            var pipeline = UniversalRenderPipelineAsset.Create(renderer);
            pipeline = SaveAsset(pipeline, "Assets/Demo/Pipeline.asset");
            GraphicsSettings.defaultRenderPipeline = pipeline;
            QualitySettings.renderPipeline = pipeline;
            var avatar = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            avatar.name = "DemoAvatar";
            var networkObject = avatar.AddComponent<NetworkObject>();
            networkObject.Flags = NetworkObjectFlags.MasterClientObject;
            avatar.AddComponent<NetworkTransform>();
            avatar.AddComponent<PlayerAvatar>();
            avatar.AddComponent<DemoAvatar>();
            var material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            material.color = new Color(0.2f, 0.65f, 0.95f);
            material = SaveAsset(material, "Assets/Demo/Avatar.mat");
            avatar.GetComponent<Renderer>().sharedMaterial = material;
            var prefab = PrefabUtility.SaveAsPrefabAsset(avatar, "Assets/Demo/Prefabs/DemoAvatar.prefab");
            Object.DestroyImmediate(avatar);
            var prefabRoot = PrefabUtility.LoadPrefabContents("Assets/Demo/Prefabs/DemoAvatar.prefab");
            new NetworkObjectBakerEditTime().Bake(prefabRoot);
            PrefabUtility.SaveAsPrefabAsset(prefabRoot, "Assets/Demo/Prefabs/DemoAvatar.prefab");
            PrefabUtility.UnloadPrefabContents(prefabRoot);

            var world = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.name = "Floor";
            floor.transform.position = new Vector3(0, -0.5f, 0);
            floor.transform.localScale = new Vector3(40, 1, 40);
            var light = new GameObject("Sun", typeof(Light));
            light.GetComponent<Light>().type = LightType.Directional;
            light.transform.rotation = Quaternion.Euler(50, -30, 0);
            var director = new GameObject("PlayerSession", typeof(NetworkObject), typeof(PlayerSessionDirector));
            director.GetComponent<NetworkObject>().Flags = NetworkObjectFlags.MasterClientObject;
            director.GetComponent<PlayerSessionDirector>().Configure(prefab.GetComponent<NetworkObject>());
            EditorSceneManager.SaveScene(world, "Assets/Demo/Scenes/World.unity");
            new NetworkObjectBakerEditTime().Bake(director);
            EditorSceneManager.SaveScene(world);

            var bootstrap = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            new GameObject("GameClient", typeof(GameClient));
            var camera = new GameObject("Camera", typeof(Camera), typeof(AudioListener));
            camera.tag = "MainCamera";
            camera.transform.position = new Vector3(8, 22, -18);
            camera.transform.LookAt(Vector3.zero);
            camera.GetComponent<Camera>().backgroundColor = new Color(0.08f, 0.12f, 0.18f);
            camera.GetComponent<Camera>().clearFlags = CameraClearFlags.SolidColor;
            EditorSceneManager.SaveScene(bootstrap, "Assets/Demo/Scenes/Bootstrap.unity");
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene("Assets/Demo/Scenes/Bootstrap.unity", true),
                new EditorBuildSettingsScene("Assets/Demo/Scenes/World.unity", true)
            };
            var playerSettings = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset")[0]);
            playerSettings.FindProperty("activeInputHandler").intValue = 1;
            playerSettings.ApplyModifiedPropertiesWithoutUndo();
            PlayerSettings.runInBackground = true;
            PlayerSettings.companyName = "TheKingsdom";
            PlayerSettings.productName = "Core Coop Demo";
            NetworkProjectConfigUtilities.RebuildPrefabTable();
            AssetDatabase.SaveAssets();
            Debug.Log("CORE_DEMO_SETUP_COMPLETE");
        }
        private static T SaveAsset<T>(T asset, string path) where T : Object
        {
            var existing = AssetDatabase.LoadAssetAtPath<T>(path);
            if (existing)
            {
                Object.DestroyImmediate(asset);
                return existing;
            }
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }
    }
}
