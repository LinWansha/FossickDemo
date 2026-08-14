using System.Collections.Generic;
using Fossick.Core.Definition.Config;
using Fossick.Core.Application.Events;
using Fossick.Core.Mine.Objects;

namespace Fossick.Core.Application.Results
{
    public sealed class FossickActionResult
    {
        public FossickToolType toolType;
        public int targetX;
        public int targetY;
        public bool isApplied;
        public bool isCollectOnly;
        public bool toolConsumed;
        public bool countsForSettlementToolUsage;
        public string invalidReason;
        public readonly List<FossickActionStep> steps = new List<FossickActionStep>();
        public readonly List<FossickToolTarget> affectedCells = new List<FossickToolTarget>();
        public readonly List<FossickCellDelta> cellDeltas = new List<FossickCellDelta>();
        public readonly List<FossickEntityDrop> entityDrops = new List<FossickEntityDrop>();
        public readonly List<FossickRewardEvent> rewards = new List<FossickRewardEvent>();
        public readonly List<FossickDomainEvent> domainEvents = new List<FossickDomainEvent>();
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
        EntityRevealed = 4,
        EntityCollected = 5,
        MineScrolled = 6,
        FogRevealed = 7,
        RadarScanned = 8,
        EntityAutoCollected = 9,
        EntityMissed = 10
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
        public FossickCellDeltaSource source;
        public int sourceX;
        public int sourceY;
        public FossickTerrainType terrainBefore;
        public FossickTerrainType terrainAfter;
        public int hpBefore;
        public int hpAfter;
        public bool hasSupportBelowBefore;
        public FossickFogType fogBefore;
        public FossickFogType fogAfter;
    }

    public enum FossickCellDeltaSource
    {
        Tool = 0,
        ExplosiveCrate = 1
    }

    public sealed class FossickEntityDrop
    {
        public FossickPickupEntity entity;
        public int fromX;
        public int fromY;
        public int toX;
        public int toY;
        public FossickElementType elementType;
        public string id;
    }

    public sealed class FossickRewardEvent
    {
        public FossickElementType elementType;
        public string id;
        public string resolvedId;
        public int amount;
        public int x;
        public int y;
    }
}
