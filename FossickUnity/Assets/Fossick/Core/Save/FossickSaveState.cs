using System;
using System.Collections.Generic;

namespace Fossick.Core.Save
{
    [Serializable]
    public sealed class FossickSaveState
    {
        public const int CurrentSchemaVersion = 1;

        public int schemaVersion = CurrentSchemaVersion;
        public int seed;
        public int topVisibleRow;
        public int depth;
        public List<int> generatedFragmentIds = new List<int>();
        public List<string> destroyedCells = new List<string>();
        public List<string> collectedCells = new List<string>();
        public List<string> visibleCells = new List<string>();
        public List<string> pendingRewards = new List<string>();
        public int oreFound;
        public int collectionFound;
        public int toolUsed;
        public int pickaxes;
        public int dynamite;
        public int tnt;
        public int radar;
        public int score;
        public int coins;
        public List<string> collectionItems = new List<string>();
    }
}
