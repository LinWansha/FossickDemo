using System;
using System.Collections.Generic;
using Fossick.Core.Generation;
using Fossick.Core.Config;

namespace Fossick.Core.Save
{
    [Serializable]
    public sealed class FossickSaveState
    {
        public const int CurrentSchemaVersion = 1;

        public int schemaVersion = CurrentSchemaVersion;
        public int seed;
        public int loadedStartRow;
        public int topVisibleRow;
        public int depth;
        public FossickGenerationState generationState;
        public List<int> generatedFragmentIds = new List<int>();
        public List<FossickSavedMineRow> loadedRows = new List<FossickSavedMineRow>();
        public List<string> destroyedCells = new List<string>();
        public List<string> collectedCells = new List<string>();
        public List<string> visibleCells = new List<string>();
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

    [Serializable]
    public sealed class FossickSavedMineRow
    {
        public int rowIndex;
        public List<FossickSavedCellState> cells = new List<FossickSavedCellState>();
    }

    [Serializable]
    public sealed class FossickSavedCellState
    {
        public int x;
        public int y;
        public string backgroundId;
        public string rewardBackgroundId;
        public FossickTerrainType terrain;
        public int hp;
        public FossickElementConfig reward;
        public List<string> decorations = new List<string>();
        public FossickFogType fog;
        public bool collected;
        public bool generatedWithObstacle;
    }
}
