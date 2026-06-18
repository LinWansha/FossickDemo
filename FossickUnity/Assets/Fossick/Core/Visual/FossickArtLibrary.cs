using System.Collections.Generic;
using Fossick.Core.Board;
using Fossick.Core.Config;
using UnityEngine;

namespace Fossick.Core.Visual
{
    public static class FossickArtLibrary
    {
        private const float PixelsPerUnit = 140f;
        private const string AutoTileRoot = "FossickArt/AutoTiles/";
        private const string DiggableAttachmentRoot = "FossickArt/Attachments/Diggable/";
        private const string StaticAttachmentRoot = "FossickArt/Attachments/Static/";
        private const string DiggedRewardRoot = "FossickArt/Rewards/Digged/";
        private const string GeneralRewardRoot = "FossickArt/Rewards/General/";
        private const string BackgroundRoot = "FossickArt/Backgrounds/";
        private const string TreasureBackgroundRoot = "FossickArt/TreasureBackgrounds/";
        private const string DefaultCatalogPath = "FossickArt/FossickArtCatalog";

        private static readonly Dictionary<string, Dictionary<int, Sprite>> AutoTileCache = new Dictionary<string, Dictionary<int, Sprite>>();
        private static readonly Dictionary<string, Sprite> ElementCache = new Dictionary<string, Sprite>();
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
            if (ActiveCatalog != null && ActiveCatalog.HasAutoTileSprites(terrain))
            {
                return true;
            }

            var folder = GetAutoTileFolder(terrain);
            return !string.IsNullOrEmpty(folder) && LoadAutoTileSet(folder).Count > 0;
        }

        public static Sprite GetAutoTileSprite(FossickTerrainType terrain, int assetIndex)
        {
            if (assetIndex <= 0)
            {
                return null;
            }

            var catalogSprite = ActiveCatalog == null ? null : ActiveCatalog.GetAutoTileSprite(terrain, assetIndex);
            if (catalogSprite != null)
            {
                return catalogSprite;
            }

            var folder = GetAutoTileFolder(terrain);
            if (string.IsNullOrEmpty(folder))
            {
                return null;
            }

            var sprites = LoadAutoTileSet(folder);
            Sprite sprite;
            return sprites.TryGetValue(assetIndex, out sprite) ? sprite : null;
        }

        public static Sprite GetFogAutoTileSprite(int assetIndex)
        {
            if (assetIndex <= 0)
            {
                return null;
            }

            var sprites = LoadAutoTileSet("Fog");
            Sprite sprite;
            return sprites.TryGetValue(assetIndex, out sprite) ? sprite : null;
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
            if (reward == null || reward.type == FossickElementType.None)
            {
                return null;
            }

            var resourceSprite = GetRewardSpriteFromResources(reward);
            if (resourceSprite != null)
            {
                return resourceSprite;
            }

            var catalogSprite = ActiveCatalog == null ? null : ActiveCatalog.GetRewardSprite(reward);
            if (catalogSprite != null)
            {
                return catalogSprite;
            }

            return null;
        }

        private static Sprite GetRewardSpriteFromResources(FossickElementConfig reward)
        {
            switch (reward.type)
            {
                case FossickElementType.Coin:
                    return GetRewardSpriteByCandidates(GeneralRewardRoot, reward.id, "coin_pile", "28", "35", "36");
                case FossickElementType.Score:
                    return GetRewardSpriteByCandidates(GeneralRewardRoot, reward.id, "score_gem", "29", "30");
                case FossickElementType.Ore:
                    return GetOreRewardSprite(reward.id);
                case FossickElementType.Item:
                    return GetItemRewardSprite(reward.id);
                case FossickElementType.Chest:
                    return GetRewardSpriteByCandidates(GeneralRewardRoot, reward.id, "treasure_chest", "宝箱关上", "20");
                case FossickElementType.Collection:
                    return GetRewardSpriteByCandidates(GeneralRewardRoot, reward.id, "collection_piece", "34", "漂流瓶阴影");
                default:
                    return null;
            }
        }

        public static Sprite GetTerrainAttachmentSprite(FossickElementConfig reward, FossickTerrainType terrain)
        {
            if (reward == null || reward.type == FossickElementType.None)
            {
                return null;
            }

            switch (reward.type)
            {
                case FossickElementType.Ore:
                    return GetOreAttachmentSprite(reward.id, terrain);
                case FossickElementType.Item:
                    return GetItemAttachmentSprite(reward.id, terrain);
                case FossickElementType.Chest:
                    return GetChestAttachmentSprite(reward.id, terrain);
                default:
                    return null;
            }
        }

        private static Sprite GetResourceSprite(string root, string id)
        {
            if (string.IsNullOrEmpty(root) || string.IsNullOrEmpty(id))
            {
                return null;
            }

            var key = root + id;
            Sprite cached;
            if (ElementCache.TryGetValue(key, out cached))
            {
                return cached;
            }

            var importedSprite = Resources.Load<Sprite>(key);
            if (importedSprite != null)
            {
                importedSprite.texture.filterMode = FilterMode.Bilinear;
                ElementCache[key] = importedSprite;
                return importedSprite;
            }

            var texture = Resources.Load<Texture2D>(key);
            if (texture == null)
            {
                ElementCache[key] = null;
                return null;
            }

            texture.filterMode = FilterMode.Bilinear;
            var sprite = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), PixelsPerUnit);
            sprite.name = texture.name;
            ElementCache[key] = sprite;
            return sprite;
        }

        public static Sprite GetToolSprite(FossickToolType toolType)
        {
            switch (toolType)
            {
                case FossickToolType.Dynamite:
                    return GetRewardSpriteByCandidates(DiggedRewardRoot, "26");
                case FossickToolType.Tnt:
                    return GetRewardSpriteByCandidates(DiggedRewardRoot, "27");
                case FossickToolType.Radar:
                    return GetRewardSpriteByCandidates(DiggedRewardRoot, "25");
                default:
                    return GetRewardSpriteByCandidates(DiggedRewardRoot, "24");
            }
        }

        private static Sprite GetItemRewardSprite(string id)
        {
            if (id == "pickaxe")
            {
                return GetRewardSpriteByCandidates(DiggedRewardRoot, "pickaxe", "24");
            }

            if (id == "dynamite")
            {
                return GetRewardSpriteByCandidates(DiggedRewardRoot, "dynamite", "26");
            }

            if (id == "tnt")
            {
                return GetRewardSpriteByCandidates(DiggedRewardRoot, "tnt", "27");
            }

            if (id == "radar")
            {
                return GetRewardSpriteByCandidates(DiggedRewardRoot, "radar", "25");
            }

            return GetRewardSpriteByCandidates(DiggedRewardRoot, id, "tool_box", "24", "25", "26", "27");
        }

        private static Sprite GetItemAttachmentSprite(string id, FossickTerrainType terrain)
        {
            if (id == "pickaxe")
            {
                return GetTerrainSpecificAttachmentSprite(terrain, "10", "11");
            }

            if (id == "dynamite")
            {
                return GetTerrainSpecificAttachmentSprite(terrain, "15", "14");
            }

            if (id == "tnt")
            {
                return GetTerrainSpecificAttachmentSprite(terrain, "13", "12");
            }

            if (id == "radar")
            {
                return GetTerrainSpecificAttachmentSprite(terrain, "18", "17");
            }

            return null;
        }

        private static Sprite GetOreAttachmentSprite(string id, FossickTerrainType terrain)
        {
            return terrain == FossickTerrainType.Empty ? null : GetOreAttachmentSprite(id);
        }

        private static Sprite GetOreRewardSprite(string id)
        {
            switch (id)
            {
                case "ore_gold":
                case "gold":
                case "ore_yellow":
                    return GetRewardSpriteByCandidates(DiggedRewardRoot, "8", id);
                case "ore_copper":
                case "copper":
                case "ore_orange":
                    return GetRewardSpriteByCandidates(DiggedRewardRoot, "5", id);
                case "ore_silver":
                case "silver":
                case "ore_blue":
                    return GetRewardSpriteByCandidates(DiggedRewardRoot, "9", id);
                case "ore_gem":
                case "gem":
                case "ore_crystal":
                case "crystal":
                    return GetRewardSpriteByCandidates(DiggedRewardRoot, "6", id);
                default:
                    return GetRewardSpriteByCandidates(DiggedRewardRoot, id, "5", "6", "8", "9");
            }
        }

        private static Sprite GetChestAttachmentSprite(string id, FossickTerrainType terrain)
        {
            return terrain == FossickTerrainType.Dirt ? GetAttachmentSpriteByCandidates("16") : null;
        }

        private static Sprite GetOreAttachmentSprite(string id)
        {
            switch (id)
            {
                case "ore_gold":
                case "gold":
                case "ore_yellow":
                    return GetAttachmentSpriteByCandidates("1");
                case "ore_copper":
                case "copper":
                case "ore_orange":
                    return GetAttachmentSpriteByCandidates("2");
                case "ore_silver":
                case "silver":
                case "ore_blue":
                    return GetAttachmentSpriteByCandidates("3");
                case "ore_gem":
                case "gem":
                case "ore_crystal":
                case "crystal":
                    return GetAttachmentSpriteByCandidates("4");
                default:
                    return GetAttachmentSpriteByCandidates("1", "2", "3", "4");
            }
        }

        private static Sprite GetTerrainSpecificAttachmentSprite(FossickTerrainType terrain, string dirtSpriteId, string stoneSpriteId)
        {
            if (IsStoneLikeTerrain(terrain))
            {
                return GetAttachmentSpriteByCandidates(stoneSpriteId);
            }

            return terrain == FossickTerrainType.Dirt ? GetAttachmentSpriteByCandidates(dirtSpriteId) : null;
        }

        private static bool IsStoneLikeTerrain(FossickTerrainType terrain)
        {
            return terrain == FossickTerrainType.Stone || terrain == FossickTerrainType.Unbreakable;
        }

        private static bool IsKnownToolItemId(string id)
        {
            return id == "pickaxe" || id == "dynamite" || id == "tnt" || id == "radar";
        }

        public static Sprite GetBackgroundSprite(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return null;
            }

            var catalogSprite = ActiveCatalog == null ? null : ActiveCatalog.GetBackgroundSprite(id);
            if (catalogSprite != null)
            {
                return catalogSprite;
            }

            if (id == "treasure_room_3x2")
            {
                return GetRewardSpriteByCandidates(TreasureBackgroundRoot, "31")
                    ?? GetRewardSpriteByCandidates(BackgroundRoot, "地图1", "底图2", "底图3");
            }

            if (id == "treasure_room_5x2")
            {
                return GetRewardSpriteByCandidates(TreasureBackgroundRoot, "32")
                    ?? GetRewardSpriteByCandidates(BackgroundRoot, "地图1", "底图2", "底图3");
            }

            if (id == "treasure_room" || id == "treasure_room_7x2")
            {
                return GetRewardSpriteByCandidates(TreasureBackgroundRoot, "33")
                    ?? GetRewardSpriteByCandidates(BackgroundRoot, "地图1", "底图2", "底图3");
            }

            if (id == "mine_default")
            {
                return GetRewardSpriteByCandidates(BackgroundRoot, "底图2", "底图3", "地图1");
            }

            return GetRewardSpriteByCandidates(BackgroundRoot, id)
                ?? GetRewardSpriteByCandidates(TreasureBackgroundRoot, id);
        }

        public static Sprite GetDecorationSprite(string id)
        {
            if (IsReservedElementArtId(id))
            {
                return null;
            }

            var catalogSprite = ActiveCatalog == null ? null : ActiveCatalog.GetDecorationSprite(id);
            if (catalogSprite != null)
            {
                return catalogSprite;
            }

            switch (id)
            {
                case "grass_large":
                    return GetRewardSpriteByCandidates(StaticAttachmentRoot, "grass_large", "21");
                case "grass_small":
                    return GetRewardSpriteByCandidates(StaticAttachmentRoot, "grass_small", "22");
                case "mushroom":
                    return GetRewardSpriteByCandidates(StaticAttachmentRoot, "mushroom", "23");
                default:
                    return GetRewardSpriteByCandidates(StaticAttachmentRoot, id);
            }
        }

        private static bool IsReservedElementArtId(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return false;
            }

            switch (id)
            {
                case "small_rock":
                case "gold_pile":
                case "pickaxe":
                case "dynamite":
                case "tnt":
                case "radar":
                case "coin_pile":
                case "score_gem":
                case "ore_copper":
                case "ore_orange":
                case "ore_silver":
                case "ore_blue":
                case "ore_gold":
                case "ore_yellow":
                case "ore_gem":
                case "ore_crystal":
                case "treasure_chest":
                case "collection_piece":
                    return true;
            }

            int numericId;
            return int.TryParse(id, out numericId) && numericId >= 1 && numericId <= 37;
        }

        public static Color GetEmptyCellColor()
        {
            return ActiveCatalog == null ? new Color(0.1f, 0.18f, 0.18f) : ActiveCatalog.emptyCellColor;
        }

        public static Color GetFogColor()
        {
            return ActiveCatalog == null ? new Color(0.08f, 0.06f, 0.045f, 0.58f) : ActiveCatalog.fogColor;
        }

        public static Color GetPreviewColor()
        {
            return ActiveCatalog == null ? new Color(0.32f, 0.9f, 0.48f, 0.95f) : ActiveCatalog.previewColor;
        }

        public static Color GetFallbackTerrainColor(FossickTerrainType terrain)
        {
            if (ActiveCatalog != null)
            {
                return ActiveCatalog.GetFallbackTerrainColor(terrain);
            }

            switch (terrain)
            {
                case FossickTerrainType.Dirt:
                    return new Color(0.56f, 0.38f, 0.25f);
                case FossickTerrainType.Stone:
                    return new Color(0.48f, 0.53f, 0.58f);
                case FossickTerrainType.Unbreakable:
                    return new Color(0.07f, 0.07f, 0.09f);
                default:
                    return GetEmptyCellColor();
            }
        }

        private static Sprite GetRewardSpriteByCandidates(string root, params string[] ids)
        {
            for (var i = 0; i < ids.Length; i++)
            {
                var sprite = GetResourceSprite(root, ids[i]);
                if (sprite != null)
                {
                    return sprite;
                }
            }

            return null;
        }

        private static Sprite GetAttachmentSpriteByCandidates(params string[] ids)
        {
            for (var i = 0; i < ids.Length; i++)
            {
                var sprite = GetResourceSprite(DiggableAttachmentRoot, ids[i]);
                if (sprite != null)
                {
                    return sprite;
                }
            }

            return null;
        }

        private static Dictionary<int, Sprite> LoadAutoTileSet(string folder)
        {
            Dictionary<int, Sprite> cached;
            if (AutoTileCache.TryGetValue(folder, out cached))
            {
                return cached;
            }

            var result = new Dictionary<int, Sprite>();
            var sprites = Resources.LoadAll<Sprite>(AutoTileRoot + folder);
            for (var i = 0; i < sprites.Length; i++)
            {
                var sprite = sprites[i];
                var index = ParseLeadingInt(sprite.name);
                if (index <= 0)
                {
                    continue;
                }

                result[index] = sprite;
            }

            var textures = Resources.LoadAll<Texture2D>(AutoTileRoot + folder);
            for (var i = 0; i < textures.Length; i++)
            {
                var texture = textures[i];
                var index = ParseLeadingInt(texture.name);
                if (index <= 0 || result.ContainsKey(index))
                {
                    continue;
                }

                texture.filterMode = FilterMode.Bilinear;
                texture.wrapMode = TextureWrapMode.Clamp;
                var runtimeSprite = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), PixelsPerUnit);
                runtimeSprite.name = texture.name;
                result[index] = runtimeSprite;
            }

            AutoTileCache[folder] = result;
            return result;
        }

        private static string GetAutoTileFolder(FossickTerrainType terrain)
        {
            switch (terrain)
            {
                case FossickTerrainType.Dirt:
                    return "Dirt";
                case FossickTerrainType.Stone:
                    return "Stone";
                case FossickTerrainType.Unbreakable:
                    return "Rock";
                default:
                    return null;
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
            // Fossick's imported diagonal assets are named by art direction, not by the raw HOP mask number.
            // Mask 6 = upper-right + lower-left, and mask 9 = upper-left + lower-right.
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

        private static int ParseLeadingInt(string value)
        {
            var number = 0;
            for (var i = 0; i < value.Length; i++)
            {
                var ch = value[i];
                if (ch < '0' || ch > '9')
                {
                    break;
                }

                number = number * 10 + ch - '0';
            }

            return number;
        }
    }
}
