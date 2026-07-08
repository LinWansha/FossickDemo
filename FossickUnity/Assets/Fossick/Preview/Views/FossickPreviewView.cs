using Fossick.Core.Application;
using Fossick.Core.Application.Results;
using Fossick.Core.Definition.Config;
using Fossick.Core.Mine;
using Fossick.Core.Visual;
using Fossick.Core.Visual.Tiling;
using Fossick.Preview.Controllers;
using Fossick.Runtime.Views;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
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
        private const string MapStudioSceneName = "FossickMapStudio";

        private static readonly Color Background = new Color(0.07f, 0.09f, 0.1f);
        private static readonly Color Panel = new Color(0.13f, 0.15f, 0.17f);
        private static readonly Color PanelDark = new Color(0.08f, 0.11f, 0.1f);
        private static readonly Color TextColor = new Color(0.92f, 0.94f, 0.95f);
        private static readonly Color MutedTextColor = new Color(0.68f, 0.72f, 0.76f);
        private static readonly Color SelectedColor = new Color(0.25f, 0.5f, 0.78f);
        private static readonly Color PreviewColor = new Color(0.32f, 0.9f, 0.48f, 0.95f);

        private readonly Dictionary<string, Image> cellImages = new Dictionary<string, Image>();
        private readonly HashSet<string> previewTargetKeys = new HashSet<string>();
        private FossickPreviewController preview;
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
            preview = GetComponent<FossickPreviewController>();
            if (preview == null)
            {
                preview = gameObject.AddComponent<FossickPreviewController>();
            }

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

            if (preview == null || preview.Mine == null)
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

            var progress = preview.Progress;
            AddHeaderStat(header, 292f, $"深度 {preview.Mine.Depth}");
            AddHeaderStat(header, 430f, progress == null ? "矿石 0" : $"矿石 {progress.oreFound}");
            AddHeaderStat(header, 560f, progress == null ? "收藏 0" : $"收藏 {progress.collectionFound}");
            AddHeaderStat(header, 698f, progress == null ? "道具 0" : $"道具 {progress.toolUsed}");
            AddHeaderStat(header, 836f, preview.Rewards == null ? "积分 0" : $"积分 {preview.Rewards.score}");
            AddHeaderStat(header, 974f, preview.Rewards == null ? "金币 0" : $"金币 {preview.Rewards.coins}");

            AddButton(header, "返回编辑器", new Vector2(128f, 38f), () =>
            {
                SceneManager.LoadScene(MapStudioSceneName);
            }, false, new Color(0.24f, 0.47f, 0.72f), new Vector2(1148f, 15f));
            AddButton(header, "重置", new Vector2(104f, 38f), () =>
            {
                preview.ResetPreview();
                Build();
            }, false, new Color(0.34f, 0.25f, 0.24f), new Vector2(1288f, 15f));
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
            var selected = preview.SelectedTool == toolType;
            var count = preview.Inventory == null ? 0 : preview.Inventory.GetToolCount(toolType);
            var countLabel = preview.UnlimitedTools ? "∞" : count.ToString();
            AddButton(parent, label + "\n" + countLabel, new Vector2(80f, 60f), () =>
            {
                var previousTool = preview.SelectedTool;
                preview.SelectTool(toolType);
                if (toolType == FossickToolType.Radar)
                {
                    preview.UseTool(0, 0);
                    preview.SelectTool(previousTool);
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
            var sub = AddText(panel, $"可视窗口 {preview.Mine.Spec.width} x {preview.Mine.Spec.visibleHeight}，点击或触摸格子执行当前工具", 14, FontStyle.Normal, TextAnchor.MiddleLeft);
            sub.color = MutedTextColor;
            SetTopLeft(sub.GetComponent<RectTransform>(), 22f, 48f, 620f, 24f);

            DrawBoardGrid(panel, panelWidth, panelHeight);
        }

        private void DrawBoardGrid(RectTransform parent, float panelWidth, float panelHeight)
        {
            var mine = preview.Mine;
            var labelWidth = 56f;
            var gridMaxWidth = panelWidth - 120f;
            var gridMaxHeight = panelHeight - 126f;
            var cellSize = Mathf.Floor(Mathf.Min((gridMaxWidth - labelWidth) / mine.Spec.width, gridMaxHeight / mine.Spec.visibleHeight));
            cellSize = Mathf.Clamp(cellSize, 48f, 144f);
            var gridWidth = labelWidth + cellSize * mine.Spec.width;
            var gridHeight = cellSize * mine.Spec.visibleHeight;
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

                var result = preview.UseTool(x, y);
                PlayResultOrRebuild(result);
            };
            boardView.Render(mine, previewTargetKeys);
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

            var log = preview.ActionLog;
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
            var targets = preview.GetPreviewTargets(x, y);
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

        private void PlayResultOrRebuild(FossickActionResult result)
        {
            previewTargetKeys.Clear();
            if (result == null || !result.scrolled || result.scrollCount <= 0 || boardView == null)
            {
                Build();
                return;
            }

            if (boardScrollRoutine != null)
            {
                StopCoroutine(boardScrollRoutine);
            }

            boardScrollRoutine = StartCoroutine(PlayBoardScroll(result.scrollCount));
        }

        private IEnumerator PlayBoardScroll(int scrollRows)
        {
            isBoardAnimating = true;
            var previousRowsAbove = boardView == null ? 0 : boardView.RenderRowsAbove;
            var previousRowsBelow = boardView == null ? 0 : boardView.RenderRowsBelow;
            if (boardView != null)
            {
                boardView.RenderRowsAbove = Mathf.Max(previousRowsAbove, scrollRows + 1);
                boardView.RenderRowsBelow = Mathf.Max(previousRowsBelow, scrollRows + 1);
            }

            var duration = Mathf.Max(0.01f, boardScrollAnimationDuration);
            var elapsed = 0f;

            while (elapsed < duration && boardView != null && preview != null && preview.Mine != null)
            {
                elapsed += Time.unscaledDeltaTime;
                var t = Mathf.Clamp01(elapsed / duration);
                var eased = 1f - Mathf.Pow(1f - t, 3f);
                boardView.SetVisualRowOffset(Mathf.Lerp(scrollRows, 0f, eased));
                boardView.Render(preview.Mine, previewTargetKeys);
                yield return null;
            }

            if (boardView != null && preview != null && preview.Mine != null)
            {
                boardView.RenderRowsAbove = previousRowsAbove;
                boardView.RenderRowsBelow = previousRowsBelow;
                boardView.SetVisualRowOffset(0f);
                boardView.Render(preview.Mine, previewTargetKeys);
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

        }

        private static string GetCellKey(int x, int y)
        {
            return x + ":" + y;
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
