using Fossick.Core.Visual;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Fossick.MapStudio
{
    public static class FossickStandaloneMapEditorHost
    {
        private const string ArtCatalogResourcePath = "FossickArt/FossickArtCatalog";
        private const string MapStudioSceneName = "FossickMapStudio";
        private const string PreviewSceneName = "FossickPreview";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Configure()
        {
            FossickArtLibrary.SetCatalogLoader(LoadArtCatalog);
            FossickArtLibrary.SetActiveCatalog(LoadArtCatalog());
            FossickMapEditorBridge.Open(string.Empty);
            ConfigureBindings();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsureSceneBindings()
        {
            ConfigureBindings();
        }

        private static void ConfigureBindings()
        {
            FossickMapEditorBridge.ExitEditor = ExitEditor;
            FossickMapEditorBridge.PlayOfficialMap = PlayOfficialMap;
        }

        private static FossickArtCatalog LoadArtCatalog()
        {
            return Resources.Load<FossickArtCatalog>(ArtCatalogResourcePath);
        }

        private static void ExitEditor()
        {
            if (SceneManager.GetActiveScene().name == MapStudioSceneName)
            {
                Application.Quit();
            }
        }

        private static void PlayOfficialMap()
        {
            SceneManager.LoadScene(PreviewSceneName);
        }
    }
}
