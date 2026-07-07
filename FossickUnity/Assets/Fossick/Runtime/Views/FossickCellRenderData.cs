using System.Collections.Generic;
using Fossick.Core.Definition.Config;
using Fossick.Core.Mine;
using Fossick.Core.Mine.Objects;

namespace Fossick.Runtime.Views
{
    public sealed class FossickCellRenderData
    {
        private FossickCellRenderData()
        {
        }

        public int x;
        public int y;
        public string backgroundId;
        public string rewardBackgroundId;
        public FossickTerrainType terrain;
        public int hp;
        public FossickRewardPayload pickupPayload;
        public FossickRewardPayload embeddedPayload;
        public bool isContentVisible;
        public string[] decorations;

        public bool HasSpawnedReward => pickupPayload != null;
        public bool HasTerrainAttachedReward => embeddedPayload != null && terrain != FossickTerrainType.Empty;
        public bool HasCollectableReward => pickupPayload != null;
        public bool IsFogged => !isContentVisible;

        public static FossickCellRenderData FromCell(FossickCell cell)
        {
            if (cell == null)
            {
                return null;
            }

            return new FossickCellRenderData
            {
                x = cell.Position.x,
                y = cell.Position.y,
                backgroundId = cell.BackgroundId,
                rewardBackgroundId = cell.RewardBackgroundId,
                terrain = cell.Terrain == null ? FossickTerrainType.Empty : cell.Terrain.Terrain,
                hp = cell.Terrain == null ? 0 : cell.Terrain.Hp,
                pickupPayload = cell.HasCollectablePickup ? cell.Pickup.Payload : null,
                embeddedPayload = cell.FossickEmbeddedContent == null ? null : cell.FossickEmbeddedContent.Payload,
                isContentVisible = cell.IsVisible,
                decorations = ToDecorationIds(cell.Decorations)
            };
        }

        private static string[] ToDecorationIds(IReadOnlyList<FossickDecorationObject> source)
        {
            if (source == null || source.Count == 0)
            {
                return new string[0];
            }

            var ids = new string[source.Count];
            for (var i = 0; i < source.Count; i++)
            {
                ids[i] = source[i] == null ? null : source[i].DecorationId;
            }

            return ids;
        }
    }
}
