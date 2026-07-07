using System;
using System.Collections.Generic;
using Fossick.Core.Definition.Config;
using UnityEngine;

namespace Fossick.Core.Visual
{
    [CreateAssetMenu(menuName = "Fossick/Art Catalog", fileName = "FossickArtCatalog")]
    public sealed class FossickArtCatalog : ScriptableObject
    {
        public Color emptyCellColor = new Color(0.08f, 0.18f, 0.22f);
        public Color previewColor = new Color(0.32f, 0.9f, 0.48f, 0.95f);

        public FossickVisualLayer0BackgroundCatalog layer0Background = new FossickVisualLayer0BackgroundCatalog();
        public FossickVisualLayer1RewardBackgroundCatalog layer1RewardBackground = new FossickVisualLayer1RewardBackgroundCatalog();
        public FossickVisualLayer2TerrainCatalog layer2Terrain = new FossickVisualLayer2TerrainCatalog();
        public FossickVisualLayer3TerrainAttachmentCatalog layer3TerrainAttachment = new FossickVisualLayer3TerrainAttachmentCatalog();
        public FossickVisualLayer4RewardCatalog layer4Reward = new FossickVisualLayer4RewardCatalog();
        public FossickVisualLayer5DecorationCatalog layer5Decoration = new FossickVisualLayer5DecorationCatalog();
        public FossickVisualLayer6FogCatalog layer6Fog = new FossickVisualLayer6FogCatalog();

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

            return FindElementSprite(layer4Reward == null ? null : layer4Reward.rewards, reward);
        }

        public Sprite GetTerrainAttachmentSprite(FossickElementConfig reward, FossickTerrainType terrain)
        {
            if (reward == null || reward.type == FossickElementType.None || terrain == FossickTerrainType.Empty)
            {
                return null;
            }

            var entries = layer3TerrainAttachment == null ? null : layer3TerrainAttachment.attachments;
            var sprite = FindTerrainAttachmentSprite(entries, reward, terrain);
            return sprite != null ? sprite : FindTerrainAttachmentSprite(entries, reward, FossickTerrainType.Empty);
        }

        public Sprite GetToolSprite(FossickToolType toolType)
        {
            var reward = new FossickElementConfig
            {
                type = FossickElementType.Item,
                id = GetToolRewardId(toolType)
            };
            return FindElementSprite(layer4Reward == null ? null : layer4Reward.rewards, reward);
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

            return FindNamedSprite(layer1RewardBackground == null ? null : layer1RewardBackground.backgrounds, id);
        }

        public Sprite GetDecorationSprite(string id)
        {
            return FindNamedSprite(layer5Decoration == null ? null : layer5Decoration.decorations, id);
        }

        private FossickAutoTileSet FindAutoTileSet(FossickTerrainType terrain)
        {
            return FindAutoTileSet(layer2Terrain == null ? null : layer2Terrain.autoTileSets, terrain);
        }

        private FossickAutoTileSet FindFogAutoTileSet()
        {
            if (layer6Fog != null && layer6Fog.autoTileSet != null && layer6Fog.autoTileSet.sprites.Count > 0)
            {
                return layer6Fog.autoTileSet;
            }

            return null;
        }

        private void OnValidate()
        {
            EnsureLayerObjects();
            NormalizeLayerData();
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
            layer0Background ??= new FossickVisualLayer0BackgroundCatalog();
            layer1RewardBackground ??= new FossickVisualLayer1RewardBackgroundCatalog();
            layer2Terrain ??= new FossickVisualLayer2TerrainCatalog();
            layer3TerrainAttachment ??= new FossickVisualLayer3TerrainAttachmentCatalog();
            layer4Reward ??= new FossickVisualLayer4RewardCatalog();
            layer5Decoration ??= new FossickVisualLayer5DecorationCatalog();
            layer6Fog ??= new FossickVisualLayer6FogCatalog();
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
    }

    [Serializable]
    public sealed class FossickVisualLayer0BackgroundCatalog
    {
        public List<FossickNamedSpriteEntry> backgrounds = new List<FossickNamedSpriteEntry>();
    }

    [Serializable]
    public sealed class FossickVisualLayer1RewardBackgroundCatalog
    {
        public List<FossickNamedSpriteEntry> backgrounds = new List<FossickNamedSpriteEntry>();
    }

    [Serializable]
    public sealed class FossickVisualLayer2TerrainCatalog
    {
        public List<FossickAutoTileSet> autoTileSets = new List<FossickAutoTileSet>();
    }

    [Serializable]
    public sealed class FossickVisualLayer3TerrainAttachmentCatalog
    {
        public List<FossickTerrainAttachmentSpriteEntry> attachments = new List<FossickTerrainAttachmentSpriteEntry>();
    }

    [Serializable]
    public sealed class FossickVisualLayer4RewardCatalog
    {
        public List<FossickElementSpriteEntry> rewards = new List<FossickElementSpriteEntry>();
    }

    [Serializable]
    public sealed class FossickVisualLayer5DecorationCatalog
    {
        public List<FossickNamedSpriteEntry> decorations = new List<FossickNamedSpriteEntry>();
    }

    [Serializable]
    public sealed class FossickVisualLayer6FogCatalog
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
    public sealed class FossickNamedSpriteEntry
    {
        public string id;
        public Sprite sprite;
    }
}
