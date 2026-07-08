using System;
using System.Collections.Generic;
using Fossick.Core.Definition.Config;

namespace Fossick.Core.Data
{
    [Serializable]
    public sealed class FossickCellData
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
