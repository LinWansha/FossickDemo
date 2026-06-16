using System.IO;
using Fossick.Core.Config;
using Fossick.Core.Serialization;

namespace Fossick.MapStudio.ImportExport
{
    public static class FossickMapFileService
    {
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
    }
}
