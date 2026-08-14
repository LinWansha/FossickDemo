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
        public Color effectHighlightColor = new Color(0.32f, 0.9f, 0.48f, 0.95f);
        public List<Sprite> collectionIcons = new List<Sprite>();

        public FossickVisualLayer0BackgroundCatalog layer0Background = new FossickVisualLayer0BackgroundCatalog();
        public FossickVisualLayer1RewardBackgroundCatalog layer1RewardBackground = new FossickVisualLayer1RewardBackgroundCatalog();
        public FossickVisualLayer2TerrainCatalog layer2Terrain = new FossickVisualLayer2TerrainCatalog();
        public FossickVisualLayer3TerrainAttachmentCatalog layer3TerrainAttachment = new FossickVisualLayer3TerrainAttachmentCatalog();
        public FossickVisualLayer4EntityCatalog layer4Entity = new FossickVisualLayer4EntityCatalog();
        public FossickVisualLayer5DecorationCatalog layer5Decoration = new FossickVisualLayer5DecorationCatalog();
        public FossickVisualLayer6FogCatalog layer6Fog = new FossickVisualLayer6FogCatalog();

        public Sprite GetAutoTileSprite(FossickTerrainType terrain, int assetIndex)
        {
            var set = FindAutoTileSet(terrain);
            return set == null ? null : set.GetSprite(assetIndex);
        }

        public Sprite GetTerrainSprite(FossickTerrainType terrain, string id = null)
        {
            if (terrain == FossickTerrainType.Empty)
            {
                return null;
            }

            return FindTerrainSprite(layer2Terrain == null ? null : layer2Terrain.terrainSprites, terrain, id);
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

        public Sprite GetEntitySprite(FossickElementConfig entity)
        {
            if (entity == null || entity.type == FossickElementType.None)
            {
                return null;
            }

            return FindElementSprite(layer4Entity == null ? null : layer4Entity.entities, entity);
        }

        public Sprite GetTerrainAttachmentSprite(FossickElementConfig entity, FossickTerrainType terrain)
        {
            if (entity == null || entity.type == FossickElementType.None || terrain == FossickTerrainType.Empty)
            {
                return null;
            }

            var entries = layer3TerrainAttachment == null ? null : layer3TerrainAttachment.attachments;
            var sprite = FindTerrainAttachmentSprite(entries, entity, terrain);
            return sprite != null ? sprite : FindTerrainAttachmentSprite(entries, entity, FossickTerrainType.Empty);
        }

        public Sprite GetToolSprite(FossickToolType toolType)
        {
            var entity = new FossickElementConfig
            {
                type = FossickElementType.Item,
                id = FossickContentIds.Tool.GetId(toolType)
            };
            return FindElementSprite(layer4Entity == null ? null : layer4Entity.entities, entity);
        }

        public Sprite GetBackgroundSprite(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return null;
            }

            return FindNamedSprite(layer0Background == null ? null : layer0Background.backgrounds, id);
        }

        public Sprite GetRewardBackgroundSprite(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return null;
            }

            return FindNamedSprite(layer1RewardBackground == null ? null : layer1RewardBackground.backgrounds, id);
        }

        public Sprite GetDecorationSprite(string id)
        {
            return FindNamedSprite(layer5Decoration == null ? null : layer5Decoration.decorations, id);
        }

        public Sprite GetCollectionSprite(string spriteName)
        {
            if (string.IsNullOrEmpty(spriteName) || collectionIcons == null)
            {
                return null;
            }

            for (var i = 0; i < collectionIcons.Count; i++)
            {
                var sprite = collectionIcons[i];
                if (sprite != null && sprite.name == spriteName)
                {
                    return sprite;
                }
            }

            return null;
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

            if (layer2Terrain.terrainSprites == null)
            {
                layer2Terrain.terrainSprites = new List<FossickTerrainSpriteEntry>();
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
            layer4Entity ??= new FossickVisualLayer4EntityCatalog();
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

        private static Sprite FindElementSprite(List<FossickElementSpriteEntry> entries, FossickElementConfig entity)
        {
            if (entries == null || entity == null)
            {
                return null;
            }

            for (var i = 0; i < entries.Count; i++)
            {
                var entry = entries[i];
                if (entry != null && entry.sprite != null && entry.type == entity.type && entry.id == entity.id)
                {
                    return entry.sprite;
                }
            }

            return null;
        }

        private static Sprite FindTerrainAttachmentSprite(List<FossickTerrainAttachmentSpriteEntry> entries, FossickElementConfig entity, FossickTerrainType terrain)
        {
            if (entries == null || entity == null)
            {
                return null;
            }

            for (var i = 0; i < entries.Count; i++)
            {
                var entry = entries[i];
                if (entry != null
                    && entry.sprite != null
                    && entry.type == entity.type
                    && entry.terrain == terrain
                    && entry.id == entity.id)
                {
                    return entry.sprite;
                }
            }

            return null;
        }

        private static Sprite FindTerrainSprite(List<FossickTerrainSpriteEntry> entries, FossickTerrainType terrain, string id)
        {
            if (entries == null)
            {
                return null;
            }

            for (var i = 0; i < entries.Count; i++)
            {
                var entry = entries[i];
                if (entry == null || entry.sprite == null || entry.terrain != terrain)
                {
                    continue;
                }

                if (string.IsNullOrEmpty(id) || entry.id == id)
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
        public List<FossickTerrainSpriteEntry> terrainSprites = new List<FossickTerrainSpriteEntry>();
    }

    [Serializable]
    public sealed class FossickVisualLayer3TerrainAttachmentCatalog
    {
        public List<FossickTerrainAttachmentSpriteEntry> attachments = new List<FossickTerrainAttachmentSpriteEntry>();
    }

    [Serializable]
    public sealed class FossickVisualLayer4EntityCatalog
    {
        public List<FossickElementSpriteEntry> entities = new List<FossickElementSpriteEntry>();
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
    public sealed class FossickTerrainSpriteEntry
    {
        public FossickTerrainType terrain;
        public string id;
        public Sprite sprite;
    }

    [Serializable]
    public sealed class FossickNamedSpriteEntry
    {
        public string id;
        public Sprite sprite;
    }
}
