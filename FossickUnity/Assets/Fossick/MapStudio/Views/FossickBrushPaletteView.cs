using System;
using Fossick.Core.Definition.Config;
using Fossick.Core.Visual;
using Fossick.Core.Visual.Tiling;
using Fossick.MapStudio.Definition;
using UnityEngine;
using UnityEngine.UI;

namespace Fossick.MapStudio.Views
{
    public sealed class FossickBrushPaletteView
    {
        private const float BrushTileWidth = 76f;
        private const float BrushTileHeight = 78f;

        private readonly Font font;

        public FossickBrushPaletteView(Font font)
        {
            this.font = font;
        }

        public struct State
        {
            public FossickBrushMode selectedBrushMode;
            public FossickTerrainType selectedTerrain;
            public FossickElementType selectedRewardType;
            public string selectedRewardId;
            public string selectedRewardBackgroundId;
            public int selectedRewardBackgroundWidth;
            public int selectedRewardBackgroundHeight;
            public string selectedDecorationId;
            public FossickFogType selectedFog;
        }

        public sealed class Callbacks
        {
            public Action<FossickBrushMode> selectBrushMode;
            public Action<FossickTerrainType> selectTerrain;
            public Action<FossickElementType, string, string> selectReward;
            public Action<string, string, int, int> selectRewardBackground;
            public Action<string, string> selectDecoration;
            public Action<FossickFogType, string> selectFog;
        }

        public void DrawBrushModeTabs(RectTransform parent, State state, Callbacks callbacks)
        {
            AddBrushModeButton(parent, state, callbacks, FossickBrushMode.RewardBackground, "1 藏宝阁");
            AddBrushModeButton(parent, state, callbacks, FossickBrushMode.Terrain, "2 地形");
            AddBrushModeButton(parent, state, callbacks, FossickBrushMode.Reward, "3 奖励");
            AddBrushModeButton(parent, state, callbacks, FossickBrushMode.Tool, "4 道具");
            AddBrushModeButton(parent, state, callbacks, FossickBrushMode.Decoration, "5 装饰");
            AddBrushModeButton(parent, state, callbacks, FossickBrushMode.Fog, "6 阴影");
        }

        public void DrawBrushes(RectTransform parent, State state, Callbacks callbacks)
        {
            if (state.selectedBrushMode == FossickBrushMode.RewardBackground)
            {
                AddRewardBackgroundBrushTile(parent, state, callbacks, string.Empty, "清空", 0, 0);
                AddRewardBackgroundBrushTile(parent, state, callbacks, FossickContentIds.RewardBackground.TreasureRoomSmall, "小藏宝阁 3x2", 3, 2);
                AddRewardBackgroundBrushTile(parent, state, callbacks, FossickContentIds.RewardBackground.TreasureRoomMedium, "中藏宝阁 5x2", 5, 2);
                AddRewardBackgroundBrushTile(parent, state, callbacks, FossickContentIds.RewardBackground.TreasureRoomLarge, "大藏宝阁 7x2", 7, 2);
                return;
            }

            if (state.selectedBrushMode == FossickBrushMode.Terrain)
            {
                AddTerrainBrushTile(parent, state, callbacks, FossickTerrainType.Empty, "空格");
                AddTerrainBrushTile(parent, state, callbacks, FossickTerrainType.Dirt, "土");
                AddTerrainBrushTile(parent, state, callbacks, FossickTerrainType.Stone, "石头");
                AddTerrainBrushTile(parent, state, callbacks, FossickTerrainType.Unbreakable, "基岩");
                AddTerrainBrushTile(parent, state, callbacks, FossickTerrainType.Explosives, "炸药箱");
                return;
            }

            if (state.selectedBrushMode == FossickBrushMode.Reward)
            {
                AddRewardBrushTile(parent, state, callbacks, FossickElementType.None, "清空", null);
                AddRewardBrushTile(parent, state, callbacks, FossickElementType.Coin, "掉落金币堆", FossickContentIds.Reward.CoinDrop);
                AddRewardBrushTile(parent, state, callbacks, FossickElementType.Coin, "小金币堆", FossickContentIds.Reward.CoinPileSmall);
                AddRewardBrushTile(parent, state, callbacks, FossickElementType.Coin, "大金币堆", FossickContentIds.Reward.CoinPileLarge);
                AddRewardBrushTile(parent, state, callbacks, FossickElementType.Ore, "铜矿", FossickContentIds.Reward.OreCopper);
                AddRewardBrushTile(parent, state, callbacks, FossickElementType.Ore, "银矿", FossickContentIds.Reward.OreSilver);
                AddRewardBrushTile(parent, state, callbacks, FossickElementType.Ore, "金矿", FossickContentIds.Reward.OreGold);
                AddRewardBrushTile(parent, state, callbacks, FossickElementType.Ore, "宝石矿", FossickContentIds.Reward.OreGem);
                AddRewardBrushTile(parent, state, callbacks, FossickElementType.Collection, "收藏品箱", FossickContentIds.Reward.CollectionBox);
                AddRewardBrushTile(parent, state, callbacks, FossickElementType.Chest, "奖励宝箱", FossickContentIds.Reward.TreasureChest);
                AddRewardBrushTile(parent, state, callbacks, FossickElementType.Chest, "漂流瓶", FossickContentIds.Reward.MessageBottle);
                return;
            }

            if (state.selectedBrushMode == FossickBrushMode.Tool)
            {
                AddRewardBrushTile(parent, state, callbacks, FossickElementType.None, "清空", null);
                AddRewardBrushTile(parent, state, callbacks, FossickElementType.Item, "矿镐", FossickContentIds.Tool.Pickaxe);
                AddRewardBrushTile(parent, state, callbacks, FossickElementType.Item, "雷管", FossickContentIds.Tool.Dynamite);
                AddRewardBrushTile(parent, state, callbacks, FossickElementType.Item, "炸药", FossickContentIds.Tool.Tnt);
                AddRewardBrushTile(parent, state, callbacks, FossickElementType.Item, "雷达", FossickContentIds.Tool.Radar);
                return;
            }

            if (state.selectedBrushMode == FossickBrushMode.Decoration)
            {
                AddDecorationBrushTile(parent, state, callbacks, string.Empty, "清空");
                AddDecorationBrushTile(parent, state, callbacks, FossickContentIds.Decoration.GrassLarge, "草丛");
                AddDecorationBrushTile(parent, state, callbacks, FossickContentIds.Decoration.GrassSmall, "小草");
                AddDecorationBrushTile(parent, state, callbacks, FossickContentIds.Decoration.Mushroom, "蘑菇");
                return;
            }

            if (state.selectedBrushMode == FossickBrushMode.Fog)
            {
                AddFogBrushTile(parent, state, callbacks, FossickFogType.None, "无阴影");
                AddFogBrushTile(parent, state, callbacks, FossickFogType.Covered, "阴影");
            }
        }

        private void AddBrushModeButton(RectTransform parent, State state, Callbacks callbacks, FossickBrushMode mode, string label)
        {
            AddButton(parent, label, new Vector2(120f, 34f), () => callbacks.selectBrushMode?.Invoke(mode), state.selectedBrushMode == mode);
        }

        private void AddRewardBackgroundBrushTile(RectTransform parent, State state, Callbacks callbacks, string id, string label, int width, int height)
        {
            var selected = state.selectedBrushMode == FossickBrushMode.RewardBackground
                && state.selectedRewardBackgroundId == id
                && state.selectedRewardBackgroundWidth == width
                && state.selectedRewardBackgroundHeight == height;
            var sprite = string.IsNullOrEmpty(id) ? null : FossickArtLibrary.GetRewardBackgroundSprite(id);
            AddBrushTile(parent, label, selected, sprite, string.IsNullOrEmpty(id) ? "×" : null, string.IsNullOrEmpty(id) ? new Color(0.11f, 0.13f, 0.15f) : new Color(0.38f, 0.27f, 0.1f, 0.9f), () =>
            {
                callbacks.selectRewardBackground?.Invoke(id, label, width, height);
            });
        }

        private void AddRewardBrushTile(RectTransform parent, State state, Callbacks callbacks, FossickElementType type, string label, string rewardId)
        {
            var id = rewardId ?? GetDefaultRewardId(type);
            var selected = state.selectedRewardType == type
                && state.selectedRewardId == id
                && (state.selectedBrushMode == FossickBrushMode.Reward || state.selectedBrushMode == FossickBrushMode.Tool);
            var previewId = FossickContentIds.Reward.IsCoinDropPlaceholder(id)
                ? FossickContentIds.Reward.CoinDropSmall
                : id;
            var sprite = type == FossickElementType.None
                ? null
                : FossickArtLibrary.GetEntitySprite(new FossickElementConfig
                {
                    type = type,
                    id = previewId
                });

            AddBrushTile(parent, label, selected, type == FossickElementType.None ? null : sprite, type == FossickElementType.None ? "×" : null, type == FossickElementType.None ? new Color(0.11f, 0.13f, 0.15f) : GetRewardColor(type), () =>
            {
                callbacks.selectReward?.Invoke(type, id, label);
            });
        }

        private void AddTerrainBrushTile(RectTransform parent, State state, Callbacks callbacks, FossickTerrainType terrain, string label)
        {
            var sprite = terrain == FossickTerrainType.Empty
                ? null
                : FossickArtLibrary.GetTerrainSprite(terrain) ?? FossickArtLibrary.GetAutoTileSprite(terrain, 15);
            AddBrushTile(parent, label, state.selectedTerrain == terrain, sprite, terrain == FossickTerrainType.Empty ? "." : null, GetTerrainColor(terrain), () =>
            {
                callbacks.selectTerrain?.Invoke(terrain);
            });
        }

        private void AddDecorationBrushTile(RectTransform parent, State state, Callbacks callbacks, string id, string label)
        {
            var sprite = string.IsNullOrEmpty(id) ? null : FossickArtLibrary.GetDecorationSprite(id);
            AddBrushTile(parent, label, state.selectedDecorationId == id, sprite, string.IsNullOrEmpty(id) ? "×" : null, string.IsNullOrEmpty(id) ? new Color(0.11f, 0.13f, 0.15f) : new Color(0.18f, 0.55f, 0.24f, 0.85f), () =>
            {
                callbacks.selectDecoration?.Invoke(id, label);
            });
        }

        private void AddFogBrushTile(RectTransform parent, State state, Callbacks callbacks, FossickFogType fog, string label)
        {
            var sprite = fog == FossickFogType.Covered ? FossickArtLibrary.GetFogAutoTileSprite(15) : null;
            AddBrushTile(parent, label, state.selectedFog == fog, sprite, fog == FossickFogType.None ? "×" : null, fog == FossickFogType.None ? new Color(0.11f, 0.13f, 0.15f) : FossickArtLibrary.GetFogColor(), () =>
            {
                callbacks.selectFog?.Invoke(fog, label);
            });
        }

        private void AddBrushTile(RectTransform parent, string label, bool selected, Sprite sprite, string placeholderText, Color backgroundColor, Action onClick)
        {
            var rect = CreateRect(label, parent);
            rect.sizeDelta = new Vector2(BrushTileWidth, BrushTileHeight);
            var background = AddImage(rect.gameObject, selected ? new Color(0.24f, 0.45f, 0.7f) : new Color(0.17f, 0.19f, 0.21f));

            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = background;
            button.onClick.AddListener(() => onClick?.Invoke());

            var iconFrame = CreateRect("Icon", rect);
            SetTopLeft(iconFrame, 8f, 5f, BrushTileWidth - 16f, 48f);
            var iconBackground = AddImage(iconFrame.gameObject, sprite == null ? backgroundColor : new Color(0.08f, 0.1f, 0.11f));
            iconBackground.raycastTarget = false;

            if (sprite != null)
            {
                var icon = CreateRect("Sprite", iconFrame);
                Stretch(icon);
                var image = AddImage(icon.gameObject, Color.white);
                image.sprite = sprite;
                image.preserveAspect = true;
                image.raycastTarget = false;
            }
            else if (!string.IsNullOrEmpty(placeholderText))
            {
                AddText(iconFrame, placeholderText, placeholderText == "×" ? 28 : 20, FontStyle.Bold, new Vector2(BrushTileWidth - 16f, 48f), TextAnchor.MiddleCenter);
            }

            var text = AddText(rect, label, 12, FontStyle.Bold, new Vector2(BrushTileWidth, 22f), TextAnchor.MiddleCenter);
            SetTopLeft(text.GetComponent<RectTransform>(), 0f, BrushTileHeight - 23f, BrushTileWidth, 22f);
        }

        private RectTransform AddButton(Transform parent, string label, Vector2 size, Action onClick, bool selected = false)
        {
            var rect = CreateRect(label, parent);
            rect.sizeDelta = size;
            AddImage(rect.gameObject, selected ? new Color(0.24f, 0.45f, 0.7f) : new Color(0.22f, 0.24f, 0.27f));

            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = rect.GetComponent<Image>();
            button.onClick.AddListener(() => onClick?.Invoke());

            AddText(rect, label, 13, FontStyle.Bold, size, TextAnchor.MiddleCenter);
            return rect;
        }

        private RectTransform CreateRect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go.GetComponent<RectTransform>();
        }

        private Text AddText(Transform parent, string value, int size, FontStyle style, Vector2 rectSize, TextAnchor anchor = TextAnchor.MiddleLeft)
        {
            var rect = CreateRect("Text", parent);
            rect.sizeDelta = rectSize;

            var text = rect.gameObject.AddComponent<Text>();
            text.text = value;
            text.font = font;
            text.fontSize = size;
            text.fontStyle = style;
            text.color = new Color(0.92f, 0.93f, 0.94f);
            text.alignment = anchor;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            return text;
        }

        private static Image AddImage(GameObject go, Color color)
        {
            var image = go.GetComponent<Image>() ?? go.AddComponent<Image>();
            image.color = color;
            return image;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static void SetTopLeft(RectTransform rect, float x, float y, float width, float height)
        {
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(x, -y);
            rect.sizeDelta = new Vector2(width, height);
        }

        private static Color GetTerrainColor(FossickTerrainType terrain)
        {
            if (terrain == FossickTerrainType.Dirt)
            {
                return new Color(0.48f, 0.32f, 0.18f);
            }

            if (terrain == FossickTerrainType.Stone)
            {
                return new Color(0.48f, 0.5f, 0.54f);
            }

            if (terrain == FossickTerrainType.Unbreakable)
            {
                return new Color(0.08f, 0.08f, 0.1f);
            }

            return new Color(0.18f, 0.26f, 0.28f);
        }

        private static Color GetRewardColor(FossickElementType type)
        {
            if (type == FossickElementType.Coin)
            {
                return new Color(0.72f, 0.56f, 0.12f);
            }

            if (type == FossickElementType.Ore)
            {
                return new Color(0.54f, 0.58f, 0.68f);
            }

            if (type == FossickElementType.Item)
            {
                return new Color(0.28f, 0.48f, 0.64f);
            }

            if (type == FossickElementType.Chest)
            {
                return new Color(0.65f, 0.36f, 0.1f);
            }

            if (type == FossickElementType.Collection)
            {
                return new Color(0.6f, 0.25f, 0.7f);
            }

            return new Color(0.1f, 0.12f, 0.14f);
        }

        private static string GetDefaultRewardId(FossickElementType type)
        {
            if (type == FossickElementType.Coin)
            {
                return FossickContentIds.Reward.CoinPileLarge;
            }

            if (type == FossickElementType.Ore)
            {
                return FossickContentIds.Reward.OreCopper;
            }

            if (type == FossickElementType.Item)
            {
                return FossickContentIds.Tool.Pickaxe;
            }

            if (type == FossickElementType.Chest)
            {
                return FossickContentIds.Reward.TreasureChest;
            }

            if (type == FossickElementType.Collection)
            {
                return FossickContentIds.Reward.CollectionBox;
            }

            return null;
        }

    }
}
