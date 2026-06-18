using System;
using System.Collections.Generic;
using Fossick.Core.Board;
using Fossick.Core.Config;
using Fossick.Core.Visual;
using UnityEngine;
using UnityEngine.UI;

namespace Fossick.Runtime.Views
{
    public sealed class FossickBoardView : MonoBehaviour
    {
        private const float SmoothTileOverlap = 2f;
        private const float FogTileOverlap = 0f;
        private const int StoneDamagedSpriteIndex = 19;
        private const string TreasureRoomSmallId = "treasure_room_3x2";
        private const string TreasureRoomMediumId = "treasure_room_5x2";
        private const string TreasureRoomLargeId = "treasure_room";

        [SerializeField] private FossickArtCatalog artCatalog;
        [SerializeField] private FossickCellView cellViewPrefab;
        [SerializeField] private bool showDebugLabels;
        [SerializeField] private float labelWidth = 56f;
        [SerializeField] private int renderRowsAbove = 1;
        [SerializeField] private int renderRowsBelow = 2;

        private readonly List<Image> terrainImages = new List<Image>();
        private readonly List<Image> stoneDamageImages = new List<Image>();
        private readonly List<Image> rewardBackgroundImages = new List<Image>();
        private readonly List<Image> attachmentImages = new List<Image>();
        private readonly List<Image> rewardImages = new List<Image>();
        private readonly List<Image> decorationImages = new List<Image>();
        private readonly List<Image> fogImages = new List<Image>();
        private readonly List<Image> previewImages = new List<Image>();
        private readonly List<Text> rowLabels = new List<Text>();
        private readonly List<FossickCellView> cellViews = new List<FossickCellView>();
        private readonly HashSet<string> previewKeys = new HashSet<string>();

        private RectTransform labelRoot;
        private RectTransform backgroundRoot;
        private RectTransform rewardBackgroundRoot;
        private RectTransform terrainRoot;
        private RectTransform attachmentRoot;
        private RectTransform rewardRoot;
        private RectTransform decorationRoot;
        private RectTransform fogRoot;
        private RectTransform previewRoot;
        private RectTransform interactionRoot;
        private Image backgroundImage;
        private Font font;
        private int currentWidth;
        private int currentHeight;
        private int currentRenderedRowCount;
        private int currentVisibleRowOffset;
        private float currentCellSize;
        private float visualRowOffset;

        private struct RewardBackgroundRegion
        {
            public string id;
            public int startX;
            public int endX;
            public int startY;
            public int endY;
        }

        public event Action<int, int> CellClicked;
        public event Action<int, int> CellPointerEntered;
        public event Action CellPointerExited;
        public event Action<int, int> CellPointerDown;
        public event Action CellPointerUp;

        public float LabelWidth
        {
            get => labelWidth;
            set => labelWidth = Mathf.Max(0f, value);
        }

        public bool ShowDebugLabels
        {
            get => showDebugLabels;
            set => showDebugLabels = value;
        }

        public int RenderRowsAbove
        {
            get => renderRowsAbove;
            set => renderRowsAbove = Mathf.Max(0, value);
        }

        public int RenderRowsBelow
        {
            get => renderRowsBelow;
            set => renderRowsBelow = Mathf.Max(0, value);
        }

        public void SetVisualRowOffset(float rowOffset)
        {
            visualRowOffset = rowOffset;
        }

        public void SetArtCatalog(FossickArtCatalog catalog)
        {
            artCatalog = catalog;
            if (catalog != null)
            {
                FossickArtLibrary.SetActiveCatalog(catalog);
            }
        }

        public void SetFont(Font value)
        {
            font = value;
        }

        public void SetPreviewKeys(IEnumerable<string> keys)
        {
            previewKeys.Clear();
            if (keys == null)
            {
                return;
            }

            foreach (var key in keys)
            {
                previewKeys.Add(key);
            }
        }

        public void Render(FossickBoard board, IEnumerable<string> previewedCells = null)
        {
            if (board == null)
            {
                Clear();
                return;
            }

            if (artCatalog != null)
            {
                FossickArtLibrary.SetActiveCatalog(artCatalog);
            }

            SetPreviewKeys(previewedCells);
            currentWidth = board.Spec.width;
            currentHeight = board.Spec.visibleHeight;
            var firstRenderedRow = Mathf.Max(board.FirstLoadedRow, board.TopVisibleRow - Mathf.Max(0, renderRowsAbove));
            var lastRenderedRow = Mathf.Min(board.RowCount - 1, board.TopVisibleRow + board.Spec.visibleHeight - 1 + Mathf.Max(0, renderRowsBelow));
            if (lastRenderedRow < firstRenderedRow)
            {
                firstRenderedRow = board.TopVisibleRow;
                lastRenderedRow = board.TopVisibleRow + board.Spec.visibleHeight - 1;
            }

            var rows = board.GetRowsWindow(firstRenderedRow, lastRenderedRow - firstRenderedRow + 1);
            currentRenderedRowCount = rows.Count;
            currentVisibleRowOffset = board.TopVisibleRow - firstRenderedRow;

            EnsureRoots();
            EnsureLayout();
            RenderBackground(rows);
            RenderTerrain(rows, currentWidth, currentHeight, currentCellSize);
            RenderCellSpriteLayers(rows, currentCellSize);
            RenderInteractionRows(board, rows, currentCellSize, currentVisibleRowOffset);
        }

        public void RefreshPreviews()
        {
            RenderPreviewLayer(currentWidth, currentHeight, currentCellSize);
        }

        private void Clear()
        {
            for (var i = transform.childCount - 1; i >= 0; i--)
            {
                Destroy(transform.GetChild(i).gameObject);
            }

            terrainImages.Clear();
            stoneDamageImages.Clear();
            rewardBackgroundImages.Clear();
            attachmentImages.Clear();
            rewardImages.Clear();
            decorationImages.Clear();
            fogImages.Clear();
            previewImages.Clear();
            rowLabels.Clear();
            cellViews.Clear();
            currentRenderedRowCount = 0;
            currentVisibleRowOffset = 0;
            labelRoot = null;
            backgroundRoot = null;
            rewardBackgroundRoot = null;
            terrainRoot = null;
            attachmentRoot = null;
            rewardRoot = null;
            decorationRoot = null;
            fogRoot = null;
            previewRoot = null;
            interactionRoot = null;
            backgroundImage = null;
        }

        private void EnsureRoots()
        {
            if (terrainRoot != null)
            {
                return;
            }

            var root = (RectTransform)transform;
            labelRoot = CreateRect("Row Labels", root);
            backgroundRoot = CreateMaskedLayer("0 Background", root);
            rewardBackgroundRoot = CreateMaskedLayer("1 Reward Background", root);
            terrainRoot = CreateMaskedLayer("2 Terrain", root);
            attachmentRoot = CreateMaskedLayer("3 Terrain Attachment", root);
            rewardRoot = CreateMaskedLayer("4 Reward", root);
            decorationRoot = CreateMaskedLayer("5 Decoration", root);
            fogRoot = CreateMaskedLayer("6 Fog", root);
            previewRoot = CreateMaskedLayer("Selection Preview", root);
            interactionRoot = CreateMaskedLayer("Interaction", root);

            backgroundImage = backgroundRoot.gameObject.AddComponent<Image>();
            backgroundImage.raycastTarget = false;
            Stretch(backgroundImage.rectTransform);
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
            SetGridRoot(backgroundRoot, left, top, gridWidth, gridHeight);
            SetGridRoot(rewardBackgroundRoot, left, top, gridWidth, gridHeight);
            SetGridRoot(terrainRoot, left, top, gridWidth, gridHeight);
            SetGridRoot(attachmentRoot, left, top, gridWidth, gridHeight);
            SetGridRoot(rewardRoot, left, top, gridWidth, gridHeight);
            SetGridRoot(decorationRoot, left, top, gridWidth, gridHeight);
            SetGridRoot(fogRoot, left, top, gridWidth, gridHeight);
            SetGridRoot(previewRoot, left, top, gridWidth, gridHeight);
            SetGridRoot(interactionRoot, left, top, gridWidth, gridHeight);
        }

        private static void SetGridRoot(RectTransform root, float left, float top, float width, float height)
        {
            SetTopLeft(root, left, top, width, height);
        }

        private void RenderBackground(IReadOnlyList<FossickCellState[]> rows)
        {
            var sprite = FindBoardBackground(rows);
            backgroundImage.gameObject.SetActive(true);
            backgroundImage.sprite = sprite;
            backgroundImage.color = sprite == null ? FossickArtLibrary.GetEmptyCellColor() : Color.white;
            backgroundImage.type = Image.Type.Simple;
            backgroundImage.preserveAspect = false;
        }

        private void RenderTerrain(IReadOnlyList<FossickCellState[]> rows, int width, int height, float cellSize)
        {
            var used = 0;
            used = RenderTerrainLayer(rows, width, rows == null ? 0 : rows.Count, cellSize, FossickTerrainType.Dirt, used);
            used = RenderTerrainLayer(rows, width, rows == null ? 0 : rows.Count, cellSize, FossickTerrainType.Stone, used);
            used = RenderTerrainLayer(rows, width, rows == null ? 0 : rows.Count, cellSize, FossickTerrainType.Unbreakable, used);
            DisableUnused(terrainImages, used);
            RenderStoneDamageOverlay(rows, width, cellSize);
        }

        private int RenderTerrainLayer(IReadOnlyList<FossickCellState[]> rows, int width, int height, float cellSize, FossickTerrainType terrain, int startIndex)
        {
            if (!FossickArtLibrary.HasAutoTileSprites(terrain))
            {
                return startIndex;
            }

            var index = startIndex;
            for (var cornerY = 0; cornerY <= height; cornerY++)
            {
                for (var cornerX = 0; cornerX <= width; cornerX++)
                {
                    var assetIndex = FossickArtLibrary.ResolveRuntimeCornerAssetIndex(rows, cornerX, cornerY, terrain);
                    var sprite = FossickArtLibrary.GetAutoTileSprite(terrain, assetIndex);
                    if (sprite == null)
                    {
                        continue;
                    }

                    var image = GetImage(terrainImages, terrainRoot, "Terrain Corner", index++);
                    BindSprite(image, sprite);
                    SetTopLeft(
                        image.rectTransform,
                        (cornerX - 0.5f) * cellSize - SmoothTileOverlap * 0.5f,
                        GetRenderTop(cornerY - 0.5f, cellSize) - SmoothTileOverlap * 0.5f,
                        cellSize + SmoothTileOverlap,
                        cellSize + SmoothTileOverlap);
                }
            }

            return index;
        }

        private void RenderStoneDamageOverlay(IReadOnlyList<FossickCellState[]> rows, int width, float cellSize)
        {
            var sprite = FossickArtLibrary.GetAutoTileSprite(FossickTerrainType.Stone, StoneDamagedSpriteIndex);
            if (sprite == null)
            {
                DisableUnused(stoneDamageImages, 0);
                return;
            }

            var used = 0;
            for (var y = 0; y < rows.Count; y++)
            {
                var row = rows[y];
                for (var x = 0; x < width; x++)
                {
                    var cell = row[x];
                    if (cell == null || !cell.IsContentVisible || cell.terrain != FossickTerrainType.Stone || cell.hp != 1)
                    {
                        continue;
                    }

                    var image = GetImage(stoneDamageImages, terrainRoot, "Stone Damage", used++);
                    BindSprite(image, sprite);
                    SetTopLeft(image.rectTransform, x * cellSize, GetRenderTop(y, cellSize), cellSize, cellSize);
                }
            }

            DisableUnused(stoneDamageImages, used);
        }

        private void RenderCellSpriteLayers(IReadOnlyList<FossickCellState[]> rows, float cellSize)
        {
            var rewardBackgroundCount = RenderRewardBackgroundRegions(rows, cellSize);
            var attachmentCount = 0;
            var rewardCount = 0;
            var decorationCount = 0;

            for (var y = 0; y < rows.Count; y++)
            {
                var row = rows[y];
                for (var x = 0; x < currentWidth; x++)
                {
                    var cell = row[x];
                    var left = x * cellSize;
                    var top = GetRenderTop(y, cellSize);

                    attachmentCount = RenderOptionalSprite(
                        attachmentImages,
                        attachmentRoot,
                        "Terrain Attachment",
                        attachmentCount,
                        GetTerrainAttachmentSprite(cell),
                        cell != null && cell.IsContentVisible,
                        left,
                        top,
                        cellSize,
                        cellSize,
                        true);

                    rewardCount = RenderOptionalSprite(
                        rewardImages,
                        rewardRoot,
                        "Reward",
                        rewardCount,
                        GetRewardSprite(cell),
                        cell != null && cell.IsContentVisible,
                        left,
                        top,
                        cellSize,
                        cellSize,
                        true);

                    decorationCount = RenderOptionalSprite(
                        decorationImages,
                        decorationRoot,
                        "Decoration",
                        decorationCount,
                        GetDecorationSprite(cell),
                        cell != null && cell.IsContentVisible,
                        left,
                        top,
                        cellSize,
                        cellSize,
                        true);

                }
            }

            DisableUnused(rewardBackgroundImages, rewardBackgroundCount);
            DisableUnused(attachmentImages, attachmentCount);
            DisableUnused(rewardImages, rewardCount);
            DisableUnused(decorationImages, decorationCount);
            RenderFogLayer(rows, currentWidth, cellSize);
            RenderPreviewLayer(currentWidth, currentHeight, cellSize);
        }

        private int RenderRewardBackgroundRegions(IReadOnlyList<FossickCellState[]> rows, float cellSize)
        {
            if (rows == null || rows.Count == 0 || currentWidth <= 0)
            {
                DisableUnused(rewardBackgroundImages, 0);
                return 0;
            }

            var finished = BuildRewardBackgroundRegions(rows, currentWidth, rows.Count);

            var used = 0;
            for (var i = 0; i < finished.Count; i++)
            {
                var region = finished[i];
                var sprite = FossickArtLibrary.GetBackgroundSprite(region.id);
                used = RenderOptionalSprite(
                    rewardBackgroundImages,
                    rewardBackgroundRoot,
                    "Reward Background Region",
                    used,
                    sprite,
                    sprite != null,
                    region.startX * cellSize,
                    GetRenderTop(region.startY, cellSize),
                    (region.endX - region.startX + 1) * cellSize,
                    (region.endY - region.startY + 1) * cellSize,
                    false);
            }

            DisableUnused(rewardBackgroundImages, used);
            return used;
        }

        private static List<RewardBackgroundRegion> BuildRewardBackgroundRegions(IReadOnlyList<FossickCellState[]> rows, int width, int height)
        {
            var fixedRegions = BuildFixedRewardBackgroundRegions(rows, width, height);
            var covered = new bool[Mathf.Max(0, height), Mathf.Max(0, width)];
            for (var i = 0; i < fixedRegions.Count; i++)
            {
                var region = fixedRegions[i];
                for (var y = region.startY; y <= region.endY && y < height; y++)
                {
                    for (var x = region.startX; x <= region.endX && x < width; x++)
                    {
                        covered[y, x] = true;
                    }
                }
            }

            var active = new List<RewardBackgroundRegion>();
            var finished = new List<RewardBackgroundRegion>(fixedRegions);

            for (var y = 0; y < height; y++)
            {
                var spans = CollectRewardBackgroundSpans(rows, width, y, covered);
                var nextActive = new List<RewardBackgroundRegion>();
                for (var i = 0; i < spans.Count; i++)
                {
                    var span = spans[i];
                    var activeIndex = FindMatchingRewardBackgroundRegion(active, span);
                    if (activeIndex >= 0)
                    {
                        var region = active[activeIndex];
                        region.endY = y;
                        nextActive.Add(region);
                        active.RemoveAt(activeIndex);
                    }
                    else
                    {
                        nextActive.Add(span);
                    }
                }

                finished.AddRange(active);
                active = nextActive;
            }

            finished.AddRange(active);
            return finished;
        }

        private static List<RewardBackgroundRegion> BuildFixedRewardBackgroundRegions(IReadOnlyList<FossickCellState[]> rows, int width, int height)
        {
            var regions = new List<RewardBackgroundRegion>();
            var covered = new bool[Mathf.Max(0, height), Mathf.Max(0, width)];
            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width; x++)
                {
                    if (covered[y, x])
                    {
                        continue;
                    }

                    var id = GetVisibleRewardBackgroundId(rows, x, y);
                    if (!TryGetRewardBackgroundSize(id, out var roomWidth, out var roomHeight))
                    {
                        continue;
                    }

                    if (x + roomWidth > width || y + roomHeight > height)
                    {
                        continue;
                    }

                    if (!HasRewardBackgroundArea(rows, x, y, roomWidth, roomHeight, id))
                    {
                        continue;
                    }

                    var region = new RewardBackgroundRegion
                    {
                        id = id,
                        startX = x,
                        endX = x + roomWidth - 1,
                        startY = y,
                        endY = y + roomHeight - 1
                    };
                    regions.Add(region);
                    for (var markY = region.startY; markY <= region.endY; markY++)
                    {
                        for (var markX = region.startX; markX <= region.endX; markX++)
                        {
                            covered[markY, markX] = true;
                        }
                    }
                }
            }

            return regions;
        }

        private static bool HasRewardBackgroundArea(IReadOnlyList<FossickCellState[]> rows, int startX, int startY, int width, int height, string id)
        {
            for (var y = startY; y < startY + height; y++)
            {
                for (var x = startX; x < startX + width; x++)
                {
                    if (GetVisibleRewardBackgroundId(rows, x, y) != id)
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        private static List<RewardBackgroundRegion> CollectRewardBackgroundSpans(IReadOnlyList<FossickCellState[]> rows, int width, int y, bool[,] covered)
        {
            var spans = new List<RewardBackgroundRegion>();
            var x = 0;
            while (x < width)
            {
                var id = covered[y, x] ? null : GetVisibleRewardBackgroundId(rows, x, y);
                if (string.IsNullOrEmpty(id) || IsFixedRewardBackgroundId(id))
                {
                    x++;
                    continue;
                }

                var startX = x;
                while (x + 1 < width && !covered[y, x + 1] && GetVisibleRewardBackgroundId(rows, x + 1, y) == id)
                {
                    x++;
                }

                spans.Add(new RewardBackgroundRegion
                {
                    id = id,
                    startX = startX,
                    endX = x,
                    startY = y,
                    endY = y
                });
                x++;
            }

            return spans;
        }

        private static string GetVisibleRewardBackgroundId(IReadOnlyList<FossickCellState[]> rows, int x, int y)
        {
            if (rows == null || y < 0 || y >= rows.Count)
            {
                return null;
            }

            return GetVisibleRewardBackgroundId(rows[y], x);
        }

        private static string GetVisibleRewardBackgroundId(FossickCellState[] row, int x)
        {
            if (row == null || x < 0 || x >= row.Length)
            {
                return null;
            }

            var cell = row[x];
            return cell != null && cell.IsContentVisible ? cell.rewardBackgroundId : null;
        }

        private static bool IsFixedRewardBackgroundId(string id)
        {
            return TryGetRewardBackgroundSize(id, out _, out _);
        }

        private static bool TryGetRewardBackgroundSize(string id, out int width, out int height)
        {
            width = 0;
            height = 0;
            switch (id)
            {
                case TreasureRoomSmallId:
                    width = 3;
                    height = 2;
                    return true;
                case TreasureRoomMediumId:
                    width = 5;
                    height = 2;
                    return true;
                case TreasureRoomLargeId:
                case "treasure_room_7x2":
                    width = 7;
                    height = 2;
                    return true;
                default:
                    return false;
            }
        }

        private static int FindMatchingRewardBackgroundRegion(List<RewardBackgroundRegion> regions, RewardBackgroundRegion span)
        {
            for (var i = 0; i < regions.Count; i++)
            {
                var region = regions[i];
                if (region.id == span.id && region.startX == span.startX && region.endX == span.endX)
                {
                    return i;
                }
            }

            return -1;
        }

        private int RenderOptionalSprite(
            List<Image> pool,
            RectTransform parent,
            string objectName,
            int index,
            Sprite sprite,
            bool visible,
            float left,
            float top,
            float width,
            float height,
            bool preserveAspect)
        {
            if (!visible || sprite == null)
            {
                return index;
            }

            var image = GetImage(pool, parent, objectName, index++);
            BindSprite(image, sprite);
            image.preserveAspect = preserveAspect;
            SetTopLeft(image.rectTransform, left, top, width, height);
            return index;
        }

        private void RenderFogLayer(IReadOnlyList<FossickCellState[]> rows, int width, float cellSize)
        {
            if (rows == null || width <= 0)
            {
                DisableUnused(fogImages, 0);
                return;
            }

            var used = 0;
            for (var cornerY = 0; cornerY <= rows.Count; cornerY++)
            {
                for (var cornerX = 0; cornerX <= width; cornerX++)
                {
                    var assetIndex = FossickArtLibrary.ResolveRuntimeFogCornerAssetIndex(rows, cornerX, cornerY);
                    var sprite = FossickArtLibrary.GetFogAutoTileSprite(assetIndex);
                    if (sprite == null)
                    {
                        continue;
                    }

                    var image = GetImage(fogImages, fogRoot, "Fog Corner", used++);
                    BindSprite(image, sprite);
                    SetTopLeft(
                        image.rectTransform,
                        (cornerX - 0.5f) * cellSize - FogTileOverlap * 0.5f,
                        GetRenderTop(cornerY - 0.5f, cellSize) - FogTileOverlap * 0.5f,
                        cellSize + FogTileOverlap,
                        cellSize + FogTileOverlap);
                }
            }

            DisableUnused(fogImages, used);
        }

        private float GetRenderTop(float renderedRowPosition, float cellSize)
        {
            return (renderedRowPosition - currentVisibleRowOffset + visualRowOffset) * cellSize;
        }

        private void RenderPreviewLayer(int width, int height, float cellSize)
        {
            var used = 0;
            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width; x++)
                {
                    if (!previewKeys.Contains(GetCellKey(x, y)))
                    {
                        continue;
                    }

                    var image = GetImage(previewImages, previewRoot, "Preview", used++);
                    image.gameObject.SetActive(true);
                    image.sprite = null;
                    image.color = FossickArtLibrary.GetPreviewColor();
                    image.type = Image.Type.Simple;
                    image.preserveAspect = false;
                    SetTopLeft(image.rectTransform, x * cellSize, y * cellSize, cellSize, cellSize);
                }
            }

            DisableUnused(previewImages, used);
        }

        private void RenderInteractionRows(FossickBoard board, IReadOnlyList<FossickCellState[]> rows, float cellSize, int visibleRowOffset)
        {
            EnsureRowLabels(currentHeight);
            EnsureCellViews(currentWidth * currentHeight);

            for (var y = 0; y < currentHeight; y++)
            {
                var rowLabel = rowLabels[y];
                rowLabel.gameObject.SetActive(true);
                rowLabel.text = (board.TopVisibleRow + y).ToString("000");
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
                        false,
                        (cellX, cellY) => CellClicked?.Invoke(cellX, cellY),
                        (cellX, cellY) => CellPointerEntered?.Invoke(cellX, cellY),
                        () => CellPointerExited?.Invoke(),
                        (cellX, cellY) => CellPointerDown?.Invoke(cellX, cellY),
                        () => CellPointerUp?.Invoke());
                }
            }

            for (var i = rows.Count; i < rowLabels.Count; i++)
            {
                rowLabels[i].gameObject.SetActive(false);
            }

            for (var i = currentWidth * rows.Count; i < cellViews.Count; i++)
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

        private Image GetImage(List<Image> pool, RectTransform parent, string objectName, int index)
        {
            while (pool.Count <= index)
            {
                var rect = CreateRect(objectName, parent);
                var image = rect.gameObject.AddComponent<Image>();
                image.raycastTarget = false;
                pool.Add(image);
            }

            return pool[index];
        }

        private static void BindSprite(Image image, Sprite sprite)
        {
            image.gameObject.SetActive(true);
            image.sprite = sprite;
            image.color = Color.white;
            image.type = Image.Type.Simple;
            image.preserveAspect = false;
        }

        private static void DisableUnused(List<Image> pool, int used)
        {
            for (var i = used; i < pool.Count; i++)
            {
                pool[i].gameObject.SetActive(false);
            }
        }

        private static Sprite FindBoardBackground(IReadOnlyList<FossickCellState[]> rows)
        {
            if (rows != null)
            {
                for (var y = 0; y < rows.Count; y++)
                {
                    var row = rows[y];
                    if (row == null)
                    {
                        continue;
                    }

                    for (var x = 0; x < row.Length; x++)
                    {
                        var cell = row[x];
                        if (cell == null || string.IsNullOrEmpty(cell.backgroundId))
                        {
                            continue;
                        }

                        var sprite = FossickArtLibrary.GetBackgroundSprite(cell.backgroundId);
                        if (sprite != null)
                        {
                            return sprite;
                        }
                    }
                }
            }

            return FossickArtLibrary.GetBackgroundSprite("mine_default");
        }

        private static Sprite GetRewardBackgroundSprite(FossickCellState cell)
        {
            if (cell == null || string.IsNullOrEmpty(cell.rewardBackgroundId))
            {
                return null;
            }

            return FossickArtLibrary.GetBackgroundSprite(cell.rewardBackgroundId);
        }

        private static Sprite GetRewardSprite(FossickCellState cell)
        {
            if (cell == null || !cell.HasSpawnedReward)
            {
                return null;
            }

            return FossickArtLibrary.GetRewardSprite(cell.reward);
        }

        private static Sprite GetTerrainAttachmentSprite(FossickCellState cell)
        {
            if (cell == null || !cell.HasTerrainAttachedReward)
            {
                return null;
            }

            return FossickArtLibrary.GetTerrainAttachmentSprite(cell.reward, cell.terrain);
        }

        private static Sprite GetDecorationSprite(FossickCellState cell)
        {
            if (cell == null || cell.decorations == null || cell.decorations.Length == 0)
            {
                return null;
            }

            for (var i = 0; i < cell.decorations.Length; i++)
            {
                var sprite = FossickArtLibrary.GetDecorationSprite(cell.decorations[i]);
                if (sprite != null)
                {
                    return sprite;
                }
            }

            return null;
        }

        private static string GetCellKey(int x, int y)
        {
            return x + ":" + y;
        }

        private static RectTransform CreateMaskedLayer(string name, Transform parent)
        {
            var rect = CreateRect(name, parent);
            rect.gameObject.AddComponent<RectMask2D>();
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

        private static void SetTopLeft(RectTransform rect, float left, float top, float width, float height)
        {
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(left, -top);
            rect.sizeDelta = new Vector2(width, height);
        }
    }
}
