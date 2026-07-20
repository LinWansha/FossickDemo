using Fossick.Core.Definition.Config;
using Fossick.Core.Definition.Serialization;

namespace Fossick.MapStudio.ImportExport
{
    public static class FossickMapFileService
    {
        public const string FragmentLibraryFileName = FossickMapProjectFileService.FragmentLibraryFileName;
        public const string GenerationRulesFileName = FossickMapProjectFileService.GenerationRulesFileName;
        public const string MapDefinitionFileName = FossickMapProjectFileService.MapDefinitionFileName;

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
