using System.Collections.Generic;
using Fossick.Core.Actions;
using Fossick.Core.Config;

namespace Fossick.Core.Presentation
{
    public sealed class FossickPresentationPlan
    {
        public FossickToolType toolType;
        public int targetX;
        public int targetY;
        public bool isApplied;
        public bool isCollectOnly;
        public bool toolConsumed;
        public string invalidReason;
        public int depthBeforeAction;
        public int depthAfterAction;
        public int totalScrollRows;
        public readonly List<FossickPresentationEvent> events = new List<FossickPresentationEvent>();
    }

    public sealed class FossickPresentationEvent
    {
        public FossickPresentationEventType type;
        public FossickActionStepType sourceStepType;
        public FossickToolType toolType;
        public int x;
        public int y;
        public FossickElementType elementType;
        public string id;
        public int amount;
        public FossickTerrainType terrainBefore;
        public FossickTerrainType terrainAfter;
        public int hpBefore;
        public int hpAfter;
        public FossickFogType fogBefore;
        public FossickFogType fogAfter;
        public int scrollRows;
        public int depthBefore;
        public int depthAfter;
    }

    public enum FossickPresentationEventType
    {
        InvalidTarget = 0,
        ToolConsumed = 1,
        ObstacleHit = 2,
        ObstacleBroken = 3,
        RewardSpawned = 4,
        RewardCollected = 5,
        RewardAutoCollected = 6,
        RewardMissed = 7,
        FogRevealed = 8,
        RadarScanned = 9,
        BoardScrolled = 10
    }
}
