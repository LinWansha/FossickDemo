using System.Collections.Generic;
using Fossick.Core.Definition.Config;
using Fossick.Core.Generation;
using Fossick.Core.State;
using Fossick.Core.Mine.Objects;

namespace Fossick.Core.Mine
{
    public static class FossickRuntimeObjectFactory
    {
        public static FossickMine CreateMine(FossickMapConfig config, FossickGeneratedMine generatedMine)
        {
            var spec = config == null ? FossickBoardSpec.Default : config.BoardSpec;
            var mine = new FossickMine(spec);
            mine.AppendGeneratedMine(generatedMine);
            return mine;
        }

        public static FossickCell[] CreateEmptyRow(FossickBoardSpec spec, int absoluteRow)
        {
            var boardSpec = spec.IsValid ? spec : FossickBoardSpec.Default;
            var row = new FossickCell[boardSpec.width];
            for (var x = 0; x < boardSpec.width; x++)
            {
                row[x] = CreateEmptyCell(x, absoluteRow);
            }

            return row;
        }

        public static FossickCell CreateEmptyCell(int x, int absoluteRow)
        {
            var cell = new FossickCell(new FossickPosition(x, absoluteRow));
            cell.Fog = new FossickFogState(true);
            return cell;
        }

        public static FossickCell CreateCell(FossickCellConfig config, int x, int absoluteRow)
        {
            if (config == null)
            {
                return CreateEmptyCell(x, absoluteRow);
            }

            var position = new FossickPosition(x, absoluteRow);
            var cell = new FossickCell(position);
            cell.Fog = new FossickFogState(config.fog == FossickFogType.None);
            cell.BackgroundId = config.backgroundId;
            cell.RewardBackgroundId = config.rewardBackgroundId;

            if (config.terrain != FossickTerrainType.Empty)
            {
                cell.Terrain = new FossickTerrainInstance(config.terrain, config.hp, position);
            }

            var reward = FossickRewardPayload.FromConfig(config.reward);
            if (reward != null)
            {
                if (cell.HasObstacle)
                {
                    cell.FossickEmbeddedContent = FossickEmbeddedContent.FromPayload(reward, null, position);
                }
                else
                {
                    cell.SetPickup(FossickPickupEntity.FromPayload(reward, position));
                }
            }

            AddDecorations(cell, config.decorations, position);
            return cell;
        }

        private static void AddDecorations(FossickCell cell, List<string> decorationIds, FossickPosition position)
        {
            if (cell == null || decorationIds == null)
            {
                return;
            }

            for (var i = 0; i < decorationIds.Count; i++)
            {
                var id = decorationIds[i];
                if (!string.IsNullOrEmpty(id))
                {
                    cell.AddDecoration(new FossickDecorationObject(id, position));
                }
            }
        }

    }
}
