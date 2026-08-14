using System;
using System.Collections.Generic;
using Fossick.Core.Application.Results;
using Fossick.Core.Definition.Config;
using Fossick.Core.Generation;
using Fossick.Core.Mine;
using Fossick.Core.Mine.Objects;
using Fossick.Core.Visual;
using Fossick.Core.Visual.Tiling;
using UnityEngine;
using UnityEngine.UI;

namespace Fossick.Runtime.Views
{
    public enum FossickBoardAnimationLayer
    {
        Terrain,
        Reward,
        Tool
    }

    public enum FossickBoardEffectLayer
    {
        RewardBack,
        Front
    }

    public sealed partial class FossickBoardView : MonoBehaviour
    {
        private sealed class RewardEntityView
        {
            public RectTransform root;
            public Image image;
        }

        private const float SmoothTileOverlap = 2f;
        private const float FogTileOverlap = 0f;
        private const int StoneDamagedSpriteIndex = 19;
        private static readonly FossickTerrainType[] AutoTileTerrainTypes =
        {
            FossickTerrainType.Dirt,
            FossickTerrainType.Stone,
            FossickTerrainType.Unbreakable
        };

        [SerializeField] private FossickArtCatalog artCatalog;
        [SerializeField] private FossickCellView cellViewPrefab;
        [SerializeField] private bool showDebugLabels;
        [SerializeField] private float labelWidth;
        [SerializeField] private int renderRowsAbove = 1;
        [SerializeField] private int renderRowsBelow = 2;

        private readonly List<Image> backgroundImages = new List<Image>();
        private readonly List<Image> terrainAutoTileImages = new List<Image>();
        private readonly List<Image> terrainCellSpriteImages = new List<Image>();
        private readonly List<Image> rewardBackgroundImages = new List<Image>();
        private readonly List<Image> attachmentImages = new List<Image>();
        private readonly List<RewardEntityView> rewardEntityViews = new List<RewardEntityView>();
        private readonly List<Image> decorationImages = new List<Image>();
        private readonly List<Image> fogImages = new List<Image>();
        private readonly List<Text> rowLabels = new List<Text>();
        private readonly List<FossickCellView> cellViews = new List<FossickCellView>();
        private readonly List<FossickCellRenderData[]> renderedRows = new List<FossickCellRenderData[]>();
        private readonly HashSet<int> affectedTerrainCells = new HashSet<int>();
        private readonly HashSet<int> affectedTerrainCorners = new HashSet<int>();
        private readonly Dictionary<FossickPosition, RectTransform> rewardRects =
            new Dictionary<FossickPosition, RectTransform>();
        private readonly Dictionary<FossickPosition, Image> rewardImageViews =
            new Dictionary<FossickPosition, Image>();
        private readonly Dictionary<FossickPosition, Sprite> rewardFlySprites =
            new Dictionary<FossickPosition, Sprite>();

        private RectTransform labelRoot;
        private RectTransform clipRoot;
        private RectTransform backgroundRoot;
        private RectTransform rewardBackgroundRoot;
        private RectTransform terrainRoot;
        private RectTransform terrainAutoTileRoot;
        private RectTransform terrainCellSpriteRoot;
        private RectTransform attachmentRoot;
        private RectTransform rewardBackEffectRoot;
        private RectTransform rewardRoot;
        private RectTransform decorationRoot;
        private RectTransform fogRoot;
        private RectTransform effectRoot;
        private RectTransform animationRoot;
        private RectTransform terrainAnimationRoot;
        private RectTransform toolAnimationRoot;
        private RectTransform interactionRoot;
        private Font font;
        private int currentWidth;
        private int currentHeight;
        private int currentRenderedRowCount;
        private int currentVisibleRowOffset;
        private int currentFirstRenderedRow;
        private float currentCellSize;
        private float visualRowOffset;
        private Vector2 visualShakeOffset;
        private Func<FossickEntityPayload, bool> persistentRewardVisualResolver;

        public event Action<int, int> CellClicked;
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

        public void SetPersistentRewardVisualResolver(Func<FossickEntityPayload, bool> resolver)
        {
            persistentRewardVisualResolver = resolver;
        }

        public void Render(FossickMine mine)
        {
            if (mine == null)
            {
                Clear();
                return;
            }

            if (artCatalog != null)
            {
                FossickArtLibrary.SetActiveCatalog(artCatalog);
            }

            currentWidth = mine.Spec.width;
            currentHeight = mine.Spec.visibleHeight;
            var firstRenderedRow = Mathf.Max(mine.FirstLoadedRow, mine.TopVisibleRow - Mathf.Max(0, renderRowsAbove));
            var lastRenderedRow = Mathf.Min(mine.RowCount - 1, mine.TopVisibleRow + mine.Spec.visibleHeight - 1 + Mathf.Max(0, renderRowsBelow));
            if (lastRenderedRow < firstRenderedRow)
            {
                firstRenderedRow = mine.TopVisibleRow;
                lastRenderedRow = mine.TopVisibleRow + mine.Spec.visibleHeight - 1;
            }

            var runtimeRows = mine.GetRowsWindow(firstRenderedRow, lastRenderedRow - firstRenderedRow + 1);
            var rows = ConvertRows(runtimeRows);
            renderedRows.Clear();
            renderedRows.AddRange(rows);
            currentRenderedRowCount = rows.Count;
            currentFirstRenderedRow = firstRenderedRow;
            currentVisibleRowOffset = mine.TopVisibleRow - firstRenderedRow;

            EnsureRoots();
            EnsureLayout();
            RenderBackground(mine);
            RenderTerrain(rows, currentWidth, currentCellSize);
            RenderCellSpriteLayers(mine, rows, currentCellSize);
            RenderInteractionRows(mine, rows, currentCellSize, currentVisibleRowOffset);
        }

        public void UpdateTerrainPresentation(IReadOnlyList<FossickCellDelta> deltas)
        {
            affectedTerrainCells.Clear();
            affectedTerrainCorners.Clear();
            for (var i = 0; i < deltas.Count; i++)
            {
                var delta = deltas[i];
                var rowIndex = delta.y - currentFirstRenderedRow;
                if (rowIndex < 0 || rowIndex >= renderedRows.Count)
                {
                    continue;
                }

                var row = renderedRows[rowIndex];
                if (row == null || delta.x < 0 || delta.x >= row.Length)
                {
                    continue;
                }

                var cell = row[delta.x];
                if (cell == null)
                {
                    continue;
                }

                cell.terrain = delta.terrainAfter;
                cell.hp = delta.hpAfter;
                affectedTerrainCells.Add(rowIndex * currentWidth + delta.x);
                AddAffectedTerrainCorners(delta.x, rowIndex);
            }

            foreach (var cellIndex in affectedTerrainCells)
            {
                var rowIndex = cellIndex / currentWidth;
                var x = cellIndex % currentWidth;
                RenderTerrainCell(renderedRows, x, rowIndex, currentWidth, currentCellSize);
            }

            var cornerWidth = currentWidth + 1;
            foreach (var cornerIndex in affectedTerrainCorners)
            {
                var cornerY = cornerIndex / cornerWidth;
                var cornerX = cornerIndex % cornerWidth;
                RenderTerrainCorner(renderedRows, cornerX, cornerY, currentWidth, currentCellSize);
            }
        }

        private void AddAffectedTerrainCorners(int x, int rowIndex)
        {
            var cornerWidth = currentWidth + 1;
            affectedTerrainCorners.Add(rowIndex * cornerWidth + x);
            affectedTerrainCorners.Add(rowIndex * cornerWidth + x + 1);
            affectedTerrainCorners.Add((rowIndex + 1) * cornerWidth + x);
            affectedTerrainCorners.Add((rowIndex + 1) * cornerWidth + x + 1);
        }

        private void Clear()
        {
            for (var i = transform.childCount - 1; i >= 0; i--)
            {
                Destroy(transform.GetChild(i).gameObject);
            }

            backgroundImages.Clear();
            terrainAutoTileImages.Clear();
            terrainCellSpriteImages.Clear();
            rewardBackgroundImages.Clear();
            attachmentImages.Clear();
            terrainAttachmentPrefabPools.Clear();
            rewardEntityViews.Clear();
            decorationImages.Clear();
            fogImages.Clear();
            rowLabels.Clear();
            cellViews.Clear();
            renderedRows.Clear();
            affectedTerrainCells.Clear();
            affectedTerrainCorners.Clear();
            rewardRects.Clear();
            rewardImageViews.Clear();
            rewardFlySprites.Clear();
            currentRenderedRowCount = 0;
            currentVisibleRowOffset = 0;
            currentFirstRenderedRow = 0;
            labelRoot = null;
            clipRoot = null;
            backgroundRoot = null;
            rewardBackgroundRoot = null;
            terrainRoot = null;
            terrainAutoTileRoot = null;
            terrainCellSpriteRoot = null;
            attachmentRoot = null;
            rewardRoot = null;
            decorationRoot = null;
            fogRoot = null;
            effectRoot = null;
            animationRoot = null;
            terrainAnimationRoot = null;
            toolAnimationRoot = null;
            interactionRoot = null;
        }

        private void RenderBackground(FossickMine mine)
        {
            var regionHeight = FossickBackgroundLayout.RegionHeight;
            var firstRegionRow = mine.BackgroundLayout.GetRegionStartRow(currentFirstRenderedRow);
            var lastRenderedRow = currentFirstRenderedRow + currentRenderedRowCount - 1;
            var used = 0;
            for (var row = firstRegionRow; row <= lastRenderedRow; row += regionHeight)
            {
                var backgroundId = mine.BackgroundLayout.GetBackgroundId(row);
                var sprite = FossickArtLibrary.GetBackgroundSprite(backgroundId);
                var image = GetImage(backgroundImages, backgroundRoot, "Mine Background " + backgroundId, used++);
                BindSprite(image, sprite);
                image.color = sprite == null ? FossickArtLibrary.GetEmptyCellColor() : Color.white;
                SetTopLeft(
                    image.rectTransform,
                    0f,
                    GetRenderTop(row - currentFirstRenderedRow, currentCellSize),
                    currentWidth * currentCellSize,
                    regionHeight * currentCellSize);
            }

            DisableUnused(backgroundImages, used);
        }

        private void RenderTerrain(IReadOnlyList<FossickCellRenderData[]> rows, int width, float cellSize)
        {
            var rowCount = rows == null ? 0 : rows.Count;
            for (var cornerY = 0; cornerY <= rowCount; cornerY++)
            {
                for (var cornerX = 0; cornerX <= width; cornerX++)
                {
                    RenderTerrainCorner(rows, cornerX, cornerY, width, cellSize);
                }
            }

            DisableUnused(
                terrainAutoTileImages,
                AutoTileTerrainTypes.Length * (width + 1) * (rowCount + 1));

            for (var y = 0; y < rowCount; y++)
            {
                for (var x = 0; x < width; x++)
                {
                    RenderTerrainCell(rows, x, y, width, cellSize);
                }
            }

            DisableUnused(terrainCellSpriteImages, width * rowCount * 2);
        }

        private void RenderTerrainCorner(
            IReadOnlyList<FossickCellRenderData[]> rows,
            int cornerX,
            int cornerY,
            int width,
            float cellSize)
        {
            var cornerCount = (width + 1) * ((rows == null ? 0 : rows.Count) + 1);
            var cornerIndex = cornerY * (width + 1) + cornerX;
            for (var terrainIndex = 0; terrainIndex < AutoTileTerrainTypes.Length; terrainIndex++)
            {
                var terrain = AutoTileTerrainTypes[terrainIndex];
                var imageIndex = terrainIndex * cornerCount + cornerIndex;
                var image = GetImage(
                    terrainAutoTileImages,
                    terrainAutoTileRoot,
                    terrain + " Auto Tile",
                    imageIndex);
                var assetIndex = ResolveRenderCornerAssetIndex(rows, cornerX, cornerY, terrain);
                var sprite = FossickArtLibrary.GetAutoTileSprite(terrain, assetIndex);
                if (sprite == null)
                {
                    image.gameObject.SetActive(false);
                    continue;
                }

                BindSprite(image, sprite);
                SetTopLeft(
                    image.rectTransform,
                    (cornerX - 0.5f) * cellSize - SmoothTileOverlap * 0.5f,
                    GetRenderTop(cornerY - 0.5f, cellSize) - SmoothTileOverlap * 0.5f,
                    cellSize + SmoothTileOverlap,
                    cellSize + SmoothTileOverlap);
            }
        }

        private void RenderTerrainCell(
            IReadOnlyList<FossickCellRenderData[]> rows,
            int x,
            int y,
            int width,
            float cellSize)
        {
            var rowCount = rows == null ? 0 : rows.Count;
            var cellCount = width * rowCount;
            var cellIndex = y * width + x;
            var cell = rows != null && y >= 0 && y < rowCount && rows[y] != null
                ? rows[y][x]
                : null;

            var terrainImage = GetImage(
                terrainCellSpriteImages,
                terrainCellSpriteRoot,
                "Terrain Cell Sprite",
                cellIndex);
            var terrainSprite = cell == null ||
                                !cell.isContentVisible ||
                                cell.terrain == FossickTerrainType.Empty ||
                                FossickArtLibrary.HasAutoTileSprites(cell.terrain)
                ? null
                : FossickArtLibrary.GetTerrainSprite(cell.terrain);
            if (terrainSprite == null)
            {
                terrainImage.gameObject.SetActive(false);
            }
            else
            {
                BindSprite(terrainImage, terrainSprite);
                SetTopLeft(
                    terrainImage.rectTransform,
                    x * cellSize,
                    GetRenderTop(y, cellSize),
                    cellSize,
                    cellSize);
            }

            var damageImage = GetImage(
                terrainCellSpriteImages,
                terrainCellSpriteRoot,
                "Stone Damage",
                cellCount + cellIndex);
            var damageSprite = cell != null &&
                               cell.isContentVisible &&
                               cell.terrain == FossickTerrainType.Stone &&
                               cell.hp == 1
                ? FossickArtLibrary.GetAutoTileSprite(FossickTerrainType.Stone, StoneDamagedSpriteIndex)
                : null;
            if (damageSprite == null)
            {
                damageImage.gameObject.SetActive(false);
            }
            else
            {
                BindSprite(damageImage, damageSprite);
                SetTopLeft(
                    damageImage.rectTransform,
                    x * cellSize,
                    GetRenderTop(y, cellSize),
                    cellSize,
                    cellSize);
            }
        }

        private void RenderCellSpriteLayers(FossickMine mine, IReadOnlyList<FossickCellRenderData[]> rows, float cellSize)
        {
            rewardRects.Clear();
            rewardImageViews.Clear();
            rewardFlySprites.Clear();
            var rewardBackgroundCount = RenderRewardBackgroundRegions(mine, cellSize);
            var attachmentCount = 0;
            var rewardViewCount = 0;
            var decorationCount = 0;
            foreach (var pool in terrainAttachmentPrefabPools.Values)
            {
                pool.used = 0;
            }

            for (var y = 0; y < rows.Count; y++)
            {
                var row = rows[y];
                for (var x = 0; x < currentWidth; x++)
                {
                    var cell = row[x];
                    var left = x * cellSize;
                    var top = GetRenderTop(y, cellSize);

                    if (IsOreTerrainAttachment(cell) && terrainAttachmentFactory != null)
                    {
                        if (cell.isContentVisible)
                        {
                            RenderTerrainAttachmentPrefab(cell.embeddedPayload.Id, left, top, cellSize);
                        }
                    }
                    else
                    {
                        attachmentCount = RenderOptionalSprite(
                            attachmentImages,
                            attachmentRoot,
                            "Terrain Attachment",
                            attachmentCount,
                            GetTerrainAttachmentSprite(cell),
                            cell != null && cell.isContentVisible,
                            left,
                            top,
                            cellSize,
                            cellSize,
                            true);
                    }

                    if (cell != null && cell.isContentVisible && cell.pickupPayload != null)
                    {
                        var rewardPosition = new FossickPosition(x, currentFirstRenderedRow + y);
                        var rewardSprite = GetEntitySprite(cell);
                        var usesPersistentVisual =
                            persistentRewardVisualResolver?.Invoke(cell.pickupPayload) == true;

                        if (rewardSprite != null)
                        {
                            rewardFlySprites[rewardPosition] = rewardSprite;
                        }

                        if (!usesPersistentVisual)
                        {
                            var entityView = GetRewardEntityView(
                                rewardViewCount++,
                                GetRewardEntityName(cell.pickupPayload));
                            SetTopLeft(entityView.root, left, top, cellSize, cellSize);
                            rewardRects[rewardPosition] = entityView.root;

                            if (entityView.image != null)
                            {
                                entityView.image.gameObject.SetActive(false);
                            }

                            if (rewardSprite != null)
                            {
                                var image = GetRewardImage(entityView);
                                BindSprite(image, rewardSprite);
                                image.preserveAspect = true;
                                rewardImageViews[rewardPosition] = image;
                            }
                        }
                    }

                    decorationCount = RenderOptionalSprite(
                        decorationImages,
                        decorationRoot,
                        "Decoration",
                        decorationCount,
                        GetDecorationSprite(cell),
                        cell != null && cell.isContentVisible,
                        left,
                        top,
                        cellSize,
                        cellSize,
                        true);

                }
            }

            DisableUnused(rewardBackgroundImages, rewardBackgroundCount);
            DisableUnused(attachmentImages, attachmentCount);
            DisableUnusedTerrainAttachmentPrefabs();
            DisableUnusedRewardEntityViews(rewardViewCount);
            DisableUnused(decorationImages, decorationCount);
            RenderFogLayer(rows, currentWidth, cellSize);
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

        private void RenderFogLayer(IReadOnlyList<FossickCellRenderData[]> rows, int width, float cellSize)
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
                    var assetIndex = ResolveRenderFogCornerAssetIndex(rows, cornerX, cornerY);
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
            return (renderedRowPosition - currentVisibleRowOffset) * cellSize;
        }

        private static List<FossickCellRenderData[]> ConvertRows(IReadOnlyList<FossickCell[]> runtimeRows)
        {
            var rows = new List<FossickCellRenderData[]>();
            if (runtimeRows == null)
            {
                return rows;
            }

            for (var y = 0; y < runtimeRows.Count; y++)
            {
                var runtimeRow = runtimeRows[y];
                if (runtimeRow == null)
                {
                    rows.Add(null);
                    continue;
                }

                var row = new FossickCellRenderData[runtimeRow.Length];
                for (var x = 0; x < runtimeRow.Length; x++)
                {
                    row[x] = ConvertCell(runtimeRow[x]);
                }

                rows.Add(row);
            }

            return rows;
        }

        private static FossickCellRenderData ConvertCell(FossickCell cell) => FossickCellRenderData.FromCell(cell);

        private Image GetImage(List<Image> pool, RectTransform parent, string objectName, int index)
        {
            while (pool.Count <= index)
            {
                var rect = CreateRect(objectName, parent);
                var image = rect.gameObject.AddComponent<Image>();
                image.raycastTarget = false;
                pool.Add(image);
            }

            var pooledImage = pool[index];
            pooledImage.gameObject.name = objectName;
            return pooledImage;
        }

        private RewardEntityView GetRewardEntityView(int index, string objectName)
        {
            while (rewardEntityViews.Count <= index)
            {
                rewardEntityViews.Add(new RewardEntityView
                {
                    root = CreateRect(objectName, rewardRoot)
                });
            }

            var entityView = rewardEntityViews[index];
            entityView.root.gameObject.name = objectName;
            entityView.root.gameObject.SetActive(true);
            return entityView;
        }

        private static string GetRewardEntityName(FossickEntityPayload payload)
        {
            return string.IsNullOrEmpty(payload.Id) ? payload.ElementType.ToString() : payload.Id;
        }

        private static Image GetRewardImage(RewardEntityView entityView)
        {
            if (entityView.image == null)
            {
                var imageRoot = CreateRect("Image", entityView.root);
                Stretch(imageRoot);
                entityView.image = imageRoot.gameObject.AddComponent<Image>();
                entityView.image.raycastTarget = false;
            }

            return entityView.image;
        }

        private void DisableUnusedRewardEntityViews(int used)
        {
            for (var i = used; i < rewardEntityViews.Count; i++)
            {
                rewardEntityViews[i].root.gameObject.SetActive(false);
            }
        }

        private static void BindSprite(Image image, Sprite sprite)
        {
            image.gameObject.SetActive(true);
            image.enabled = true;
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

        private static Sprite GetEntitySprite(FossickCellRenderData cell)
        {
            if (cell == null || !cell.HasSpawnedReward)
            {
                return null;
            }

            return FossickArtLibrary.GetEntitySprite(ToElementConfig(cell.pickupPayload));
        }

        private static Sprite GetTerrainAttachmentSprite(FossickCellRenderData cell)
        {
            if (cell == null || !cell.HasTerrainAttachedReward)
            {
                return null;
            }

            return FossickArtLibrary.GetTerrainAttachmentSprite(ToElementConfig(cell.embeddedPayload), cell.terrain);
        }

        private static Sprite GetDecorationSprite(FossickCellRenderData cell)
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

        private static FossickElementConfig ToElementConfig(FossickEntityPayload payload)
        {
            return payload == null
                ? null
                : new FossickElementConfig
                {
                    type = payload.ElementType,
                    id = payload.Id
                };
        }

        private static int ResolveRenderCornerAssetIndex(IReadOnlyList<FossickCellRenderData[]> rows, int cornerX, int cornerY, FossickTerrainType terrain)
        {
            if (rows == null || terrain == FossickTerrainType.Empty)
            {
                return 0;
            }

            return FossickAutoTileResolver.ResolveCornerAssetIndex(
                RenderCellMatches(rows, cornerX - 1, cornerY - 1, terrain),
                RenderCellMatches(rows, cornerX, cornerY - 1, terrain),
                RenderCellMatches(rows, cornerX - 1, cornerY, terrain),
                RenderCellMatches(rows, cornerX, cornerY, terrain));
        }

        private static int ResolveRenderFogCornerAssetIndex(IReadOnlyList<FossickCellRenderData[]> rows, int cornerX, int cornerY)
        {
            if (rows == null)
            {
                return 0;
            }

            return FossickAutoTileResolver.ResolveCornerAssetIndex(
                RenderCellIsFogged(rows, cornerX - 1, cornerY - 1),
                RenderCellIsFogged(rows, cornerX, cornerY - 1),
                RenderCellIsFogged(rows, cornerX - 1, cornerY),
                RenderCellIsFogged(rows, cornerX, cornerY));
        }

        private static bool RenderCellMatches(IReadOnlyList<FossickCellRenderData[]> rows, int x, int y, FossickTerrainType terrain)
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

        private static bool RenderCellIsFogged(IReadOnlyList<FossickCellRenderData[]> rows, int x, int y)
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
            return cell == null || cell.IsFogged;
        }

    }
}
