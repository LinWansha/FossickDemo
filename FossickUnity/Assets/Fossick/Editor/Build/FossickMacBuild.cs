using System.IO;
using Fossick.Core.Serialization;
using UnityEditor;
using UnityEngine;

namespace Fossick.Editor.Build
{
    public static class FossickMacBuild
    {
        private const string BuildDirectory = "../Builds/Mac/FossickMapStudioPlaytest";
        private const string AppName = "FossickMapStudio.app";
        private const string MapStudioScenePath = "Assets/Fossick/MapStudio/Scenes/FossickMapStudio.unity";
        private const string PreviewScenePath = "Assets/Fossick/Preview/Scenes/FossickPreview.unity";
        private const string SourceMapsPath = "Assets/Fossick/MapStudio/Maps";

        [MenuItem("Fossick/Build/Mac Playtest App")]
        public static void BuildPlaytestMac()
        {
            var projectRoot = Directory.GetParent(Application.dataPath).FullName;
            var outputDirectory = Path.GetFullPath(Path.Combine(projectRoot, BuildDirectory));
            var appPath = Path.Combine(outputDirectory, AppName);
            var previousCompanyName = PlayerSettings.companyName;
            var previousProductName = PlayerSettings.productName;
            var previousFullScreenMode = PlayerSettings.fullScreenMode;
            var previousDefaultScreenWidth = PlayerSettings.defaultScreenWidth;
            var previousDefaultScreenHeight = PlayerSettings.defaultScreenHeight;
            var previousResizableWindow = PlayerSettings.resizableWindow;

            if (Directory.Exists(outputDirectory))
            {
                Directory.Delete(outputDirectory, true);
            }

            Directory.CreateDirectory(outputDirectory);

            UnityEditor.Build.Reporting.BuildReport report;
            try
            {
                CopyMapFilesToStreamingAssets(projectRoot);
                AssetDatabase.Refresh();

                PlayerSettings.companyName = "Magic Tavern";
                PlayerSettings.productName = "Fossick MapStudio";
                PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
                PlayerSettings.defaultScreenWidth = 1440;
                PlayerSettings.defaultScreenHeight = 900;
                PlayerSettings.resizableWindow = true;
                report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
                {
                    scenes = new[]
                    {
                        MapStudioScenePath,
                        PreviewScenePath
                    },
                    locationPathName = appPath,
                    target = BuildTarget.StandaloneOSX,
                    options = BuildOptions.None
                });
            }
            finally
            {
                PlayerSettings.companyName = previousCompanyName;
                PlayerSettings.productName = previousProductName;
                PlayerSettings.fullScreenMode = previousFullScreenMode;
                PlayerSettings.defaultScreenWidth = previousDefaultScreenWidth;
                PlayerSettings.defaultScreenHeight = previousDefaultScreenHeight;
                PlayerSettings.resizableWindow = previousResizableWindow;
            }

            if (report.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
            {
                throw new System.Exception($"Fossick macOS build failed: {report.summary.result}");
            }

            Debug.Log($"Fossick macOS playtest app built: {appPath}");
        }

        private static void CopyMapFilesToStreamingAssets(string projectRoot)
        {
            var source = Path.Combine(projectRoot, SourceMapsPath);
            var target = Path.Combine(Application.streamingAssetsPath, FossickMapProjectFileService.RelativeMapsFolder);
            Directory.CreateDirectory(target);

            CopyJson(source, target, "FossickFragmentLibrary.json");
            CopyJson(source, target, "FossickGenerationRules.json");
            CopyJson(source, target, "FossickMapDefinition.json");
        }

        private static void CopyJson(string sourceDirectory, string targetDirectory, string fileName)
        {
            var source = Path.Combine(sourceDirectory, fileName);
            if (!File.Exists(source))
            {
                throw new FileNotFoundException($"Missing Fossick map file: {source}");
            }

            File.Copy(source, Path.Combine(targetDirectory, fileName), true);
        }
    }
}
