using Fossick.Core.Config;

namespace Fossick.Core.Board
{
    public sealed class FossickCellState
    {
        public int x;
        public int y;
        public string backgroundId;
        public string rewardBackgroundId;
        public FossickTerrainType terrain;
        public int hp;
        public FossickElementConfig reward;
        public string[] decorations;
        public FossickFogType fog;
        public bool collected;
        public bool generatedWithObstacle;

        public bool HasObstacle => terrain != FossickTerrainType.Empty;
        public bool IsBreakable => terrain != FossickTerrainType.Empty && terrain != FossickTerrainType.Unbreakable && hp > 0;
        public bool HasCollectableReward => reward != null && reward.type != FossickElementType.None && !collected;
        public bool HasTerrainAttachedReward => HasCollectableReward && HasObstacle;
        public bool HasSpawnedReward => HasCollectableReward && !HasObstacle;
        public bool HasBuriedReward => HasTerrainAttachedReward;
        public bool HasRewardOverlay => HasTerrainAttachedReward || HasSpawnedReward;
        public bool HasCollectableElement => HasCollectableReward;
        public bool IsContentVisible => fog == FossickFogType.None;

        public FossickCellState Clone()
        {
            return (FossickCellState)MemberwiseClone();
        }
    }
}
