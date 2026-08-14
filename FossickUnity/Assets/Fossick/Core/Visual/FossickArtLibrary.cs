using System;
using System.Collections.Generic;
using Fossick.Core.Definition.Config;
using Fossick.Core.Mine.Objects;
using Fossick.Core.Visual.Tiling;
using UnityEngine;

namespace Fossick.Core.Visual
{
    public static class FossickArtLibrary
    {
        private static FossickArtCatalog activeCatalog;
        private static Func<FossickArtCatalog> catalogLoader;

        public static FossickArtCatalog ActiveCatalog
        {
            get
            {
                if (activeCatalog == null && catalogLoader != null)
                {
                    activeCatalog = catalogLoader();
                }

                return activeCatalog;
            }
        }

        public static void SetCatalogLoader(Func<FossickArtCatalog> loader)
        {
            catalogLoader = loader;
        }

        public static void SetActiveCatalog(FossickArtCatalog catalog)
        {
            activeCatalog = catalog;
        }

        public static bool HasAutoTileSprites(FossickTerrainType terrain)
        {
            return ActiveCatalog != null && ActiveCatalog.HasAutoTileSprites(terrain);
        }

        public static Sprite GetAutoTileSprite(FossickTerrainType terrain, int assetIndex)
        {
            if (assetIndex <= 0 || ActiveCatalog == null)
            {
                return null;
            }

            return ActiveCatalog.GetAutoTileSprite(terrain, assetIndex);
        }

        public static Sprite GetTerrainSprite(FossickTerrainType terrain, string id = null)
        {
            return ActiveCatalog == null ? null : ActiveCatalog.GetTerrainSprite(terrain, id);
        }

        public static Sprite GetFogAutoTileSprite(int assetIndex)
        {
            if (assetIndex <= 0 || ActiveCatalog == null)
            {
                return null;
            }

            return ActiveCatalog.GetFogAutoTileSprite(assetIndex);
        }

        public static int ResolveConfigCornerAssetIndex(IReadOnlyList<FossickCellConfig[]> rows, int cornerX, int cornerY, FossickTerrainType terrain)
        {
            return FossickAutoTileResolver.ResolveConfigCornerAssetIndex(rows, cornerX, cornerY, terrain);
        }

        public static Sprite GetEntitySprite(FossickElementConfig entity)
        {
            return ActiveCatalog == null ? null : ActiveCatalog.GetEntitySprite(entity);
        }

        public static Sprite GetTerrainAttachmentSprite(FossickElementConfig entity, FossickTerrainType terrain)
        {
            return ActiveCatalog == null ? null : ActiveCatalog.GetTerrainAttachmentSprite(entity, terrain);
        }

        public static Sprite GetToolSprite(FossickToolType toolType)
        {
            return ActiveCatalog == null ? null : ActiveCatalog.GetToolSprite(toolType);
        }

        public static Sprite GetBackgroundSprite(string id)
        {
            return ActiveCatalog == null ? null : ActiveCatalog.GetBackgroundSprite(id);
        }

        public static Sprite GetRewardBackgroundSprite(string id)
        {
            return ActiveCatalog == null ? null : ActiveCatalog.GetRewardBackgroundSprite(id);
        }

        public static Sprite GetDecorationSprite(string id)
        {
            return ActiveCatalog == null ? null : ActiveCatalog.GetDecorationSprite(id);
        }

        public static Sprite GetCollectionSprite(string spriteName)
        {
            return ActiveCatalog == null ? null : ActiveCatalog.GetCollectionSprite(spriteName);
        }

        public static Color GetEmptyCellColor()
        {
            return ActiveCatalog == null ? new Color(0.1f, 0.18f, 0.18f) : ActiveCatalog.emptyCellColor;
        }

        public static Color GetFogColor()
        {
            return new Color(1f, 1f, 1f, 1f);
        }

        public static Color GetEffectHighlightColor()
        {
            return ActiveCatalog == null
                ? new Color(0.32f, 0.9f, 0.48f, 0.95f)
                : ActiveCatalog.effectHighlightColor;
        }

        public static List<string> ValidateRequiredSprites()
        {
            var issues = new List<string>();
            ValidateAutoTileSet(issues, FossickTerrainType.Dirt, 18, "土块四方连续");
            ValidateAutoTileSet(issues, FossickTerrainType.Stone, 19, "石头四方连续");
            ValidateAutoTileSet(issues, FossickTerrainType.Unbreakable, 18, "基岩四方连续");
            ValidateTerrainSprite(issues, FossickTerrainType.Explosives, FossickExplosivesTerrain.Id, "炸药箱");
            ValidateFogAutoTileSet(issues, 15);
            ValidateEntitySprite(issues, FossickElementType.Coin, FossickContentIds.Reward.CoinDropSmall, "小金币掉落实体");
            ValidateEntitySprite(issues, FossickElementType.Coin, FossickContentIds.Reward.CoinDropMedium, "中金币掉落实体");
            ValidateEntitySprite(issues, FossickElementType.Coin, FossickContentIds.Reward.CoinDropLarge, "大金币掉落实体");
            ValidateEntitySprite(issues, FossickElementType.Coin, FossickContentIds.Reward.CoinPileSmall, "小金币堆实体");
            ValidateEntitySprite(issues, FossickElementType.Coin, FossickContentIds.Reward.CoinPileLarge, "大金币堆实体");
            ValidateEntitySprite(issues, FossickElementType.Ore, FossickContentIds.Reward.OreCopper, "铜矿实体");
            ValidateEntitySprite(issues, FossickElementType.Ore, FossickContentIds.Reward.OreGem, "宝石矿实体");
            ValidateEntitySprite(issues, FossickElementType.Ore, FossickContentIds.Reward.OreGold, "金矿实体");
            ValidateEntitySprite(issues, FossickElementType.Ore, FossickContentIds.Reward.OreSilver, "银矿实体");
            ValidateEntitySprite(issues, FossickElementType.Item, FossickContentIds.Tool.Pickaxe, "矿镐实体");
            ValidateEntitySprite(issues, FossickElementType.Item, FossickContentIds.Tool.Dynamite, "炸药实体");
            ValidateEntitySprite(issues, FossickElementType.Item, FossickContentIds.Tool.Tnt, "雷管实体");
            ValidateEntitySprite(issues, FossickElementType.Item, FossickContentIds.Tool.Radar, "雷达实体");
            ValidateEntitySprite(issues, FossickElementType.Collection, FossickContentIds.Reward.CollectionBox, "收藏品箱实体");
            ValidateEntitySprite(issues, FossickElementType.Chest, FossickContentIds.Reward.TreasureChest, "奖励宝箱实体");
            ValidateEntitySprite(issues, FossickElementType.Chest, FossickContentIds.Reward.MessageBottle, "漂流瓶实体");
            ValidateTerrainAttachment(issues, FossickElementType.Ore, FossickContentIds.Reward.OreCopper, FossickTerrainType.Dirt, "铜矿土块附着图");
            ValidateTerrainAttachment(issues, FossickElementType.Ore, FossickContentIds.Reward.OreCopper, FossickTerrainType.Stone, "铜矿石块附着图");
            ValidateTerrainAttachment(issues, FossickElementType.Ore, FossickContentIds.Reward.OreGem, FossickTerrainType.Dirt, "宝石矿土块附着图");
            ValidateTerrainAttachment(issues, FossickElementType.Ore, FossickContentIds.Reward.OreGem, FossickTerrainType.Stone, "宝石矿石块附着图");
            ValidateTerrainAttachment(issues, FossickElementType.Ore, FossickContentIds.Reward.OreGold, FossickTerrainType.Dirt, "金矿土块附着图");
            ValidateTerrainAttachment(issues, FossickElementType.Ore, FossickContentIds.Reward.OreGold, FossickTerrainType.Stone, "金矿石块附着图");
            ValidateTerrainAttachment(issues, FossickElementType.Ore, FossickContentIds.Reward.OreSilver, FossickTerrainType.Dirt, "银矿土块附着图");
            ValidateTerrainAttachment(issues, FossickElementType.Ore, FossickContentIds.Reward.OreSilver, FossickTerrainType.Stone, "银矿石块附着图");
            ValidateTerrainAttachment(issues, FossickElementType.Item, FossickContentIds.Tool.Pickaxe, FossickTerrainType.Dirt, "矿镐土块附着图");
            ValidateTerrainAttachment(issues, FossickElementType.Item, FossickContentIds.Tool.Pickaxe, FossickTerrainType.Stone, "矿镐石块附着图");
            ValidateTerrainAttachment(issues, FossickElementType.Item, FossickContentIds.Tool.Dynamite, FossickTerrainType.Dirt, "炸药土块附着图");
            ValidateTerrainAttachment(issues, FossickElementType.Item, FossickContentIds.Tool.Dynamite, FossickTerrainType.Stone, "炸药石块附着图");
            ValidateTerrainAttachment(issues, FossickElementType.Item, FossickContentIds.Tool.Tnt, FossickTerrainType.Dirt, "雷管土块附着图");
            ValidateTerrainAttachment(issues, FossickElementType.Item, FossickContentIds.Tool.Tnt, FossickTerrainType.Stone, "雷管石块附着图");
            ValidateTerrainAttachment(issues, FossickElementType.Item, FossickContentIds.Tool.Radar, FossickTerrainType.Dirt, "雷达土块附着图");
            ValidateTerrainAttachment(issues, FossickElementType.Item, FossickContentIds.Tool.Radar, FossickTerrainType.Stone, "雷达石块附着图");
            ValidateTerrainAttachment(issues, FossickElementType.Collection, FossickContentIds.Reward.CollectionBox, FossickTerrainType.Dirt, "收藏品箱土块附着图");
            ValidateToolSprite(issues, FossickToolType.Pickaxe, "矿镐道具图");
            ValidateToolSprite(issues, FossickToolType.Dynamite, "炸药道具图");
            ValidateToolSprite(issues, FossickToolType.Tnt, "雷管道具图");
            ValidateToolSprite(issues, FossickToolType.Radar, "雷达道具图");
            ValidateBackground(issues, FossickContentIds.Background.MineDefault, "默认矿井背景");
            ValidateBackground(issues, FossickContentIds.Background.MineMap, "矿井地图背景");
            ValidateBackground(issues, FossickContentIds.Background.MineVariant, "矿井变化背景");
            ValidateRewardBackground(issues, FossickContentIds.RewardBackground.TreasureRoomSmall, "藏宝阁 3x2 背景");
            ValidateRewardBackground(issues, FossickContentIds.RewardBackground.TreasureRoomMedium, "藏宝阁 5x2 背景");
            ValidateRewardBackground(issues, FossickContentIds.RewardBackground.TreasureRoomLarge, "藏宝阁 7x2 背景");
            ValidateDecoration(issues, FossickContentIds.Decoration.GrassLarge, "大草装饰");
            ValidateDecoration(issues, FossickContentIds.Decoration.GrassSmall, "小草装饰");
            ValidateDecoration(issues, FossickContentIds.Decoration.Mushroom, "蘑菇装饰");
            return issues;
        }

        private static void ValidateAutoTileSet(List<string> issues, FossickTerrainType terrain, int requiredMaxIndex, string label)
        {
            for (var i = 1; i <= requiredMaxIndex; i++)
            {
                if (GetAutoTileSprite(terrain, i) == null)
                {
                    issues.Add($"{label}缺少索引 {i}。");
                }
            }
        }

        private static void ValidateFogAutoTileSet(List<string> issues, int requiredMaxIndex)
        {
            for (var i = 1; i <= requiredMaxIndex; i++)
            {
                if (GetFogAutoTileSprite(i) == null)
                {
                    issues.Add($"阴影四方连续缺少索引 {i}。");
                }
            }
        }

        private static void ValidateTerrainSprite(List<string> issues, FossickTerrainType terrain, string id, string label)
        {
            if (GetTerrainSprite(terrain, id) == null)
            {
                issues.Add($"{label}缺少配置。");
            }
        }

        private static void ValidateEntitySprite(List<string> issues, FossickElementType type, string id, string label)
        {
            if (GetEntitySprite(new FossickElementConfig { type = type, id = id }) == null)
            {
                issues.Add($"{label}缺少配置。");
            }
        }

        private static void ValidateTerrainAttachment(List<string> issues, FossickElementType type, string id, FossickTerrainType terrain, string label)
        {
            if (GetTerrainAttachmentSprite(new FossickElementConfig { type = type, id = id }, terrain) == null)
            {
                issues.Add($"{label}缺少配置。");
            }
        }

        private static void ValidateToolSprite(List<string> issues, FossickToolType toolType, string label)
        {
            if (GetToolSprite(toolType) == null)
            {
                issues.Add($"{label}缺少配置。");
            }
        }

        private static void ValidateBackground(List<string> issues, string id, string label)
        {
            if (GetBackgroundSprite(id) == null)
            {
                issues.Add($"{label}缺少配置。");
            }
        }

        private static void ValidateRewardBackground(List<string> issues, string id, string label)
        {
            if (GetRewardBackgroundSprite(id) == null)
            {
                issues.Add($"{label}缺少配置。");
            }
        }

        private static void ValidateDecoration(List<string> issues, string id, string label)
        {
            if (GetDecorationSprite(id) == null)
            {
                issues.Add($"{label}缺少配置。");
            }
        }

    }
}
