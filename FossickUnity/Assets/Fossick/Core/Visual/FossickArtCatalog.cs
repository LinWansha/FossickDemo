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
        public Color fogColor = new Color(0.08f, 0.06f, 0.045f, 0.58f);
        public Color previewColor = new Color(0.32f, 0.9f, 0.48f, 0.95f);
        public Color fallbackDirtColor = new Color(0.56f, 0.38f, 0.25f);
        public Color fallbackStoneColor = new Color(0.48f, 0.53f, 0.58f);
        public Color fallbackRockColor = new Color(0.07f, 0.07f, 0.09f);
        public Sprite mineBackground;
        public Sprite treasureRoomBackground;
        public Sprite fogSprite;
        public List<FossickAutoTileSet> autoTileSets = new List<FossickAutoTileSet>();
        public List<FossickElementSpriteEntry> rewardSprites = new List<FossickElementSpriteEntry>();
        public List<FossickToolSpriteEntry> toolSprites = new List<FossickToolSpriteEntry>();
        public List<FossickNamedSpriteEntry> decorations = new List<FossickNamedSpriteEntry>();
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

        public Sprite GetRewardSprite(FossickElementConfig reward)
        {
            if (reward == null || reward.type == FossickElementType.None)
            {
                return null;
            }

            for (var i = 0; i < rewardSprites.Count; i++)
            {
                var entry = rewardSprites[i];
                if (entry != null && entry.sprite != null && entry.type == reward.type && entry.id == reward.id)
                {
                    return entry.sprite;
                }
            }

            for (var i = 0; i < rewardSprites.Count; i++)
            {
                var entry = rewardSprites[i];
                if (entry == null || entry.sprite == null || entry.type != reward.type)
                {
                    continue;
                }

                if (string.IsNullOrEmpty(entry.id))
                {
                    return entry.sprite;
                }
            }

            return null;
        }

        public Sprite GetToolSprite(FossickToolType toolType)
        {
            for (var i = 0; i < toolSprites.Count; i++)
            {
                var entry = toolSprites[i];
                if (entry != null && entry.type == toolType)
                {
                    return entry.sprite;
                }
            }

            return null;
        }

        public Sprite GetBackgroundSprite(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return null;
            }

            if (id == "mine_default")
            {
                return mineBackground;
            }

            if (id == "treasure_room")
            {
                return treasureRoomBackground;
            }

            return FindNamedSprite(backgrounds, id);
        }

        public Sprite GetDecorationSprite(string id)
        {
            return FindNamedSprite(decorations, id);
        }

        public Color GetFallbackTerrainColor(FossickTerrainType terrain)
        {
            switch (terrain)
            {
                case FossickTerrainType.Dirt:
                    return fallbackDirtColor;
                case FossickTerrainType.Stone:
                    return fallbackStoneColor;
                case FossickTerrainType.Unbreakable:
                    return fallbackRockColor;
                default:
                    return emptyCellColor;
            }
        }

        private FossickAutoTileSet FindAutoTileSet(FossickTerrainType terrain)
        {
            for (var i = 0; i < autoTileSets.Count; i++)
            {
                var set = autoTileSets[i];
                if (set != null && set.terrain == terrain)
                {
                    return set;
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
    public sealed class FossickAutoTileSet
    {
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
