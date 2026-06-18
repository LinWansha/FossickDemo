using Fossick.Core.Actions;
using Fossick.Core.Board;
using Fossick.Core.Config;
using Fossick.Core.Gameplay;
using Fossick.Core.Visual;
using Fossick.Preview.Controllers;
using Fossick.Runtime.Views;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Fossick.Preview.Views
{
    [RequireComponent(typeof(FossickPreviewController))]
    public sealed class FossickPreviewView : MonoBehaviour
    {
        private const float HeaderHeight = 68f;
        private const float ToolPanelWidth = 112f;
        private const float LogPanelWidth = 318f;
        private const float OuterPadding = 16f;
        private const float ColumnGap = 18f;
        private const float SmoothTileOverlap = 2f;

        private static readonly Color Background = new Color(0.07f, 0.09f, 0.1f);
        private static readonly Color Panel = new Color(0.13f, 0.15f, 0.17f);
        private static readonly Color PanelDark = new Color(0.08f, 0.11f, 0.1f);
        private static readonly Color TextColor = new Color(0.92f, 0.94f, 0.95f);
        private static readonly Color MutedTextColor = new Color(0.68f, 0.72f, 0.76f);
        private static readonly Color SelectedColor = new Color(0.25f, 0.5f, 0.78f);
        private static readonly Color PreviewColor = new Color(0.32f, 0.9f, 0.48f, 0.95f);

        private readonly Dictionary<string, Image> cellImages = new Dictionary<string, Image>();
        private readonly HashSet<string> previewTargetKeys = new HashSet<string>();
        private FossickPreviewController controller;
        private GameObject canvasObject;
        private RectTransform root;
        private Font font;
        [SerializeField] private FossickArtCatalog artCatalog;
        [SerializeField] private float boardScrollAnimationDuration = 0.22f;
        private FossickBoardView boardView;
        private Coroutine boardScrollRoutine;
        private bool isBoardAnimating;

        private void Awake()
        {
            controller = GetComponent<FossickPreviewController>();
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }

        private void Start()
        {
            Build();
        }

        private void OnDestroy()
        {
            if (canvasObject != null)
            {
                Destroy(canvasObject);
            }
        }

        private void Build()
        {
            EnsureEventSystem();
            ClearCanvas();

            canvasObject = new GameObject("Fossick Preview Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(transform, false);

            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 20;

            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            root = CreateRect("Root", canvasObject.transform);
            Stretch(root);
            AddImage(root.gameObject, Background).raycastTarget = false;

            if (controller == null || controller.Board == null)
            {
                DrawNotReady();
                return;
            }

            DrawHeader();
            DrawToolPanel();
            DrawBoardPanel();
            DrawLogPanel();
        }

        private void ClearCanvas()
        {
            cellImages.Clear();
            previewTargetKeys.Clear();
            if (canvasObject != null)
            {
                canvasObject.SetActive(false);
                Destroy(canvasObject);
            }
        }

        private void DrawNotReady()
        {
            var text = AddText(root, "Fossick 灰盒预览未就绪", 24, FontStyle.Bold, TextAnchor.MiddleCenter);
            Stretch(text.GetComponent<RectTransform>());
        }

        private void DrawHeader()
        {
            var header = CreatePanel("Header", root);
            SetTopLeft(header, 0f, 0f, 1920f, HeaderHeight);

            var title = AddText(header, "Fossick 灰盒预览", 24, FontStyle.Bold, TextAnchor.MiddleLeft);
            SetTopLeft(title.GetComponent<RectTransform>(), 18f, 0f, 250f, HeaderHeight);

            var progress = controller.Progress;
            AddHeaderStat(header, 292f, $"深度 {controller.Board.Depth}");
            AddHeaderStat(header, 430f, progress == null ? "矿石 0" : $"矿石 {progress.oreFound}");
            AddHeaderStat(header, 560f, progress == null ? "收藏 0" : $"收藏 {progress.collectionFound}");
            AddHeaderStat(header, 698f, progress == null ? "道具 0" : $"道具 {progress.toolUsed}");
            AddHeaderStat(header, 836f, controller.Rewards == null ? "积分 0" : $"积分 {controller.Rewards.score}");
            AddHeaderStat(header, 974f, controller.Rewards == null ? "金币 0" : $"金币 {controller.Rewards.coins}");

            AddButton(header, "保存", new Vector2(104f, 38f), () =>
            {
                controller.Save();
                Build();
            }, false, SelectedColor, new Vector2(1288f, 15f));
            AddButton(header, controller.HasSave ? "重载" : "未保存", new Vector2(104f, 38f), () =>
            {
                if (controller.HasSave)
                {
                    controller.ReloadSaved();
                    Build();
                }
            }, !controller.HasSave, controller.HasSave ? SelectedColor : new Color(0.24f, 0.26f, 0.3f), new Vector2(1404f, 15f));
            AddButton(header, "重置", new Vector2(104f, 38f), () =>
            {
                controller.ResetPreview();
                Build();
            }, false, new Color(0.34f, 0.25f, 0.24f), new Vector2(1520f, 15f));
        }

        private void AddHeaderStat(RectTransform parent, float x, string label)
        {
            var text = AddText(parent, label, 16, FontStyle.Bold, TextAnchor.MiddleLeft);
            SetTopLeft(text.GetComponent<RectTransform>(), x, 0f, 128f, HeaderHeight);
        }

        private void DrawToolPanel()
        {
            var panel = CreatePanel("Tool Panel", root);
            SetTopLeft(panel, OuterPadding, HeaderHeight + OuterPadding, ToolPanelWidth, 1080f - HeaderHeight - OuterPadding * 2f);

            var title = AddText(panel, "工具", 18, FontStyle.Bold, TextAnchor.MiddleCenter);
            SetTopLeft(title.GetComponent<RectTransform>(), 0f, 14f, ToolPanelWidth, 28f);

            DrawToolButton(panel, FossickToolType.Pickaxe, "矿镐", 62f);
            DrawToolButton(panel, FossickToolType.Dynamite, "雷管", 146f);
            DrawToolButton(panel, FossickToolType.Tnt, "炸药", 230f);
            DrawToolButton(panel, FossickToolType.Radar, "雷达", 314f);
        }

        private void DrawToolButton(RectTransform parent, FossickToolType toolType, string label, float y)
        {
            var selected = controller.SelectedTool == toolType;
            var count = controller.Inventory == null ? 0 : controller.Inventory.GetToolCount(toolType);
            var countLabel = controller.UnlimitedTools ? "∞" : count.ToString();
            AddButton(parent, label + "\n" + countLabel, new Vector2(80f, 60f), () =>
            {
                var previousTool = controller.SelectedTool;
                controller.SelectTool(toolType);
                if (toolType == FossickToolType.Radar)
                {
                    controller.UseTool(0, 0);
                    controller.SelectTool(previousTool);
                }

                Build();
            }, false, selected ? SelectedColor : new Color(0.21f, 0.24f, 0.28f), new Vector2(16f, y));
        }

        private void DrawBoardPanel()
        {
            var panelLeft = OuterPadding + ToolPanelWidth + ColumnGap;
            var panelRight = 1920f - OuterPadding - LogPanelWidth - ColumnGap;
            var panelTop = HeaderHeight + OuterPadding;
            var panelHeight = 1080f - HeaderHeight - OuterPadding * 2f;
            var panelWidth = panelRight - panelLeft;

            var panel = CreatePanel("Board Panel", root);
            SetTopLeft(panel, panelLeft, panelTop, panelWidth, panelHeight);

            var title = AddText(panel, "矿井", 20, FontStyle.Bold, TextAnchor.MiddleLeft);
            SetTopLeft(title.GetComponent<RectTransform>(), 22f, 16f, 240f, 28f);
            var sub = AddText(panel, $"可视窗口 {controller.Board.Spec.width} x {controller.Board.Spec.visibleHeight}，点击或触摸格子执行当前工具", 14, FontStyle.Normal, TextAnchor.MiddleLeft);
            sub.color = MutedTextColor;
            SetTopLeft(sub.GetComponent<RectTransform>(), 22f, 48f, 620f, 24f);

            DrawBoardGrid(panel, panelWidth, panelHeight);
        }

        private void DrawBoardGrid(RectTransform parent, float panelWidth, float panelHeight)
        {
            var board = controller.Board;
            var rows = board.GetVisibleRows();
            var labelWidth = 56f;
            var gridMaxWidth = panelWidth - 120f;
            var gridMaxHeight = panelHeight - 126f;
            var cellSize = Mathf.Floor(Mathf.Min((gridMaxWidth - labelWidth) / board.Spec.width, gridMaxHeight / board.Spec.visibleHeight));
            cellSize = Mathf.Clamp(cellSize, 48f, 144f);
            var gridWidth = labelWidth + cellSize * board.Spec.width;
            var gridHeight = cellSize * board.Spec.visibleHeight;
            var startX = Mathf.Max(24f, (panelWidth - gridWidth) * 0.5f);
            var startY = 94f + Mathf.Max(0f, (gridMaxHeight - gridHeight) * 0.5f);

            var frame = CreateRect("Board Frame", parent);
            SetTopLeft(frame, startX, startY, gridWidth, gridHeight);
            AddImage(frame.gameObject, PanelDark).raycastTarget = false;

            boardView = frame.gameObject.AddComponent<FossickBoardView>();
            boardView.LabelWidth = labelWidth;
            boardView.ShowDebugLabels = true;
            boardView.SetFont(font);
            boardView.SetArtCatalog(artCatalog);
            boardView.CellPointerEntered += ShowPreview;
            boardView.CellPointerExited += ClearPreview;
            boardView.CellPointerDown += ShowPreview;
            boardView.CellPointerUp += ClearPreview;
            boardView.CellClicked += (x, y) =>
            {
                if (isBoardAnimating)
                {
                    return;
                }

                var result = controller.UseTool(x, y);
                PlayResultOrRebuild(result);
            };
            boardView.Render(board, previewTargetKeys);
        }

        private void DrawRuntimeSmoothGrid(RectTransform parent, IReadOnlyList<FossickCellState[]> rows, int width, int height, float cellSize)
        {
            if (rows == null || rows.Count == 0 || width <= 0 || height <= 0)
            {
                return;
            }

            DrawRuntimeSmoothGridLayer(parent, rows, width, height, cellSize, FossickTerrainType.Dirt);
            DrawRuntimeSmoothGridLayer(parent, rows, width, height, cellSize, FossickTerrainType.Stone);
            DrawRuntimeSmoothGridLayer(parent, rows, width, height, cellSize, FossickTerrainType.Unbreakable);
        }

        private void DrawRuntimeSmoothGridLayer(RectTransform parent, IReadOnlyList<FossickCellState[]> rows, int width, int height, float cellSize, FossickTerrainType terrain)
        {
            if (!FossickArtLibrary.HasAutoTileSprites(terrain))
            {
                return;
            }

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

                    var rect = CreateRect($"{terrain} Smooth Corner {cornerX},{cornerY}-{assetIndex}", parent);
                    SetTopLeft(
                        rect,
                        (cornerX - 0.5f) * cellSize - SmoothTileOverlap * 0.5f,
                        (cornerY - 0.5f) * cellSize - SmoothTileOverlap * 0.5f,
                        cellSize + SmoothTileOverlap,
                        cellSize + SmoothTileOverlap);
                    var image = AddImage(rect.gameObject, Color.white);
                    image.raycastTarget = false;
                    image.sprite = sprite;
                    image.type = Image.Type.Simple;
                    image.preserveAspect = false;
                }
            }
        }

        private void DrawCell(RectTransform parent, FossickCellState cell, int x, int y, float left, float top, float size)
        {
            var rect = CreateRect($"Cell {x},{y}", parent);
            SetTopLeft(rect, left, top, size - 2f, size - 2f);
            var image = AddImage(rect.gameObject, GetCellOverlayColor(cell));
            image.raycastTarget = true;
            cellImages[GetCellKey(x, y)] = image;

            DrawRewardLayer(rect, cell, size);

            var label = AddText(rect, GetCellLabel(cell), Mathf.RoundToInt(Mathf.Clamp(size * 0.18f, 12f, 22f)), FontStyle.Bold, TextAnchor.MiddleCenter);
            Stretch(label.GetComponent<RectTransform>());

            var trigger = rect.gameObject.AddComponent<EventTrigger>();
            AddEvent(trigger, EventTriggerType.PointerEnter, _ => ShowPreview(x, y));
            AddEvent(trigger, EventTriggerType.PointerExit, _ => ClearPreview());
            AddEvent(trigger, EventTriggerType.PointerDown, _ => ShowPreview(x, y));
            AddEvent(trigger, EventTriggerType.PointerClick, _ =>
            {
                controller.UseTool(x, y);
                Build();
            });
            AddEvent(trigger, EventTriggerType.PointerUp, _ => ClearPreview());
        }

        private void DrawRewardLayer(RectTransform parent, FossickCellState cell, float size)
        {
            if (cell == null || !cell.IsContentVisible || !cell.HasRewardOverlay)
            {
                return;
            }

            var sprite = cell.HasTerrainAttachedReward
                ? FossickArtLibrary.GetTerrainAttachmentSprite(cell.reward, cell.terrain)
                : FossickArtLibrary.GetRewardSprite(cell.reward);
            if (sprite == null)
            {
                return;
            }

            var inset = size * 0.15f;
            var rect = CreateRect("Reward Sprite", parent);
            SetTopLeft(rect, inset, inset, size - inset * 2f, size - inset * 2f);
            var image = AddImage(rect.gameObject, Color.white);
            image.raycastTarget = false;
            image.sprite = sprite;
            image.preserveAspect = true;
        }

        private void DrawLogPanel()
        {
            var panel = CreatePanel("Log Panel", root);
            SetTopLeft(panel, 1920f - OuterPadding - LogPanelWidth, HeaderHeight + OuterPadding, LogPanelWidth, 1080f - HeaderHeight - OuterPadding * 2f);

            var title = AddText(panel, "动作日志", 20, FontStyle.Bold, TextAnchor.MiddleLeft);
            SetTopLeft(title.GetComponent<RectTransform>(), 18f, 16f, 220f, 30f);

            var hint = AddText(panel, "最近操作会显示在这里", 13, FontStyle.Normal, TextAnchor.MiddleLeft);
            hint.color = MutedTextColor;
            SetTopLeft(hint.GetComponent<RectTransform>(), 18f, 48f, 240f, 22f);

            var log = controller.ActionLog;
            if (log == null || log.Count == 0)
            {
                var empty = AddText(panel, "暂无操作", 15, FontStyle.Normal, TextAnchor.UpperLeft);
                empty.color = MutedTextColor;
                SetTopLeft(empty.GetComponent<RectTransform>(), 18f, 88f, LogPanelWidth - 36f, 32f);
                return;
            }

            for (var i = 0; i < Mathf.Min(10, log.Count); i++)
            {
                var item = AddText(panel, log[i], 14, FontStyle.Normal, TextAnchor.UpperLeft);
                SetTopLeft(item.GetComponent<RectTransform>(), 18f, 88f + i * 42f, LogPanelWidth - 36f, 34f);
            }
        }

        private void ShowPreview(int x, int y)
        {
            previewTargetKeys.Clear();
            var targets = controller.GetPreviewTargets(x, y);
            if (targets != null)
            {
                for (var i = 0; i < targets.Count; i++)
                {
                    previewTargetKeys.Add(GetCellKey(targets[i].x, targets[i].y));
                }
            }

            RefreshCellColors();
        }

        private void ClearPreview()
        {
            if (previewTargetKeys.Count == 0)
            {
                return;
            }

            previewTargetKeys.Clear();
            RefreshCellColors();
        }

        private void PlayResultOrRebuild(FossickGameplayActionResult result)
        {
            previewTargetKeys.Clear();
            if (result == null || result.action == null || !result.action.scrolled || result.action.scrollCount <= 0 || boardView == null)
            {
                Build();
                return;
            }

            if (boardScrollRoutine != null)
            {
                StopCoroutine(boardScrollRoutine);
            }

            boardScrollRoutine = StartCoroutine(PlayBoardScroll(result.action.scrollCount));
        }

        private IEnumerator PlayBoardScroll(int scrollRows)
        {
            isBoardAnimating = true;
            var duration = Mathf.Max(0.01f, boardScrollAnimationDuration);
            var elapsed = 0f;

            while (elapsed < duration && boardView != null && controller != null && controller.Board != null)
            {
                elapsed += Time.unscaledDeltaTime;
                var t = Mathf.Clamp01(elapsed / duration);
                var eased = 1f - Mathf.Pow(1f - t, 3f);
                boardView.SetVisualRowOffset(Mathf.Lerp(scrollRows, 0f, eased));
                boardView.Render(controller.Board, previewTargetKeys);
                yield return null;
            }

            if (boardView != null && controller != null && controller.Board != null)
            {
                boardView.SetVisualRowOffset(0f);
                boardView.Render(controller.Board, previewTargetKeys);
            }

            isBoardAnimating = false;
            boardScrollRoutine = null;
            Build();
        }

        private void RefreshCellColors()
        {
            if (boardView != null)
            {
                boardView.SetPreviewKeys(previewTargetKeys);
                boardView.RefreshPreviews();
                return;
            }

            var rows = controller.Board.GetVisibleRows();
            for (var y = 0; y < rows.Count; y++)
            {
                var row = rows[y];
                for (var x = 0; x < row.Length; x++)
                {
                    var key = GetCellKey(x, y);
                    if (!cellImages.TryGetValue(key, out var image) || image == null)
                    {
                        continue;
                    }

                    image.color = previewTargetKeys.Contains(key) ? PreviewColor : GetCellOverlayColor(row[x]);
                }
            }
        }

        private static string GetCellKey(int x, int y)
        {
            return x + ":" + y;
        }

        private static Color GetCellOverlayColor(FossickCellState cell)
        {
            if (cell == null)
            {
                return new Color(0.12f, 0.13f, 0.14f);
            }

            if (!cell.IsContentVisible)
            {
                return new Color(0.16f, 0.17f, 0.19f);
            }

            if (cell.terrain != FossickTerrainType.Empty && FossickArtLibrary.HasAutoTileSprites(cell.terrain))
            {
                return new Color(1f, 1f, 1f, 0f);
            }

            if (cell.terrain == FossickTerrainType.Empty)
            {
                return new Color(1f, 1f, 1f, 0f);
            }

            switch (cell.terrain)
            {
                case FossickTerrainType.Dirt:
                    return new Color(0.56f, 0.38f, 0.25f);
                case FossickTerrainType.Stone:
                    return new Color(0.48f, 0.53f, 0.58f);
                case FossickTerrainType.Unbreakable:
                    return new Color(0.07f, 0.07f, 0.09f);
                default:
                    return cell.HasCollectableElement ? new Color(0.42f, 0.34f, 0.14f) : new Color(0.1f, 0.18f, 0.18f);
            }
        }

        private static string GetCellLabel(FossickCellState cell)
        {
            if (cell == null)
            {
                return "?";
            }

            if (!cell.IsContentVisible)
            {
                return "?";
            }

            if (cell.terrain == FossickTerrainType.Dirt)
            {
                return "土";
            }

            if (cell.terrain == FossickTerrainType.Stone)
            {
                return "石" + cell.hp;
            }

            if (cell.terrain == FossickTerrainType.Unbreakable)
            {
                return "X";
            }

            return cell.HasCollectableElement ? "$" : string.Empty;
        }

        private RectTransform AddButton(Transform parent, string label, Vector2 size, Action onClick, bool disabled, Color color, Vector2 topLeft)
        {
            var rect = CreateRect(label, parent);
            SetTopLeft(rect, topLeft.x, topLeft.y, size.x, size.y);
            var image = AddImage(rect.gameObject, disabled ? new Color(0.18f, 0.19f, 0.21f) : color);
            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.interactable = !disabled;
            button.onClick.AddListener(() => onClick?.Invoke());

            var text = AddText(rect, label, 15, FontStyle.Bold, TextAnchor.MiddleCenter);
            text.color = disabled ? MutedTextColor : TextColor;
            Stretch(text.GetComponent<RectTransform>());
            return rect;
        }

        private RectTransform CreatePanel(string name, Transform parent)
        {
            var rect = CreateRect(name, parent);
            AddImage(rect.gameObject, Panel);
            return rect;
        }

        private RectTransform CreateRect(string name, Transform parent)
        {
            var obj = new GameObject(name, typeof(RectTransform));
            obj.transform.SetParent(parent, false);
            return obj.GetComponent<RectTransform>();
        }

        private Image AddImage(GameObject target, Color color)
        {
            var image = target.AddComponent<Image>();
            image.color = color;
            return image;
        }

        private Text AddText(Transform parent, string text, int size, FontStyle style, TextAnchor alignment)
        {
            var rect = CreateRect("Text", parent);
            var label = rect.gameObject.AddComponent<Text>();
            label.text = text;
            label.font = font;
            label.fontSize = size;
            label.fontStyle = style;
            label.color = TextColor;
            label.alignment = alignment;
            label.raycastTarget = false;
            return label;
        }

        private static void AddEvent(EventTrigger trigger, EventTriggerType eventType, Action<BaseEventData> callback)
        {
            var entry = new EventTrigger.Entry
            {
                eventID = eventType
            };
            entry.callback.AddListener(data => callback(data));
            trigger.triggers.Add(entry);
        }

        private static void SetTopLeft(RectTransform rect, float left, float top, float width, float height)
        {
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(left, -top);
            rect.sizeDelta = new Vector2(width, height);
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static void EnsureEventSystem()
        {
            if (FindObjectOfType<EventSystem>() != null)
            {
                return;
            }

            var eventSystem = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            DontDestroyOnLoad(eventSystem);
        }
    }
}
