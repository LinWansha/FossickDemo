using System.Collections.Generic;
using System.IO;
using Fossick.MapStudio.Controllers;
using Fossick.MapStudio.Views;
using Fossick.Preview.Controllers;
using Fossick.Preview.Views;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Fossick.Editor.Setup
{
    public static class FossickProjectSetup
    {
        private const string PreviewScenePath = "Assets/Fossick/Preview/Scenes/FossickPreview.unity";
        private const string MapStudioScenePath = "Assets/Fossick/MapStudio/Scenes/FossickMapStudio.unity";

        [MenuItem("Fossick/Setup/Create Test Scenes")]
        public static void CreateTestScenes()
        {
            EnsureDirectory(Path.GetDirectoryName(PreviewScenePath));
            EnsureDirectory(Path.GetDirectoryName(MapStudioScenePath));

            CreatePreviewScene();
            CreateMapStudioScene();
            AddScenesToBuildSettings();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Fossick test scenes created.");
        }

        [MenuItem("Fossick/Setup/Run Deployment Smoke Test")]
        public static void RunDeploymentSmokeTest()
        {
            AssertSceneComponent<FossickPreviewController>(PreviewScenePath, "FossickPreviewRoot");
            AssertSceneComponent<FossickPreviewView>(PreviewScenePath, "FossickPreviewRoot");
            AssertSceneComponent<FossickMapStudioController>(MapStudioScenePath, "FossickMapStudioRoot");
            AssertSceneComponent<FossickMapStudioView>(MapStudioScenePath, "FossickMapStudioRoot");
            Debug.Log("Fossick deployment smoke test passed.");
        }

        private static void CreatePreviewScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var root = new GameObject("FossickPreviewRoot");
            root.AddComponent<FossickPreviewController>();
            root.AddComponent<FossickPreviewView>();

            CreateCamera("PreviewCamera");
            EditorSceneManager.SaveScene(scene, PreviewScenePath);
        }

        private static void CreateMapStudioScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var root = new GameObject("FossickMapStudioRoot");
            root.AddComponent<FossickMapStudioController>();
            root.AddComponent<FossickMapStudioView>();

            CreateCamera("MapStudioCamera");
            EditorSceneManager.SaveScene(scene, MapStudioScenePath);
        }

        private static void CreateCamera(string name)
        {
            var cameraObject = new GameObject(name);
            var camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.08f, 0.09f, 0.1f);
            camera.orthographic = true;
            camera.orthographicSize = 5f;
            cameraObject.transform.position = new Vector3(0f, 0f, -10f);
        }

        private static void AddScenesToBuildSettings()
        {
            var scenePaths = new HashSet<string>
            {
                PreviewScenePath,
                MapStudioScenePath
            };

            var scenes = new List<EditorBuildSettingsScene>();
            for (var i = 0; i < EditorBuildSettings.scenes.Length; i++)
            {
                var existing = EditorBuildSettings.scenes[i];
                scenes.Add(existing);
                scenePaths.Remove(existing.path);
            }

            foreach (var path in scenePaths)
            {
                scenes.Add(new EditorBuildSettingsScene(path, true));
            }

            EditorBuildSettings.scenes = scenes.ToArray();
        }

        private static void EnsureDirectory(string path)
        {
            if (string.IsNullOrEmpty(path) || Directory.Exists(path))
            {
                return;
            }

            Directory.CreateDirectory(path);
        }

        private static void AssertSceneComponent<T>(string scenePath, string objectName) where T : Component
        {
            var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            var roots = scene.GetRootGameObjects();
            for (var i = 0; i < roots.Length; i++)
            {
                var root = roots[i];
                if (root.name == objectName && root.GetComponent<T>() != null)
                {
                    return;
                }
            }

            throw new MissingComponentException($"{scenePath} is missing {typeof(T).Name} on {objectName}.");
        }
    }
}
