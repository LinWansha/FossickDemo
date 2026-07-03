using System.Collections.Generic;
using Fossick.Core.Board;
using Fossick.Core.Config;
using UnityEngine;

namespace Fossick.Core.Visual
{
    public static class FossickArtLibrary
    {
        private const string DefaultCatalogPath = "FossickArt/FossickArtCatalog";

        private static FossickArtCatalog activeCatalog;

        public static FossickArtCatalog ActiveCatalog
        {
            get
            {
                if (activeCatalog == null)
                {
                    activeCatalog = Resources.Load<FossickArtCatalog>(DefaultCatalogPath);
                }

                return activeCatalog;
            }
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

        public static Sprite GetFogAutoTileSprite(int assetIndex)
        {
            if (assetIndex <= 0 || ActiveCatalog == null)
            {
                return null;
            }

            return ActiveCatalog.GetFogAutoTileSprite(assetIndex);
        }

        public static int ResolveRuntimeCornerAssetIndex(IReadOnlyList<FossickCellState[]> rows, int cornerX, int cornerY, FossickTerrainType terrain)
        {
            if (rows == null || terrain == FossickTerrainType.Empty)
            {
                return 0;
            }

            var mask = 0;
            if (RuntimeCellMatches(rows, cornerX - 1, cornerY - 1, terrain))
            {
                mask |= 1;
            }

            if (RuntimeCellMatches(rows, cornerX, cornerY - 1, terrain))
            {
                mask |= 2;
            }

            if (RuntimeCellMatches(rows, cornerX - 1, cornerY, terrain))
            {
                mask |= 4;
            }

            if (RuntimeCellMatches(rows, cornerX, cornerY, terrain))
            {
                mask |= 8;
            }

            return MapCornerMaskToSpriteIndex(mask);
        }

        public static int ResolveConfigCornerAssetIndex(IReadOnlyList<FossickCellConfig[]> rows, int cornerX, int cornerY, FossickTerrainType terrain)
        {
            if (rows == null || terrain == FossickTerrainType.Empty)
            {
                return 0;
            }

            var mask = 0;
            if (ConfigCellMatches(rows, cornerX - 1, cornerY - 1, terrain))
            {
                mask |= 1;
            }

            if (ConfigCellMatches(rows, cornerX, cornerY - 1, terrain))
            {
                mask |= 2;
            }

            if (ConfigCellMatches(rows, cornerX - 1, cornerY, terrain))
            {
                mask |= 4;
            }

            if (ConfigCellMatches(rows, cornerX, cornerY, terrain))
            {
                mask |= 8;
            }

            return MapCornerMaskToSpriteIndex(mask);
        }

        public static int ResolveRuntimeFogCornerAssetIndex(IReadOnlyList<FossickCellState[]> rows, int cornerX, int cornerY)
        {
            if (rows == null)
            {
                return 0;
            }

            var mask = 0;
            if (RuntimeCellIsFogged(rows, cornerX - 1, cornerY - 1))
            {
                mask |= 1;
            }

            if (RuntimeCellIsFogged(rows, cornerX, cornerY - 1))
            {
                mask |= 2;
            }

            if (RuntimeCellIsFogged(rows, cornerX - 1, cornerY))
            {
                mask |= 4;
            }

            if (RuntimeCellIsFogged(rows, cornerX, cornerY))
            {
                mask |= 8;
            }

            return MapCornerMaskToSpriteIndex(mask);
        }

        public static Sprite GetRewardSprite(FossickElementConfig reward)
        {
            return ActiveCatalog == null ? null : ActiveCatalog.GetRewardSprite(reward);
        }

        public static Sprite GetTerrainAttachmentSprite(FossickElementConfig reward, FossickTerrainType terrain)
        {
            return ActiveCatalog == null ? null : ActiveCatalog.GetTerrainAttachmentSprite(reward, terrain);
        }

        public static Sprite GetToolSprite(FossickToolType toolType)
        {
            return ActiveCatalog == null ? null : ActiveCatalog.GetToolSprite(toolType);
        }

        public static Sprite GetBackgroundSprite(string id)
        {
            return ActiveCatalog == null ? null : ActiveCatalog.GetBackgroundSprite(id);
        }

        public static Sprite GetDecorationSprite(string id)
        {
            return ActiveCatalog == null ? null : ActiveCatalog.GetDecorationSprite(id);
        }

        public static Color GetEmptyCellColor()
        {
            return ActiveCatalog == null ? new Color(0.1f, 0.18f, 0.18f) : ActiveCatalog.emptyCellColor;
        }

        public static Color GetFogColor()
        {
            return new Color(1f, 1f, 1f, 1f);
        }

        public static Color GetPreviewColor()
        {
            return ActiveCatalog == null ? new Color(0.32f, 0.9f, 0.48f, 0.95f) : ActiveCatalog.previewColor;
        }

        public static List<string> ValidateRequiredSprites()
        {
            var issues = new List<string>();
            ValidateAutoTileSet(issues, FossickTerrainType.Dirt, 18, "土块四方连续");
            ValidateAutoTileSet(issues, FossickTerrainType.Stone, 19, "石头四方连续");
            ValidateAutoTileSet(issues, FossickTerrainType.Unbreakable, 18, "基岩四方连续");
            ValidateFogAutoTileSet(issues, 15);
            ValidateRewardSprite(issues, FossickElementType.Coin, "coin_pile", "金币实体");
            ValidateRewardSprite(issues, FossickElementType.Ore, "ore_copper", "铜矿实体");
            ValidateRewardSprite(issues, FossickElementType.Ore, "ore_gem", "宝石矿实体");
            ValidateRewardSprite(issues, FossickElementType.Ore, "ore_gold", "金矿实体");
            ValidateRewardSprite(issues, FossickElementType.Ore, "ore_silver", "银矿实体");
            ValidateRewardSprite(issues, FossickElementType.Item, "pickaxe", "矿镐实体");
            ValidateRewardSprite(issues, FossickElementType.Item, "dynamite", "炸药实体");
            ValidateRewardSprite(issues, FossickElementType.Item, "tnt", "雷管实体");
            ValidateRewardSprite(issues, FossickElementType.Item, "radar", "雷达实体");
            ValidateRewardSprite(issues, FossickElementType.Chest, "treasure_chest", "宝箱实体");
            ValidateRewardSprite(issues, FossickElementType.Collection, "collection_piece", "收藏品实体");
            ValidateTerrainAttachment(issues, FossickElementType.Ore, "ore_copper", FossickTerrainType.Dirt, "铜矿土块附着图");
            ValidateTerrainAttachment(issues, FossickElementType.Ore, "ore_copper", FossickTerrainType.Stone, "铜矿石块附着图");
            ValidateTerrainAttachment(issues, FossickElementType.Ore, "ore_gem", FossickTerrainType.Dirt, "宝石矿土块附着图");
            ValidateTerrainAttachment(issues, FossickElementType.Ore, "ore_gem", FossickTerrainType.Stone, "宝石矿石块附着图");
            ValidateTerrainAttachment(issues, FossickElementType.Ore, "ore_gold", FossickTerrainType.Dirt, "金矿土块附着图");
            ValidateTerrainAttachment(issues, FossickElementType.Ore, "ore_gold", FossickTerrainType.Stone, "金矿石块附着图");
            ValidateTerrainAttachment(issues, FossickElementType.Ore, "ore_silver", FossickTerrainType.Dirt, "银矿土块附着图");
            ValidateTerrainAttachment(issues, FossickElementType.Ore, "ore_silver", FossickTerrainType.Stone, "银矿石块附着图");
            ValidateTerrainAttachment(issues, FossickElementType.Item, "pickaxe", FossickTerrainType.Dirt, "矿镐土块附着图");
            ValidateTerrainAttachment(issues, FossickElementType.Item, "pickaxe", FossickTerrainType.Stone, "矿镐石块附着图");
            ValidateTerrainAttachment(issues, FossickElementType.Item, "dynamite", FossickTerrainType.Dirt, "炸药土块附着图");
            ValidateTerrainAttachment(issues, FossickElementType.Item, "dynamite", FossickTerrainType.Stone, "炸药石块附着图");
            ValidateTerrainAttachment(issues, FossickElementType.Item, "tnt", FossickTerrainType.Dirt, "雷管土块附着图");
            ValidateTerrainAttachment(issues, FossickElementType.Item, "tnt", FossickTerrainType.Stone, "雷管石块附着图");
            ValidateTerrainAttachment(issues, FossickElementType.Item, "radar", FossickTerrainType.Dirt, "雷达土块附着图");
            ValidateTerrainAttachment(issues, FossickElementType.Item, "radar", FossickTerrainType.Stone, "雷达石块附着图");
            ValidateTerrainAttachment(issues, FossickElementType.Chest, "treasure_chest", FossickTerrainType.Dirt, "宝箱土块附着图");
            ValidateToolSprite(issues, FossickToolType.Pickaxe, "矿镐道具图");
            ValidateToolSprite(issues, FossickToolType.Dynamite, "炸药道具图");
            ValidateToolSprite(issues, FossickToolType.Tnt, "雷管道具图");
            ValidateToolSprite(issues, FossickToolType.Radar, "雷达道具图");
            ValidateBackground(issues, "mine_default", "默认矿井背景");
            ValidateBackground(issues, "mine_map", "矿井地图背景");
            ValidateBackground(issues, "mine_variant", "矿井变化背景");
            ValidateBackground(issues, "treasure_room_3x2", "藏宝阁 3x2 背景");
            ValidateBackground(issues, "treasure_room_5x2", "藏宝阁 5x2 背景");
            ValidateBackground(issues, "treasure_room_7x2", "藏宝阁 7x2 背景");
            ValidateDecoration(issues, "grass_large", "大草装饰");
            ValidateDecoration(issues, "grass_small", "小草装饰");
            ValidateDecoration(issues, "mushroom", "蘑菇装饰");
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

        private static void ValidateRewardSprite(List<string> issues, FossickElementType type, string id, string label)
        {
            if (GetRewardSprite(new FossickElementConfig { type = type, id = id }) == null)
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

        private static void ValidateDecoration(List<string> issues, string id, string label)
        {
            if (GetDecorationSprite(id) == null)
            {
                issues.Add($"{label}缺少配置。");
            }
        }

        private static bool RuntimeCellMatches(IReadOnlyList<FossickCellState[]> rows, int x, int y, FossickTerrainType terrain)
        {
            if (y < 0 || y >= rows.Count)
            {
                return false;
            }

            var row = rows[y];
            if (row == null || x < 0 || x >= row.Length)
            {
                return false;
            }

            var cell = row[x];
            return cell != null && cell.terrain == terrain;
        }

        private static bool RuntimeCellIsFogged(IReadOnlyList<FossickCellState[]> rows, int x, int y)
        {
            if (y < 0 || y >= rows.Count)
            {
                return false;
            }

            var row = rows[y];
            if (row == null || x < 0 || x >= row.Length)
            {
                return false;
            }

            var cell = row[x];
            return cell == null || !cell.IsContentVisible;
        }

        private static bool ConfigCellMatches(IReadOnlyList<FossickCellConfig[]> rows, int x, int y, FossickTerrainType terrain)
        {
            if (y < 0 || y >= rows.Count)
            {
                return false;
            }

            var row = rows[y];
            if (row == null || x < 0 || x >= row.Length)
            {
                return false;
            }

            var cell = row[x];
            return cell != null && cell.terrain == terrain;
        }

        private static int MapCornerMaskToSpriteIndex(int mask)
        {
            // Fossick imported diagonal assets follow art-direction naming, not raw HOP mask order.
            switch (mask)
            {
                case 6:
                    return 9;
                case 9:
                    return 6;
                default:
                    return mask;
            }
        }
    }
}
