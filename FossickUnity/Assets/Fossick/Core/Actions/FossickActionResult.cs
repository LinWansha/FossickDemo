using System.Collections.Generic;
using Fossick.Core.Config;

namespace Fossick.Core.Actions
{
    public sealed class FossickActionResult
    {
        public FossickToolType toolType;
        public int targetX;
        public int targetY;
        public bool isApplied;
        public bool isCollectOnly;
        public bool toolConsumed;
        public string invalidReason;
        public readonly List<FossickActionStep> steps = new List<FossickActionStep>();
        public readonly List<FossickCellDelta> cellDeltas = new List<FossickCellDelta>();
        public readonly List<FossickRewardEvent> rewards = new List<FossickRewardEvent>();
        public bool scrolled;
        public int scrollCount;
        public int depthBeforeAction;
        public int depthAfterAction;
    }

    public enum FossickActionStepType
    {
        InvalidTarget = 0,
        ToolConsumed = 1,
        ObstacleHit = 2,
        ObstacleBroken = 3,
        RewardRevealed = 4,
        RewardCollected = 5,
        BoardScrolled = 6,
        FogRevealed = 7,
        RadarScanned = 8,
        RewardAutoCollected = 9,
        RewardMissed = 10
    }

    public sealed class FossickActionStep
    {
        public FossickActionStepType type;
        public int x;
        public int y;
        public FossickElementType elementType;
        public string id;
        public int amount;
    }

    public sealed class FossickToolTarget
    {
        public int x;
        public int y;
    }

    public sealed class FossickCellDelta
    {
        public int x;
        public int y;
        public FossickTerrainType terrainBefore;
        public FossickTerrainType terrainAfter;
        public int hpBefore;
        public int hpAfter;
        public FossickFogType fogBefore;
        public FossickFogType fogAfter;
        public bool rewardRevealed;
        public bool elementRevealed;
        public bool rewardCollected;
    }

    public sealed class FossickRewardEvent
    {
        public FossickElementType elementType;
        public string id;
        public int amount;
        public int x;
        public int y;
    }
}
