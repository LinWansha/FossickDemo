using Fossick.Core.Mine;
using Fossick.Core.Visual;
using UnityEngine;
using UnityEngine.UI;

namespace Fossick.Runtime.Views
{
    public sealed partial class FossickBoardView
    {
        public void SetVisualRowOffset(float rowOffset)
        {
            visualRowOffset = rowOffset;
            ApplyVisualRowOffset();
        }

        public void SetVisualShakeOffset(Vector2 pixelOffset)
        {
            visualShakeOffset = pixelOffset;
            ApplyVisualRowOffset();
        }

        private void EnsureRoots()
        {
            if (terrainRoot != null)
            {
                return;
            }

            var root = (RectTransform)transform;
            labelRoot = CreateRect("Row Labels", root);
            clipRoot = CreateRect("Grid Clip", root);
            clipRoot.gameObject.AddComponent<RectMask2D>();
            backgroundRoot = CreateLayer(GetVisualLayerName(FossickVisualLayer.Background), clipRoot);
            rewardBackgroundRoot = CreateLayer(GetVisualLayerName(FossickVisualLayer.RewardBackground), clipRoot);
            terrainRoot = CreateLayer(GetVisualLayerName(FossickVisualLayer.Terrain), clipRoot);
            terrainAutoTileRoot = CreateLayer("Terrain Auto Tiles", terrainRoot);
            terrainCellSpriteRoot = CreateLayer("Terrain Cell Sprites", terrainRoot);
            attachmentRoot = CreateLayer(GetVisualLayerName(FossickVisualLayer.TerrainAttachment), clipRoot);
            rewardBackEffectRoot = CreateLayer("Reward Back Effect", clipRoot);
            rewardRoot = CreateLayer(GetVisualLayerName(FossickVisualLayer.Entity), clipRoot);
            decorationRoot = CreateLayer(GetVisualLayerName(FossickVisualLayer.Decoration), clipRoot);
            fogRoot = CreateLayer(GetVisualLayerName(FossickVisualLayer.Fog), clipRoot);
            animationRoot = CreateLayer("Animation", clipRoot);
            terrainAnimationRoot = CreateLayer("Terrain", animationRoot);
            toolAnimationRoot = CreateLayer("Tool", animationRoot);
            effectRoot = CreateLayer("Effect", clipRoot);
            interactionRoot = CreateLayer("Interaction", clipRoot);

        }

        private void EnsureLayout()
        {
            var rect = (RectTransform)transform;
            var widthSpace = Mathf.Max(1f, rect.rect.width - labelWidth);
            var heightSpace = Mathf.Max(1f, rect.rect.height);
            var heightByWidth = widthSpace / Mathf.Max(1, currentWidth);
            var heightByHeight = heightSpace / Mathf.Max(1, currentHeight);
            currentCellSize = Mathf.Max(1f, Mathf.Floor(Mathf.Min(heightByWidth, heightByHeight)));

            var gridWidth = currentCellSize * currentWidth;
            var gridHeight = currentCellSize * currentHeight;
            var left = labelWidth + Mathf.Max(0f, (rect.rect.width - labelWidth - gridWidth) * 0.5f);
            var top = Mathf.Max(0f, (rect.rect.height - gridHeight) * 0.5f);

            SetTopLeft(labelRoot, left - labelWidth, top, labelWidth, gridHeight);
            SetTopLeft(clipRoot, left, top, gridWidth, gridHeight);
            SetGridRoot(backgroundRoot, 0f, 0f, gridWidth, gridHeight);
            SetGridRoot(rewardBackgroundRoot, 0f, 0f, gridWidth, gridHeight);
            SetGridRoot(terrainRoot, 0f, 0f, gridWidth, gridHeight);
            SetGridRoot(terrainAutoTileRoot, 0f, 0f, gridWidth, gridHeight);
            SetGridRoot(terrainCellSpriteRoot, 0f, 0f, gridWidth, gridHeight);
            SetGridRoot(attachmentRoot, 0f, 0f, gridWidth, gridHeight);
            SetGridRoot(rewardBackEffectRoot, 0f, 0f, gridWidth, gridHeight);
            SetGridRoot(rewardRoot, 0f, 0f, gridWidth, gridHeight);
            SetGridRoot(decorationRoot, 0f, 0f, gridWidth, gridHeight);
            SetGridRoot(fogRoot, 0f, 0f, gridWidth, gridHeight);
            SetGridRoot(effectRoot, 0f, 0f, gridWidth, gridHeight);
            SetGridRoot(animationRoot, 0f, 0f, gridWidth, gridHeight);
            SetGridRoot(terrainAnimationRoot, 0f, 0f, gridWidth, gridHeight);
            SetGridRoot(toolAnimationRoot, 0f, 0f, gridWidth, gridHeight);
            SetGridRoot(interactionRoot, 0f, 0f, gridWidth, gridHeight);
            ApplyVisualRowOffset();
        }

        private void ApplyVisualRowOffset()
        {
            if (terrainRoot == null || currentCellSize <= 0f)
            {
                return;
            }

            var offset = new Vector2(
                visualShakeOffset.x,
                -visualRowOffset * currentCellSize + visualShakeOffset.y);
            SetLayerOffset(backgroundRoot, offset);
            SetLayerOffset(rewardBackgroundRoot, offset);
            SetLayerOffset(terrainRoot, offset);
            SetLayerOffset(attachmentRoot, offset);
            SetLayerOffset(rewardBackEffectRoot, offset);
            SetLayerOffset(rewardRoot, offset);
            SetLayerOffset(decorationRoot, offset);
            SetLayerOffset(fogRoot, offset);
            SetLayerOffset(effectRoot, offset);
            SetLayerOffset(animationRoot, offset);
        }

        private static void SetLayerOffset(RectTransform root, Vector2 offset)
        {
            var position = root.anchoredPosition;
            position.x = offset.x;
            position.y = offset.y;
            root.anchoredPosition = position;
        }

        private RectTransform GetAnimationLayerRoot(FossickBoardAnimationLayer layer)
        {
            switch (layer)
            {
                case FossickBoardAnimationLayer.Terrain:
                    return terrainAnimationRoot;
                case FossickBoardAnimationLayer.Reward:
                    return rewardRoot;
                case FossickBoardAnimationLayer.Tool:
                    return toolAnimationRoot;
                default:
                    return animationRoot;
            }
        }

        private RectTransform GetEffectLayerRoot(FossickBoardEffectLayer layer)
        {
            return layer == FossickBoardEffectLayer.RewardBack
                ? rewardBackEffectRoot
                : effectRoot;
        }

        private static string GetVisualLayerName(FossickVisualLayer layer)
        {
            switch (layer)
            {
                case FossickVisualLayer.Background:
                    return "0 Background";
                case FossickVisualLayer.RewardBackground:
                    return "1 Reward Background";
                case FossickVisualLayer.Terrain:
                    return "2 Terrain";
                case FossickVisualLayer.TerrainAttachment:
                    return "3 Terrain Attachment";
                case FossickVisualLayer.Entity:
                    return "4 Entity";
                case FossickVisualLayer.Decoration:
                    return "5 Decoration";
                case FossickVisualLayer.Fog:
                    return "6 Fog";
                default:
                    return ((int)layer) + " " + layer;
            }
        }

        private static RectTransform CreateLayer(string name, Transform parent)
        {
            var rect = CreateRect(name, parent);
            return rect;
        }

        private static RectTransform CreateRect(string name, Transform parent)
        {
            var obj = new GameObject(name, typeof(RectTransform));
            obj.transform.SetParent(parent, false);
            return obj.GetComponent<RectTransform>();
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static void SetGridRoot(RectTransform root, float left, float top, float width, float height)
        {
            SetTopLeft(root, left, top, width, height);
        }

        private static void SetTopLeft(RectTransform rect, float left, float top, float width, float height)
        {
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(Mathf.Round(left), -Mathf.Round(top));
            rect.sizeDelta = new Vector2(Mathf.Round(width), Mathf.Round(height));
        }
    }
}
