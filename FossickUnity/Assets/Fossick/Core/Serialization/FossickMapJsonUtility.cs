using Fossick.Core.Config;
using UnityEngine;

namespace Fossick.Core.Serialization
{
    public static class FossickMapJsonUtility
    {
        public static FossickMapConfig FromJson(string json)
        {
            return JsonUtility.FromJson<FossickMapConfig>(json);
        }

        public static string ToJson(FossickMapConfig config, bool prettyPrint = true)
        {
            return JsonUtility.ToJson(config, prettyPrint);
        }
    }
}
