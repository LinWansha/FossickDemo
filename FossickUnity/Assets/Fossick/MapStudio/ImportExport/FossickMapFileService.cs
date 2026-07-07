using System.IO;
using Fossick.Core.Definition.Config;
using Fossick.Core.Definition.Serialization;

namespace Fossick.MapStudio.ImportExport
{
    public static class FossickMapFileService
    {
        public const string FragmentLibraryFileName = FossickMapProjectFileService.FragmentLibraryFileName;
        public const string GenerationRulesFileName = FossickMapProjectFileService.GenerationRulesFileName;
        public const string MapDefinitionFileName = FossickMapProjectFileService.MapDefinitionFileName;

        public static FossickMapConfig Load(string path)
        {
            var json = File.ReadAllText(path);
            return FossickMapJsonUtility.FromJson(json);
        }

        public static void Save(string path, FossickMapConfig config)
        {
            var json = FossickMapJsonUtility.ToJson(config);
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllText(path, json);
        }

        public static FossickMapProjectConfig LoadSplitProject(string folder)
        {
            return FossickMapProjectFileService.LoadSplitProject(folder);
        }

        public static void SaveSplitProject(string folder, FossickMapProjectConfig project)
        {
            FossickMapProjectFileService.SaveSplitProject(folder, project);
        }
    }
}
