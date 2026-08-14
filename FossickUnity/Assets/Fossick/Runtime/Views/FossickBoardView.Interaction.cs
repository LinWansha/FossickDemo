using System.Collections.Generic;
using Fossick.Core.Application.Results;
using Fossick.Core.Mine;
using Fossick.Core.Visual;
using UnityEngine;
using UnityEngine.UI;

namespace Fossick.Runtime.Views
{
    public sealed partial class FossickBoardView
    {
        public RectTransform AnimationRoot
        {
            get
            {
                EnsureRoots();
                return animationRoot;
            }
        }

        public RectTransform EffectRoot
        {
            get
            {
                EnsureRoots();
                return effectRoot;
            }
        }

        public void ShowEffectRangeHighlights(IReadOnlyList<FossickToolTarget> targets, int topVisibleRow)
        {
            ClearEffectRangeHighlights();
            var color = FossickArtLibrary.GetEffectHighlightColor();
            for (var i = 0; i < targets.Count; i++)
            {
                var target = targets[i];
                var localY = target.y - topVisibleRow;
                if (target.x < 0 || target.x >= currentWidth || localY < 0 || localY >= currentHeight)
                {
                    continue;
                }

                cellViews[localY * currentWidth + target.x].SetEffectHighlighted(true, color);
            }
        }

        public void SetEffectRangeHighlightAlpha(float alpha)
        {
            for (var i = 0; i < cellViews.Count; i++)
            {
                cellViews[i].SetEffectHighlightAlpha(alpha);
            }
        }

        public void ClearEffectRangeHighlights()
        {
            var color = FossickArtLibrary.GetEffectHighlightColor();
            for (var i = 0; i < cellViews.Count; i++)
            {
                cellViews[i].SetEffectHighlighted(false, color);
            }
        }

        public bool PlaceAnimation(
            RectTransform target,
            int x,
            int absoluteY,
            int topVisibleRow,
            FossickBoardAnimationLayer layer)
        {
            if (target == null || currentCellSize <= 0f || x < 0 || x >= currentWidth)
            {
                return false;
            }

            var localY = absoluteY - topVisibleRow;
            if (localY < -renderRowsAbove || localY >= currentHeight + renderRowsBelow)
            {
                return false;
            }

            var parent = GetAnimationLayerRoot(layer);
            if (target.parent != parent)
            {
                target.SetParent(parent, false);
            }
            target.anchorMin = new Vector2(0f, 1f);
            target.anchorMax = new Vector2(0f, 1f);
            target.pivot = new Vector2(0.5f, 0.5f);
            target.anchoredPosition = new Vector2(
                (x + 0.5f) * currentCellSize,
                -(localY + 0.5f) * currentCellSize);
            return true;
        }

        public bool PlaceEffect(
            Transform target,
            int x,
            int absoluteY,
            int topVisibleRow,
            FossickBoardEffectLayer layer)
        {
            if (target == null || currentCellSize <= 0f || x < 0 || x >= currentWidth)
            {
                return false;
            }

            var localY = absoluteY - topVisibleRow;
            if (localY < -renderRowsAbove || localY >= currentHeight + renderRowsBelow)
            {
                return false;
            }

            var parent = GetEffectLayerRoot(layer);
            if (target.parent != parent)
            {
                target.SetParent(parent, false);
            }
            target.localPosition = new Vector3(
                (x + 0.5f) * currentCellSize,
                -(localY + 0.5f) * currentCellSize,
                0f);
            return true;
        }

        public bool TryPrepareEntityDrop(
            FossickEntityDrop drop,
            out RectTransform target,
            out Vector2 startPosition,
            out Vector2 endPosition)
        {
            target = null;
            startPosition = default;
            endPosition = default;
            if (drop == null || currentCellSize <= 0f ||
                !rewardRects.TryGetValue(new FossickPosition(drop.toX, drop.toY), out target) ||
                !target.gameObject.activeInHierarchy)
            {
                return false;
            }

            endPosition = target.anchoredPosition;
            startPosition = endPosition + new Vector2(
                (drop.fromX - drop.toX) * currentCellSize,
                -(drop.fromY - drop.toY) * currentCellSize);
            target.anchoredPosition = startPosition;
            return true;
        }

        public bool TryGetRewardMotionTarget(
            FossickPosition position,
            out RectTransform target,
            out Vector2 restingPosition)
        {
            restingPosition = default;
            if (!rewardRects.TryGetValue(position, out target) || !target.gameObject.activeInHierarchy)
            {
                return false;
            }

            restingPosition = target.anchoredPosition;
            return true;
        }

        public void SetRewardImageVisible(FossickPosition position, bool visible)
        {
            if (rewardImageViews.TryGetValue(position, out var image))
            {
                image.enabled = visible;
            }
        }

        public bool AttachRewardVisual(FossickPosition position, RectTransform visual)
        {
            if (visual == null ||
                !PlaceAnimation(
                    visual,
                    position.x,
                    position.y,
                    currentFirstRenderedRow + currentVisibleRowOffset,
                    FossickBoardAnimationLayer.Reward))
            {
                return false;
            }

            rewardRects[position] = visual;
            return true;
        }

        public bool TryCreateRewardFlySource(int x, int y, out GameObject source)
        {
            source = null;
            var position = new FossickPosition(x, y);
            if (!rewardRects.TryGetValue(position, out var rewardRect) ||
                !rewardRect.gameObject.activeInHierarchy ||
                !rewardFlySprites.TryGetValue(position, out var sprite))
            {
                return false;
            }

            source = new GameObject("Reward Fly Source", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            var sourceRect = source.GetComponent<RectTransform>();
            var sourceImage = source.GetComponent<Image>();
            sourceRect.SetParent(animationRoot, false);
            sourceRect.sizeDelta = new Vector2(currentCellSize, currentCellSize);
            sourceRect.position = rewardRect.TransformPoint(rewardRect.rect.center);
            sourceImage.sprite = sprite;
            sourceImage.preserveAspect = true;
            sourceImage.raycastTarget = false;
            return true;
        }

        private void RenderInteractionRows(
            FossickMine mine,
            IReadOnlyList<FossickCellRenderData[]> rows,
            float cellSize,
            int visibleRowOffset)
        {
            EnsureRowLabels(currentHeight);
            EnsureCellViews(currentWidth * currentHeight);

            for (var y = 0; y < currentHeight; y++)
            {
                var rowLabel = rowLabels[y];
                rowLabel.gameObject.SetActive(true);
                rowLabel.text = (mine.TopVisibleRow + y).ToString("000");
                rowLabel.font = font;
                rowLabel.fontSize = 16;
                rowLabel.fontStyle = FontStyle.Bold;
                rowLabel.alignment = TextAnchor.MiddleCenter;
                rowLabel.color = new Color(0.68f, 0.72f, 0.76f);
                SetTopLeft(rowLabel.rectTransform, 0f, y * cellSize, labelWidth, cellSize);

                var sourceY = visibleRowOffset + y;
                var row = rows != null && sourceY >= 0 && sourceY < rows.Count ? rows[sourceY] : null;
                for (var x = 0; x < currentWidth; x++)
                {
                    var cellView = cellViews[y * currentWidth + x];
                    cellView.gameObject.SetActive(true);
                    SetTopLeft(cellView.RectTransform, x * cellSize, y * cellSize, cellSize, cellSize);
                    cellView.Bind(
                        row == null || x < 0 || x >= row.Length ? null : row[x],
                        x,
                        y,
                        font,
                        showDebugLabels,
                        (cellX, cellY) => CellClicked?.Invoke(cellX, cellY),
                        (cellX, cellY) => CellPointerDown?.Invoke(cellX, cellY),
                        () => CellPointerUp?.Invoke());
                }
            }

            for (var i = currentHeight; i < rowLabels.Count; i++)
            {
                rowLabels[i].gameObject.SetActive(false);
            }

            for (var i = currentWidth * currentHeight; i < cellViews.Count; i++)
            {
                cellViews[i].gameObject.SetActive(false);
            }
        }

        private void EnsureRowLabels(int count)
        {
            while (rowLabels.Count < count)
            {
                var rect = CreateRect("Row Label", labelRoot);
                var text = rect.gameObject.AddComponent<Text>();
                text.raycastTarget = false;
                rowLabels.Add(text);
            }
        }

        private void EnsureCellViews(int count)
        {
            while (cellViews.Count < count)
            {
                cellViews.Add(CreateCellView());
            }
        }

        public GameObject GetCellTarget(int x, int y)
        {
            if (x < 0 || x >= currentWidth || y < 0 || y >= currentHeight)
            {
                return null;
            }

            var index = y * currentWidth + x;
            return index < cellViews.Count && cellViews[index].gameObject.activeInHierarchy
                ? cellViews[index].gameObject
                : null;
        }

        private FossickCellView CreateCellView()
        {
            if (cellViewPrefab != null)
            {
                return Instantiate(cellViewPrefab, interactionRoot);
            }

            var rect = CreateRect("Cell View", interactionRoot);
            var image = rect.gameObject.AddComponent<Image>();
            image.color = Color.clear;
            image.raycastTarget = true;
            return rect.gameObject.AddComponent<FossickCellView>();
        }
    }
}
