using System.Collections.Generic;
using System;
using Fossick.Core.Definition.Config;
using Fossick.Core.Generation;
using Fossick.Core.Data;
using Fossick.Core.Mine.Objects;

namespace Fossick.Core.Mine
{
    public static class FossickRuntimeObjectFactory
    {
        public static FossickMine CreateMine(
            FossickMapConfig config,
            FossickGeneratedMine generatedMine,
            IFossickRewardProvider rewardProvider)
        {
            var mine = new FossickMine(
                config.BoardSpec,
                new FossickBackgroundLayout(generatedMine.seed, config.visual),
                rewardProvider);
            mine.AppendGeneratedMine(generatedMine);
            return mine;
        }

        public static FossickCell[] CreateEmptyRow(FossickBoardSpec spec, int absoluteRow)
        {
            var row = new FossickCell[spec.width];
            for (var x = 0; x < spec.width; x++)
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

        public static FossickCell CreateCell(
            FossickCellConfig config,
            int x,
            int absoluteRow,
            IFossickRewardProvider rewardProvider)
        {
            if (config == null)
            {
                return CreateEmptyCell(x, absoluteRow);
            }

            var position = new FossickPosition(x, absoluteRow);
            var cell = new FossickCell(position);
            cell.Fog = new FossickFogState(config.fog == FossickFogType.None);

            if (config.terrain != FossickTerrainType.Empty)
            {
                cell.Terrain = FossickTerrainFactory.Create(config.terrain, position);
            }

            var rewardConfig = ResolveMapReward(config.reward, rewardProvider);
            var entityPayload = FossickEntityPayload.FromConfig(rewardConfig, rewardProvider);
            if (entityPayload != null)
            {
                if (cell.HasObstacle)
                {
                    cell.FossickEmbeddedContent = FossickEmbeddedContent.FromPayload(entityPayload, null, position);
                }
                else
                {
                    cell.SetPickup(FossickPickupEntity.FromPayload(entityPayload, position));
                }
            }

            AddDecorations(cell, config.decorations, position);
            return cell;
        }

        public static FossickCell CreateCell(
            FossickCellData data,
            int x,
            int absoluteRow,
            IFossickRewardProvider rewardProvider)
        {
            var config = new FossickCellConfig
            {
                x = data.x,
                y = data.y,
                terrain = data.terrain,
                reward = data.collected ? null : data.reward,
                decorations = data.decorations,
                fog = data.fog
            };
            var cell = CreateCell(config, x, absoluteRow, rewardProvider);
            if (cell.Terrain != null)
            {
                cell.Terrain = FossickTerrainFactory.Create(data.terrain, data.hp, cell.Position);
            }

            return cell;
        }

        private static FossickElementConfig ResolveMapReward(
            FossickElementConfig reward,
            IFossickRewardProvider rewardProvider)
        {
            if (reward == null || reward.type != FossickElementType.Coin || !FossickContentIds.Reward.IsCoinDropPlaceholder(reward.id))
            {
                return reward;
            }

            return new FossickElementConfig
            {
                type = FossickElementType.Coin,
                id = rewardProvider.PickCoinDropId()
            };
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
