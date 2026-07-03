using System;
using System.Collections.Generic;
using Fossick.Core.Config;
using UnityEngine;

namespace Fossick.Core.Visual
{
    [CreateAssetMenu(menuName = "Fossick/Art Catalog", fileName = "FossickArtCatalog")]
    public sealed class FossickArtCatalog : ScriptableObject
    {
        public Color emptyCellColor = new Color(0.08f, 0.18f, 0.22f);
        public Color previewColor = new Color(0.32f, 0.9f, 0.48f, 0.95f);

        public FossickLayer0BackgroundCatalog layer0Background = new FossickLayer0BackgroundCatalog();
        public FossickLayer1RewardBackgroundCatalog layer1RewardBackground = new FossickLayer1RewardBackgroundCatalog();
        public FossickLayer2TerrainCatalog layer2Terrain = new FossickLayer2TerrainCatalog();
        public FossickLayer3TerrainAttachmentCatalog layer3TerrainAttachment = new FossickLayer3TerrainAttachmentCatalog();
        public FossickLayer4RewardCatalog layer4Reward = new FossickLayer4RewardCatalog();
        public FossickLayer5DecorationCatalog layer5Decoration = new FossickLayer5DecorationCatalog();
        public FossickLayer6FogCatalog layer6Fog = new FossickLayer6FogCatalog();

        [HideInInspector]
        public List<FossickAutoTileSet> autoTileSets = new List<FossickAutoTileSet>();
        [HideInInspector]
        public List<FossickElementSpriteEntry> rewardSprites = new List<FossickElementSpriteEntry>();
        [HideInInspector]
        public List<FossickTerrainAttachmentSpriteEntry> terrainAttachmentSprites = new List<FossickTerrainAttachmentSpriteEntry>();
        [HideInInspector]
        public List<FossickToolSpriteEntry> toolSprites = new List<FossickToolSpriteEntry>();
        [HideInInspector]
        public List<FossickNamedSpriteEntry> decorations = new List<FossickNamedSpriteEntry>();
        [HideInInspector]
        public List<FossickNamedSpriteEntry> backgrounds = new List<FossickNamedSpriteEntry>();

        public Sprite GetAutoTileSprite(FossickTerrainType terrain, int assetIndex)
        {
            var set = FindAutoTileSet(terrain);
            return set == null ? null : set.GetSprite(assetIndex);
        }

        public bool HasAutoTileSprites(FossickTerrainType terrain)
        {
            var set = FindAutoTileSet(terrain);
            return set != null && set.sprites.Count > 0;
        }

        public Sprite GetFogAutoTileSprite(int assetIndex)
        {
            var set = FindFogAutoTileSet();
            return set == null ? null : set.GetSprite(assetIndex);
        }

        public Sprite GetRewardSprite(FossickElementConfig reward)
        {
            if (reward == null || reward.type == FossickElementType.None)
            {
                return null;
            }

            var sprite = FindElementSprite(layer4Reward == null ? null : layer4Reward.rewards, reward);
            return sprite != null ? sprite : FindElementSprite(rewardSprites, reward);
        }

        public Sprite GetTerrainAttachmentSprite(FossickElementConfig reward, FossickTerrainType terrain)
        {
            if (reward == null || reward.type == FossickElementType.None || terrain == FossickTerrainType.Empty)
            {
                return null;
            }

            var entries = layer3TerrainAttachment == null ? null : layer3TerrainAttachment.attachments;
            var sprite = FindTerrainAttachmentSprite(entries, reward, terrain);
            sprite ??= FindTerrainAttachmentSprite(entries, reward, FossickTerrainType.Empty);
            sprite ??= FindTerrainAttachmentSprite(terrainAttachmentSprites, reward, terrain);
            return sprite != null ? sprite : FindTerrainAttachmentSprite(terrainAttachmentSprites, reward, FossickTerrainType.Empty);
        }

        public Sprite GetToolSprite(FossickToolType toolType)
        {
            var reward = new FossickElementConfig
            {
                type = FossickElementType.Item,
                id = GetToolRewardId(toolType)
            };
            var sprite = FindElementSprite(layer4Reward == null ? null : layer4Reward.rewards, reward);
            return sprite != null ? sprite : FindToolSprite(toolSprites, toolType);
        }

        public Sprite GetBackgroundSprite(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return null;
            }

            if (id == "treasure_room")
            {
                id = "treasure_room_7x2";
            }

            var sprite = FindNamedSprite(layer0Background == null ? null : layer0Background.backgrounds, id);
            if (sprite != null)
            {
                return sprite;
            }

            sprite = FindNamedSprite(layer1RewardBackground == null ? null : layer1RewardBackground.backgrounds, id);
            return sprite != null ? sprite : FindNamedSprite(backgrounds, id);
        }

        public Sprite GetDecorationSprite(string id)
        {
            var sprite = FindNamedSprite(layer5Decoration == null ? null : layer5Decoration.decorations, id);
            return sprite != null ? sprite : FindNamedSprite(decorations, id);
        }

        private FossickAutoTileSet FindAutoTileSet(FossickTerrainType terrain)
        {
            var set = FindAutoTileSet(layer2Terrain == null ? null : layer2Terrain.autoTileSets, terrain);
            return set ?? FindAutoTileSet(autoTileSets, terrain);
        }

        private FossickAutoTileSet FindFogAutoTileSet()
        {
            if (layer6Fog != null && layer6Fog.autoTileSet != null && layer6Fog.autoTileSet.sprites.Count > 0)
            {
                return layer6Fog.autoTileSet;
            }

            for (var i = 0; i < autoTileSets.Count; i++)
            {
                var set = autoTileSets[i];
                if (set != null && set.kind == FossickAutoTileSetKind.Fog)
                {
                    return set;
                }
            }

            return null;
        }

        private void OnValidate()
        {
            MigrateLegacyFieldsIfNeeded();
        }

        private void MigrateLegacyFieldsIfNeeded()
        {
            EnsureLayerObjects();
            NormalizeLayerData();

            if (layer2Terrain.autoTileSets.Count == 0 && autoTileSets != null)
            {
                for (var i = 0; i < autoTileSets.Count; i++)
                {
                    var set = autoTileSets[i];
                    if (set == null)
                    {
                        continue;
                    }

                    if (set.kind == FossickAutoTileSetKind.Terrain)
                    {
                        layer2Terrain.autoTileSets.Add(set);
                    }
                    else if (set.kind == FossickAutoTileSetKind.Fog && (layer6Fog.autoTileSet == null || layer6Fog.autoTileSet.sprites.Count == 0))
                    {
                        layer6Fog.autoTileSet = set;
                    }
                }
            }

            CopyIfEmpty(layer4Reward.rewards, rewardSprites);
            CopyIfEmpty(layer3TerrainAttachment.attachments, terrainAttachmentSprites);
            CopyIfEmpty(layer5Decoration.decorations, decorations);

            if (backgrounds != null && backgrounds.Count > 0)
            {
                if (layer0Background.backgrounds.Count == 0)
                {
                    for (var i = 0; i < backgrounds.Count; i++)
                    {
                        var entry = backgrounds[i];
                        if (entry != null && !IsRewardBackgroundId(entry.id))
                        {
                            layer0Background.backgrounds.Add(entry);
                        }
                    }
                }

                if (layer1RewardBackground.backgrounds.Count == 0)
                {
                    for (var i = 0; i < backgrounds.Count; i++)
                    {
                        var entry = backgrounds[i];
                        if (entry != null && IsRewardBackgroundId(entry.id))
                        {
                            layer1RewardBackground.backgrounds.Add(entry);
                        }
                    }
                }
            }
        }

        private void NormalizeLayerData()
        {
            if (layer2Terrain.autoTileSets == null)
            {
                layer2Terrain.autoTileSets = new List<FossickAutoTileSet>();
            }

            for (var i = layer2Terrain.autoTileSets.Count - 1; i >= 0; i--)
            {
                var set = layer2Terrain.autoTileSets[i];
                if (set == null || set.kind == FossickAutoTileSetKind.Terrain)
                {
                    continue;
                }

                if (set.kind == FossickAutoTileSetKind.Fog && (layer6Fog.autoTileSet == null || layer6Fog.autoTileSet.sprites.Count == 0))
                {
                    layer6Fog.autoTileSet = set;
                }

                layer2Terrain.autoTileSets.RemoveAt(i);
            }

            if (layer6Fog.autoTileSet != null)
            {
                layer6Fog.autoTileSet.kind = FossickAutoTileSetKind.Fog;
                layer6Fog.autoTileSet.terrain = FossickTerrainType.Empty;
            }
        }

        private void EnsureLayerObjects()
        {
            layer0Background ??= new FossickLayer0BackgroundCatalog();
            layer1RewardBackground ??= new FossickLayer1RewardBackgroundCatalog();
            layer2Terrain ??= new FossickLayer2TerrainCatalog();
            layer3TerrainAttachment ??= new FossickLayer3TerrainAttachmentCatalog();
            layer4Reward ??= new FossickLayer4RewardCatalog();
            layer5Decoration ??= new FossickLayer5DecorationCatalog();
            layer6Fog ??= new FossickLayer6FogCatalog();
        }

        private static FossickAutoTileSet FindAutoTileSet(List<FossickAutoTileSet> sets, FossickTerrainType terrain)
        {
            if (sets == null)
            {
                return null;
            }

            for (var i = 0; i < sets.Count; i++)
            {
                var set = sets[i];
                if (set != null && set.kind == FossickAutoTileSetKind.Terrain && set.terrain == terrain)
                {
                    return set;
                }
            }

            return null;
        }

        private static Sprite FindToolSprite(List<FossickToolSpriteEntry> entries, FossickToolType toolType)
        {
            if (entries == null)
            {
                return null;
            }

            for (var i = 0; i < entries.Count; i++)
            {
                var entry = entries[i];
                if (entry != null && entry.type == toolType)
                {
                    return entry.sprite;
                }
            }

            return null;
        }

        private static Sprite FindElementSprite(List<FossickElementSpriteEntry> entries, FossickElementConfig reward)
        {
            if (entries == null || reward == null)
            {
                return null;
            }

            for (var i = 0; i < entries.Count; i++)
            {
                var entry = entries[i];
                if (entry != null && entry.sprite != null && entry.type == reward.type && entry.id == reward.id)
                {
                    return entry.sprite;
                }
            }

            return null;
        }

        private static Sprite FindTerrainAttachmentSprite(List<FossickTerrainAttachmentSpriteEntry> entries, FossickElementConfig reward, FossickTerrainType terrain)
        {
            if (entries == null || reward == null)
            {
                return null;
            }

            for (var i = 0; i < entries.Count; i++)
            {
                var entry = entries[i];
                if (entry != null
                    && entry.sprite != null
                    && entry.type == reward.type
                    && entry.terrain == terrain
                    && entry.id == reward.id)
                {
                    return entry.sprite;
                }
            }

            return null;
        }

        private static Sprite FindNamedSprite(List<FossickNamedSpriteEntry> entries, string id)
        {
            if (entries == null || string.IsNullOrEmpty(id))
            {
                return null;
            }

            for (var i = 0; i < entries.Count; i++)
            {
                var entry = entries[i];
                if (entry != null && entry.id == id)
                {
                    return entry.sprite;
                }
            }

            return null;
        }

        private static string GetToolRewardId(FossickToolType toolType)
        {
            switch (toolType)
            {
                case FossickToolType.Pickaxe:
                    return "pickaxe";
                case FossickToolType.Dynamite:
                    return "dynamite";
                case FossickToolType.Tnt:
                    return "tnt";
                case FossickToolType.Radar:
                    return "radar";
                default:
                    return string.Empty;
            }
        }

        private static void CopyIfEmpty<T>(List<T> target, List<T> source)
        {
            if (target == null || target.Count > 0 || source == null || source.Count == 0)
            {
                return;
            }

            for (var i = 0; i < source.Count; i++)
            {
                target.Add(source[i]);
            }
        }

        private static bool IsRewardBackgroundId(string id)
        {
            return !string.IsNullOrEmpty(id) && id.StartsWith("treasure_room", StringComparison.Ordinal);
        }
    }

    [Serializable]
    public sealed class FossickLayer0BackgroundCatalog
    {
        public List<FossickNamedSpriteEntry> backgrounds = new List<FossickNamedSpriteEntry>();
    }

    [Serializable]
    public sealed class FossickLayer1RewardBackgroundCatalog
    {
        public List<FossickNamedSpriteEntry> backgrounds = new List<FossickNamedSpriteEntry>();
    }

    [Serializable]
    public sealed class FossickLayer2TerrainCatalog
    {
        public List<FossickAutoTileSet> autoTileSets = new List<FossickAutoTileSet>();
    }

    [Serializable]
    public sealed class FossickLayer3TerrainAttachmentCatalog
    {
        public List<FossickTerrainAttachmentSpriteEntry> attachments = new List<FossickTerrainAttachmentSpriteEntry>();
    }

    [Serializable]
    public sealed class FossickLayer4RewardCatalog
    {
        public List<FossickElementSpriteEntry> rewards = new List<FossickElementSpriteEntry>();
    }

    [Serializable]
    public sealed class FossickLayer5DecorationCatalog
    {
        public List<FossickNamedSpriteEntry> decorations = new List<FossickNamedSpriteEntry>();
    }

    [Serializable]
    public sealed class FossickLayer6FogCatalog
    {
        public FossickAutoTileSet autoTileSet = new FossickAutoTileSet { kind = FossickAutoTileSetKind.Fog };
    }

    public enum FossickAutoTileSetKind
    {
        Terrain,
        Fog
    }

    [Serializable]
    public sealed class FossickAutoTileSet
    {
        public FossickAutoTileSetKind kind;
        public FossickTerrainType terrain;
        public List<FossickAutoTileSpriteEntry> sprites = new List<FossickAutoTileSpriteEntry>();

        public Sprite GetSprite(int index)
        {
            for (var i = 0; i < sprites.Count; i++)
            {
                var entry = sprites[i];
                if (entry != null && entry.index == index)
                {
                    return entry.sprite;
                }
            }

            return null;
        }
    }

    [Serializable]
    public sealed class FossickAutoTileSpriteEntry
    {
        public int index;
        public Sprite sprite;
    }

    [Serializable]
    public sealed class FossickElementSpriteEntry
    {
        public FossickElementType type;
        public string id;
        public Sprite sprite;
    }

    [Serializable]
    public sealed class FossickTerrainAttachmentSpriteEntry
    {
        public FossickElementType type;
        public string id;
        public FossickTerrainType terrain;
        public Sprite sprite;
    }

    [Serializable]
    public sealed class FossickToolSpriteEntry
    {
        public FossickToolType type;
        public Sprite sprite;
    }

    [Serializable]
    public sealed class FossickNamedSpriteEntry
    {
        public string id;
        public Sprite sprite;
    }
}
