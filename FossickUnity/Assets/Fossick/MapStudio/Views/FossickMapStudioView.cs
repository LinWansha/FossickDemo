using System;
using System.Collections.Generic;
using System.IO;
using Fossick.Core.Definition.Config;
using Fossick.Core.Generation;
using Fossick.Core.Data;
using Fossick.Core.Definition.Serialization;
using Fossick.Core.Mine.Objects;
using Fossick.Core.Visual;
using Fossick.Core.Visual.Tiling;
using Fossick.MapStudio.Controllers;
using Fossick.MapStudio.Definition;
using Fossick.MapStudio.ImportExport;
using Fossick.MapStudio.Validation;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

namespace Fossick.MapStudio.Views
{
    [RequireComponent(typeof(FossickMapStudioController))]
    public sealed partial class FossickMapStudioView : MonoBehaviour
    {
        private const float HeaderHeight = 56f;
        private const float LeftWidth = 270f;
        private const float CenterWidth = 1442f;
        private const float ColumnGap = 24f;
        private const float CellSize = 64f;
        private const float MineCellSize = 96f;
        private const float BrushTileWidth = 76f;
        private const float BrushTileHeight = 78f;
        private const float SmoothTileOverlap = 2f;
        private const float GridLabelWidth = 50f;
        private const float LeftButtonWidth = 222f;
        private const string PreviewScenePath = "Assets/Fossick/Preview/Scenes/FossickPreview.unity";
        private const string PreviewSceneName = "FossickPreview";

        private FossickMapStudioController controller;
        private FossickBrushPaletteView brushPaletteView;
        private GameObject canvasObject;
        private RectTransform root;
        private Font font;
        private int selectedFragmentIndex;
        private int selectedMineSequenceIndex = -1;
        private MapStudioEditMode editMode = MapStudioEditMode.MineInstance;
        private int minePreviewRows = 72;
        private float mineScrollNormalizedPosition = 1f;
        private FossickTerrainType selectedTerrain = FossickTerrainType.Dirt;
        private string exportStatus;
        private string currentFeedbackMessage;
        private MapStudioFeedbackKind currentFeedbackKind = MapStudioFeedbackKind.Info;
        private string editNotice
        {
            get => currentFeedbackMessage;
            set => SetFeedback(value, InferFeedbackKind(value));
        }
        private int pendingPaintFragmentId = -1;
        private int pendingPaintX = -1;
        private int pendingPaintY = -1;
        private bool isDragPainting;
        private FossickBrushMode selectedBrushMode = FossickBrushMode.Terrain;
        private FossickElementType selectedRewardType = FossickElementType.Ore;
        private string selectedRewardId = "ore_copper";
        private int selectedRewardAmountOverride;
        private string selectedRewardBackgroundId = string.Empty;
        private int selectedRewardBackgroundWidth;
        private int selectedRewardBackgroundHeight;
        private string selectedDecorationId = string.Empty;
        private FossickFogType selectedFog = FossickFogType.Covered;
        private bool showFogInEditor = true;
        private bool templateLibraryOpen;
        private TemplateLibraryFilter templateLibraryFilter = TemplateLibraryFilter.All;
        private TemplatePresetType selectedTemplatePreset = TemplatePresetType.Blank;
        private bool generationRulesOpen;
        private FossickFragmentConfig templateEditDraft;
        private int templateEditSourceIndex = -1;
        private bool templateEditDirty;
        private FossickMapConfig mineInstanceSourceConfig;
        private int mineInstanceSeed;
        private bool mineInstanceGenerated;
        private bool generationRulesDirty;
        private bool generationRulesEditDirty;
        private bool generationRulesDirtySnapshot;
        private FossickGenerationConfig generationRulesSnapshot;
        private readonly List<string> templateUndoStack = new List<string>();
        private readonly List<string> templateRedoStack = new List<string>();
        private readonly HashSet<string> selectedPaintCells = new HashSet<string>();
        private readonly Dictionary<string, RectTransform> selectedPaintCellRects = new Dictionary<string, RectTransform>();
        private readonly Dictionary<string, GameObject> selectedPaintCellHighlights = new Dictionary<string, GameObject>();

        private static readonly Color Background = new Color(0.09f, 0.1f, 0.11f);
        private static readonly Color Panel = new Color(0.14f, 0.15f, 0.17f);
        private static readonly Color TextColor = new Color(0.92f, 0.93f, 0.94f);
        private static readonly Color ButtonDefault = new Color(0.22f, 0.24f, 0.27f);
        private static readonly Color ButtonPrimary = new Color(0.24f, 0.47f, 0.72f);
        private static readonly Color ButtonSelected = ButtonPrimary;
        private static readonly Color ButtonDanger = new Color(0.45f, 0.27f, 0.22f);
        private static readonly Color ButtonMuted = new Color(0.17f, 0.2f, 0.22f);
        private const string TreasureRoomSmallId = "treasure_room_3x2";
        private const string TreasureRoomMediumId = "treasure_room_5x2";
        private const string TreasureRoomLargeId = "treasure_room_7x2";

        private enum MapStudioEditMode
        {
            Template,
            MineInstance
        }

        private enum TemplateLibraryFilter
        {
            All,
            Tutorial,
            Regular,
            Reward
        }

        private enum TemplatePresetType
        {
            Blank,
            FilledRegular,
            RewardRoom
        }

        private enum MapStudioFeedbackKind
        {
            Info,
            Success,
            Warning,
            Error
        }

        private enum ButtonTone
        {
            Default,
            Primary,
            Danger,
            Muted
        }

        private struct RewardBackgroundRegion
        {
            public string id;
            public int startX;
            public int endX;
            public int startY;
            public int endY;
        }

        private void Awake()
        {
            controller = GetComponent<FossickMapStudioController>();
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            brushPaletteView = new FossickBrushPaletteView(font);
        }

        private void Start()
        {
            if (controller != null && controller.CurrentConfig != null && controller.CurrentConfig.visual == null)
            {
                controller.CurrentConfig.visual = new FossickVisualConfig();
            }

            GenerateMineInstance(false, "已按当前模板、半随机规则和种子生成默认地图预览。");
            Build();
        }

        private void Update()
        {
            if (isDragPainting && !Input.GetMouseButton(0))
            {
                FinishDragPainting();
            }

        }

        private void Build()
        {
            EnsureEventSystem();

            if (canvasObject != null)
            {
                canvasObject.SetActive(false);
                Destroy(canvasObject);
            }

            selectedPaintCellRects.Clear();
            selectedPaintCellHighlights.Clear();

            canvasObject = new GameObject("Fossick MapStudio Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(transform, false);

            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;

            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            root = CreateRect("Root", canvasObject.transform);
            Stretch(root);
            AddImage(root.gameObject, Background);

            DrawHeader();
            DrawFragmentList();
            DrawMineEditor();
            DrawMinePreviewPanel();
            if (operationDialogOpen)
            {
                DrawOperationDialog();
            }

        }

        private void DrawHeader()
        {
            var header = CreatePanel("Header", root, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -HeaderHeight), Vector2.zero);
            AddHorizontalLayout(header.gameObject, 12, TextAnchor.MiddleLeft);

            AddText(header, "Fossick 矿井编辑器", 22, FontStyle.Bold, new Vector2(260f, 40f));

            var config = controller.CurrentConfig;
            AddText(header, $"棋盘 {config.boardWidth} x {config.visibleHeight}", 16, FontStyle.Normal, new Vector2(140f, 40f));
            AddText(header, $"碎片 {config.fragments.Count}", 16, FontStyle.Normal, new Vector2(120f, 40f));
            AddText(header, $"种子 {controller.Seed}", 16, FontStyle.Normal, new Vector2(120f, 40f));

            AddSpacer(header, 1f);
            AddActionButton(header, "试玩", new Vector2(100f, 36f), PlayPreviewScene, ButtonTone.Primary);
            AddActionButton(header, "校验", new Vector2(120f, 36f), () =>
            {
                controller.Validate();
                Build();
            });
            AddActionButton(header, "导出 JSON", new Vector2(140f, 36f), ExportJson);
            AddActionButton(header, "打开数据目录", new Vector2(150f, 36f), OpenDataFolder);
        }

        private void DrawFragmentList()
        {
            var panel = CreatePanel("Fragments Panel", root, new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(12f, 12f), new Vector2(LeftWidth, -HeaderHeight - 24f));
            var mask = panel.gameObject.AddComponent<Mask>();
            mask.showMaskGraphic = true;

            const float x = 14f;
            var y = 14f;

            var title = AddText(panel, "菜单栏", 20, FontStyle.Bold, new Vector2(LeftButtonWidth, 30f));
            SetTopLeft(title.GetComponent<RectTransform>(), x, y, LeftButtonWidth, 30f);
            y += 34f;

            var description = AddText(panel, "模板、生成规则和预览分开管理。", 12, FontStyle.Normal, new Vector2(LeftButtonWidth, 38f));
            SetTopLeft(description.GetComponent<RectTransform>(), x, y, LeftButtonWidth, 38f);
            y += 58f;

            var stateTitle = AddText(panel, "当前状态", 16, FontStyle.Bold, new Vector2(LeftButtonWidth, 22f));
            SetTopLeft(stateTitle.GetComponent<RectTransform>(), x, y, LeftButtonWidth, 22f);
            y += 28f;

            var stateCard = CreateRect("Mode Summary Card", panel);
            SetTopLeft(stateCard, x, y, LeftButtonWidth, 58f);
            AddImage(stateCard.gameObject, ButtonMuted);

            var stateStripe = CreateRect("Mode Summary Stripe", stateCard);
            SetTopLeft(stateStripe, 0f, 0f, 4f, 58f);
            AddImage(stateStripe.gameObject, GetCurrentModeColor()).raycastTarget = false;

            var stateCardTitle = AddText(stateCard, GetCurrentModeTitle(), 14, FontStyle.Bold, new Vector2(LeftButtonWidth - 16f, 20f));
            SetTopLeft(stateCardTitle.GetComponent<RectTransform>(), 12f, 6f, LeftButtonWidth - 18f, 20f);

            var stateDetail = AddText(stateCard, GetCurrentModeDescription(), 12, FontStyle.Normal, new Vector2(LeftButtonWidth - 16f, 26f));
            SetTopLeft(stateDetail.GetComponent<RectTransform>(), 12f, 29f, LeftButtonWidth - 18f, 26f);
            y += 76f;

            var templateTitle = AddText(panel, "模板管理", 17, FontStyle.Bold, new Vector2(LeftButtonWidth, 24f));
            SetTopLeft(templateTitle.GetComponent<RectTransform>(), x, y, LeftButtonWidth, 24f);
            y += 28f;

            var fragments = controller.CurrentConfig.fragments;
            var templateCounts = AddText(panel, FormatTemplateCounts(fragments), 12, FontStyle.Normal, new Vector2(LeftButtonWidth, 40f));
            SetTopLeft(templateCounts.GetComponent<RectTransform>(), x, y, LeftButtonWidth, 40f);
            y += 46f;

            var selected = GetSelectedFragment();
            var selectedTemplate = AddText(
                panel,
                selected == null ? "当前模板：未选择" : $"当前模板：{selected.id}  {FormatFragmentType(selected.type)}",
                12,
                FontStyle.Bold,
                new Vector2(LeftButtonWidth, 22f));
            SetTopLeft(selectedTemplate.GetComponent<RectTransform>(), x, y, LeftButtonWidth, 22f);
            y += 28f;

            var templateButton = AddActionButton(panel, "打开模板库", new Vector2(LeftButtonWidth, 32f), OpenTemplateLibrary, templateLibraryOpen ? ButtonTone.Primary : ButtonTone.Default);
            SetTopLeft(templateButton, x, y, LeftButtonWidth, 32f);
            y += 54f;

            var generationTitle = AddText(panel, "生成配置", 17, FontStyle.Bold, new Vector2(LeftButtonWidth, 24f));
            SetTopLeft(generationTitle.GetComponent<RectTransform>(), x, y, LeftButtonWidth, 24f);
            y += 30f;

            var generationButton = AddActionButton(panel, "生成规则", new Vector2(LeftButtonWidth, 32f), OpenGenerationRules, generationRulesOpen ? ButtonTone.Primary : ButtonTone.Default);
            SetTopLeft(generationButton, x, y, LeftButtonWidth, 32f);
            y += 40f;

            var generationStatus = AddText(
                panel,
                generationRulesDirty ? "模板或规则已修改，需要重新生成地图预览。" : "模板和规则与当前地图预览一致。",
                12,
                generationRulesDirty ? FontStyle.Bold : FontStyle.Normal,
                new Vector2(LeftButtonWidth, 36f));
            SetTopLeft(generationStatus.GetComponent<RectTransform>(), x, y, LeftButtonWidth, 36f);
            y += 62f;

            var previewCard = CreateRect("Generation Preview Card", panel);
            SetTopLeft(previewCard, x, y, LeftButtonWidth, 226f);
            AddImage(previewCard.gameObject, new Color(0.1f, 0.13f, 0.15f));

            var mineTitle = AddText(previewCard, "地图预览", 17, FontStyle.Bold, new Vector2(LeftButtonWidth - 24f, 24f));
            SetTopLeft(mineTitle.GetComponent<RectTransform>(), 12f, 12f, LeftButtonWidth - 24f, 24f);

            var mineDesc = AddText(previewCard, "用当前模板和规则生成矿井。", 12, FontStyle.Normal, new Vector2(LeftButtonWidth - 24f, 22f));
            SetTopLeft(mineDesc.GetComponent<RectTransform>(), 12f, 40f, LeftButtonWidth - 24f, 22f);

            var seedTitle = AddText(previewCard, "种子", 12, FontStyle.Bold, new Vector2(44f, 22f));
            SetTopLeft(seedTitle.GetComponent<RectTransform>(), 12f, 76f, 44f, 22f);

            var seedValue = AddText(previewCard, controller.Seed.ToString(), 14, FontStyle.Bold, new Vector2(82f, 22f));
            SetTopLeft(seedValue.GetComponent<RectTransform>(), 56f, 76f, 82f, 22f);

            var seedButton = AddActionButton(previewCard, "换一个", new Vector2(70f, 30f), () =>
            {
                controller.RandomizeSeed();
                generationRulesDirty = true;
                editNotice = mineInstanceGenerated
                    ? "已换一个种子。点击“更新预览”后会刷新当前地图预览。"
                    : "已换一个种子。点击“生成预览”后会生成对应矿井。";
                Build();
            });
            SetTopLeft(seedButton, 12f, 106f, 70f, 30f);

            var generateButton = AddActionButton(previewCard, mineInstanceGenerated ? "更新预览" : "生成预览", new Vector2(126f, 30f), () =>
            {
                GenerateMineInstance(true, "已按当前模板、半随机规则和种子生成地图预览；矿井会按需无限向下延展。");
                Build();
            }, ButtonTone.Primary);
            SetTopLeft(generateButton, 88f, 106f, 126f, 30f);

            var previewStatus = AddText(previewCard, FormatMineInstanceSummary(), 12, FontStyle.Normal, new Vector2(LeftButtonWidth - 24f, 46f));
            SetTopLeft(previewStatus.GetComponent<RectTransform>(), 12f, 152f, LeftButtonWidth - 24f, 46f);

            if (editMode == MapStudioEditMode.MineInstance && !templateLibraryOpen && !generationRulesOpen)
            {
                var currentPreview = AddText(previewCard, "正在查看地图预览", 12, FontStyle.Bold, new Vector2(LeftButtonWidth - 24f, 22f));
                SetTopLeft(currentPreview.GetComponent<RectTransform>(), 12f, 196f, LeftButtonWidth - 24f, 22f);
                return;
            }

            var previewButton = AddActionButton(
                previewCard,
                mineInstanceGenerated ? "查看地图预览" : "暂无预览可查看",
                new Vector2(LeftButtonWidth - 24f, 30f),
                mineInstanceGenerated ? OpenGeneratedPreviewFromMenu : null,
                mineInstanceGenerated ? ButtonTone.Default : ButtonTone.Muted);
            SetTopLeft(previewButton, 12f, 190f, LeftButtonWidth - 24f, 30f);
        }

        private void DrawPalette(RectTransform parent)
        {
            AddText(parent, "分层画笔", 16, FontStyle.Bold, new Vector2(620f, 24f));
            var row = CreateRow(parent, "Brush Row", 900f, BrushTileHeight + 8f);
            DrawBrushPalette(row);
        }

        private void DrawGrid(RectTransform parent, FossickFragmentConfig fragment)
        {
            var gridRoot = CreateRect("Grid", parent);
            gridRoot.sizeDelta = new Vector2(GridLabelWidth + fragment.width * CellSize, fragment.height * CellSize);

            var terrainRoot = CreateRect("Terrain Visuals", gridRoot);
            SetTopLeft(terrainRoot, GridLabelWidth, 0f, fragment.width * CellSize, fragment.height * CellSize);
            AddImage(terrainRoot.gameObject, GetTerrainColor(FossickTerrainType.Empty)).raycastTarget = false;
            terrainRoot.gameObject.AddComponent<RectMask2D>();

            var clickRoot = CreateRect("Grid Clicks", gridRoot);
            SetTopLeft(clickRoot, GridLabelWidth, 0f, fragment.width * CellSize, fragment.height * CellSize);

            var labelRoot = CreateRect("Grid Labels", gridRoot);
            SetTopLeft(labelRoot, 0f, 0f, GridLabelWidth, fragment.height * CellSize);

            var rows = BuildConfigRows(fragment);
            DrawTerrainSmoothGrid(terrainRoot, rows, fragment.width, fragment.height, CellSize);
            DrawSingleTerrainSprites(terrainRoot, rows, fragment.width, fragment.height, CellSize);

            for (var y = 0; y < fragment.height; y++)
            {
                var rowLabelRect = CreateRect($"Row Label {y}", labelRoot);
                SetTopLeft(rowLabelRect, 0f, y * CellSize, GridLabelWidth - 6f, CellSize);
                var rowLabel = rowLabelRect.gameObject.AddComponent<Text>();
                rowLabel.text = y.ToString("00");
                rowLabel.font = font;
                rowLabel.fontSize = 13;
                rowLabel.color = TextColor;
                rowLabel.alignment = TextAnchor.MiddleLeft;

                for (var x = 0; x < fragment.width; x++)
                {
                    var cell = FindOrCreateCell(fragment, x, y);
                    AddCellClickArea(clickRoot, fragment, cell);
                }
            }
        }

        private void DrawMineEditor()
        {
            if (templateLibraryOpen)
            {
                DrawTemplateLibraryPanel();
                return;
            }

            if (generationRulesOpen)
            {
                DrawGenerationRulesPanel();
                return;
            }

            if (editMode == MapStudioEditMode.Template)
            {
                DrawTemplateEditorPanel();
                return;
            }

            DrawGeneratedMineEditorPanel();
        }

        private void DrawTemplateEditorPanel()
        {
            var panelLeft = LeftWidth + 24f;
            var panelRight = CenterWidth;
            var panelWidth = panelRight - panelLeft;
            var panel = CreatePanel("Template Editor Panel", root, new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(panelLeft, 12f), new Vector2(panelRight, -HeaderHeight - 24f));

            var fragment = GetTemplateEditFragment();
            const float padding = 16f;
            var editorWidth = panelWidth - padding * 2f;

            var title = AddText(panel, "模板编辑", 20, FontStyle.Bold, new Vector2(panelWidth - 32f, 30f));
            SetTopLeft(title.GetComponent<RectTransform>(), 16f, 14f, panelWidth - 32f, 30f);

            var summary = AddText(panel, fragment == null
                ? "当前模板 未选择"
                : $"当前模板 {fragment.id}   {FormatTemplatePool(fragment)}   画笔模式 {FormatBrushMode(selectedBrushMode)}   画笔 {FormatCurrentBrush()}   {(templateEditDirty ? "有未保存修改" : "已保存")}",
                14, FontStyle.Normal, new Vector2(editorWidth, 26f));
            SetTopLeft(summary.GetComponent<RectTransform>(), padding, 54f, editorWidth, 26f);

            if (!string.IsNullOrEmpty(editNotice))
            {
                DrawFeedbackBanner(panel, padding, 82f, editorWidth);
            }

            if (fragment == null)
            {
                return;
            }

            DrawTemplateCommandBar(panel, padding, 120f, editorWidth);

            var brushTitle = AddText(panel, "分层画笔", 16, FontStyle.Bold, new Vector2(240f, 24f));
            SetTopLeft(brushTitle.GetComponent<RectTransform>(), padding, 176f, 240f, 24f);
            var layerRow = CreateRow(panel, "Template Layer Row", Mathf.Min(790f, editorWidth), 38f);
            SetTopLeft(layerRow, padding, 206f, Mathf.Min(790f, editorWidth), 38f);
            DrawBrushModeTabs(layerRow);

            var palette = CreateRow(panel, "Template Brush Row", editorWidth, BrushTileHeight + 8f);
            SetTopLeft(palette, padding, 256f, editorWidth, BrushTileHeight + 8f);
            DrawBrushPalette(palette);

            DrawTemplateGridEditor(panel, fragment, padding, editorWidth, 376f);
        }

        private void DrawTemplateCommandBar(RectTransform parent, float x, float y, float width)
        {
            var bar = CreateRect("Template Command Bar", parent);
            SetTopLeft(bar, x, y, width, 42f);
            AddImage(bar.gameObject, new Color(0.11f, 0.13f, 0.15f));

            AddTextAt(bar, "模板操作", 13, FontStyle.Bold, 12f, 0f, 72f, 42f);

            var row = CreateRow(bar, "Template Command Row", Mathf.Min(width - 96f, 620f), 34f);
            SetTopLeft(row, 92f, 4f, Mathf.Min(width - 96f, 620f), 34f);
            AddActionButton(row, "撤销", new Vector2(76f, 30f), templateUndoStack.Count > 0 ? UndoTemplateEdit : null, templateUndoStack.Count > 0 ? ButtonTone.Default : ButtonTone.Muted);
            AddActionButton(row, "重做", new Vector2(76f, 30f), templateRedoStack.Count > 0 ? RedoTemplateEdit : null, templateRedoStack.Count > 0 ? ButtonTone.Default : ButtonTone.Muted);
            AddActionButton(row, "保存模板", new Vector2(112f, 30f), SaveTemplateEdit, ButtonTone.Primary);
            AddActionButton(row, "放弃修改", new Vector2(112f, 30f), templateEditDirty ? DiscardTemplateEdit : null, templateEditDirty ? ButtonTone.Danger : ButtonTone.Muted);
            AddActionButton(row, "返回地图", new Vector2(112f, 30f), ReturnFromTemplateEdit);
        }

        private void SetTemplateDraftType(FossickFragmentType type)
        {
            var fragment = GetTemplateEditFragment();
            if (fragment == null || fragment.type == type)
            {
                return;
            }

            RecordTemplateUndoSnapshot();
            fragment.type = type;
            NormalizeFragmentDifficulty(fragment);
            MarkTemplateDraftChanged();
            ClearPendingPaint();
            editNotice = $"已把模板 {fragment.id} 设为{FormatFragmentType(fragment.type)}，保存后生效。";
            Build();
        }

        private void SetTemplateDraftDifficulty(int difficulty)
        {
            var fragment = GetTemplateEditFragment();
            if (fragment == null || fragment.type != FossickFragmentType.Regular || fragment.difficulty == difficulty)
            {
                return;
            }

            RecordTemplateUndoSnapshot();
            fragment.difficulty = Mathf.Clamp(difficulty, 1, 3);
            MarkTemplateDraftChanged();
            ClearPendingPaint();
            editNotice = $"已把模板 {fragment.id} 难度设为 {fragment.difficulty}，保存后生效。";
            Build();
        }

        private void ResizeTemplateDraft(int height)
        {
            var fragment = GetTemplateEditFragment();
            if (fragment == null || fragment.height == height)
            {
                return;
            }

            RecordTemplateUndoSnapshot();
            ResizeFragment(fragment, height);
            MarkTemplateDraftChanged();
            ClearPendingPaint();
            editNotice = $"已调整模板 {fragment.id} 高度为 {fragment.height}，保存后生效。";
            Build();
        }

        private void DrawGeneratedMineEditorPanel()
        {
            var panelLeft = LeftWidth + 24f;
            var panelRight = CenterWidth;
            var panelWidth = panelRight - panelLeft;
            var panel = CreatePanel("Mine Editor Panel", root, new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(panelLeft, 12f), new Vector2(panelRight, -HeaderHeight - 24f));

            var mine = BuildPreviewMine();

            var title = AddText(panel, "地图预览", 20, FontStyle.Bold, new Vector2(panelWidth - 32f, 30f));
            SetTopLeft(title.GetComponent<RectTransform>(), 16f, 14f, panelWidth - 32f, 30f);
            DrawModeBadge(panel, panelWidth, 14f);

            var summaryText = "只读预览：由模板库和生成规则实时拼接矿井；点击格子可查看来源碎片，模板内容请回到模板库编辑。";
            var summary = AddText(panel, summaryText, 14, FontStyle.Normal, new Vector2(panelWidth - 32f, 26f));
            SetTopLeft(summary.GetComponent<RectTransform>(), 16f, 54f, panelWidth - 32f, 26f);
            if (!string.IsNullOrEmpty(editNotice))
            {
                DrawFeedbackBanner(panel, 16f, 82f, panelWidth - 32f);
            }

            if (!mineInstanceGenerated || mine.rows.Count == 0)
            {
                var emptyText = AddText(panel, "当前没有预览矿井。请点击顶部或左侧的“生成地图预览”。", 16, FontStyle.Bold, new Vector2(panelWidth - 32f, 32f));
                SetTopLeft(emptyText.GetComponent<RectTransform>(), 16f, 130f, panelWidth - 32f, 32f);
                var generateButton = AddButton(panel, "生成地图预览", new Vector2(180f, 40f), () =>
                {
                    GenerateMineInstance(true, "已按当前模板、半随机规则和种子生成地图预览；矿井会按需无限向下延展。");
                    Build();
                });
                SetTopLeft(generateButton, 16f, 176f, 180f, 40f);
                return;
            }

            var previewHint = AddText(panel, "预览不会写入模板库；需要调整内容时，请编辑对应模板或修改生成规则后重新生成。", 13, FontStyle.Normal, new Vector2(panelWidth - 32f, 24f));
            SetTopLeft(previewHint.GetComponent<RectTransform>(), 16f, 118f, panelWidth - 32f, 24f);

            const float controlsTop = 158f;
            const float controlsHeight = 38f;
            const float stageGap = 24f;
            DrawPreviewCommandBar(panel, 16f, controlsTop, panelWidth - 32f);

            var gridWidth = GridLabelWidth + controller.CurrentConfig.boardWidth * MineCellSize;
            var viewportHeight = controller.CurrentConfig.visibleHeight * MineCellSize;
            var scrollViewWidth = gridWidth + 28f;
            var mineStage = CreateRect("Mine Stage", panel);
            SetTopLeft(mineStage, 16f, controlsTop + controlsHeight + stageGap, panelWidth - 32f, viewportHeight + 40f);

            var scrollView = CreateRect("Mine Preview Scroll View", mineStage);
            SetTopLeft(scrollView, Mathf.Max(0f, (panelWidth - 32f - scrollViewWidth) * 0.5f), 20f, scrollViewWidth, viewportHeight);
            AddImage(scrollView.gameObject, new Color(0.08f, 0.1f, 0.1f));

            var viewport = CreateRect("Viewport", scrollView);
            SetTopLeft(viewport, 0f, 0f, gridWidth, viewportHeight);
            var viewportImage = AddImage(viewport.gameObject, GetTerrainColor(FossickTerrainType.Empty));
            viewportImage.raycastTarget = true;
            var mask = viewport.gameObject.AddComponent<Mask>();
            mask.showMaskGraphic = true;

            var content = CreateRect("Content", viewport);
            SetTopLeft(content, 0f, 0f, gridWidth, mine.rows.Count * MineCellSize);

            var mineCellRows = BuildConfigRows(mine);
            var rewardBackgroundRoot = CreateRect("Mine Reward Background Regions", content);
            SetTopLeft(rewardBackgroundRoot, GridLabelWidth, 0f, controller.CurrentConfig.boardWidth * MineCellSize, mine.rows.Count * MineCellSize);
            DrawRewardBackgroundRegions(rewardBackgroundRoot, mineCellRows, controller.CurrentConfig.boardWidth, mine.rows.Count, MineCellSize);

            var terrainRoot = CreateRect("Mine Terrain Visuals", content);
            SetTopLeft(terrainRoot, GridLabelWidth, 0f, controller.CurrentConfig.boardWidth * MineCellSize, mine.rows.Count * MineCellSize);
            terrainRoot.gameObject.AddComponent<RectMask2D>();
            DrawTerrainSmoothGrid(terrainRoot, mineCellRows, controller.CurrentConfig.boardWidth, mine.rows.Count, MineCellSize);
            DrawSingleTerrainSprites(terrainRoot, mineCellRows, controller.CurrentConfig.boardWidth, mine.rows.Count, MineCellSize);

            for (var i = 0; i < mine.rows.Count; i++)
            {
                DrawMinePreviewRow(content, mineCellRows, mine.rows[i], i);
            }

            var scrollbar = CreateVerticalScrollbar(scrollView, gridWidth + 8f, 0f, 16f, viewportHeight);
            var scrollRect = scrollView.gameObject.AddComponent<ScrollRect>();
            scrollRect.viewport = viewport;
            scrollRect.content = content;
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            scrollRect.scrollSensitivity = MineCellSize;
            scrollRect.verticalScrollbar = scrollbar;
            scrollRect.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.Permanent;
            scrollRect.verticalNormalizedPosition = mineScrollNormalizedPosition;
            scrollRect.onValueChanged.AddListener(position =>
            {
                mineScrollNormalizedPosition = position.y;
            });

            DrawMineViewportOverlay(viewport, controller.CurrentConfig.boardWidth, controller.CurrentConfig.visibleHeight);
        }

        private void DrawPreviewCommandBar(RectTransform parent, float x, float y, float width)
        {
            var bar = CreateRect("Preview Command Bar", parent);
            SetTopLeft(bar, x, y, width, 42f);
            AddImage(bar.gameObject, new Color(0.11f, 0.13f, 0.15f));

            AddTextAt(bar, "视图操作", 13, FontStyle.Bold, 12f, 0f, 72f, 42f);

            var row = CreateRow(bar, "Preview Command Row", Mathf.Min(width - 96f, 620f), 34f);
            SetTopLeft(row, 92f, 4f, Mathf.Min(width - 96f, 620f), 34f);
            var canReduceRows = minePreviewRows > controller.CurrentConfig.visibleHeight * 2;
            AddActionButton(row, "减少行数", new Vector2(94f, 30f), canReduceRows ? () =>
            {
                minePreviewRows = Mathf.Max(controller.CurrentConfig.visibleHeight * 2, minePreviewRows - controller.CurrentConfig.visibleHeight);
                mineScrollNormalizedPosition = 1f;
                ClearPendingPaint();
                Build();
            } : null, canReduceRows ? ButtonTone.Default : ButtonTone.Muted);
            AddActionButton(row, "向下预览", new Vector2(94f, 30f), () =>
            {
                minePreviewRows = Mathf.Max(controller.CurrentConfig.visibleHeight * 2, minePreviewRows + controller.CurrentConfig.visibleHeight);
                mineScrollNormalizedPosition = 1f;
                ClearPendingPaint();
                Build();
            });
            AddActionButton(row, showFogInEditor ? "隐藏阴影" : "显示阴影", new Vector2(100f, 30f), ToggleFogVisibility, showFogInEditor ? ButtonTone.Primary : ButtonTone.Default);
            AddActionButton(row, "关闭预览", new Vector2(94f, 30f), () =>
            {
                ClearMineInstance("已关闭当前地图预览。");
                Build();
            }, ButtonTone.Danger);
        }

        private void DrawMinePreviewPanel()
        {
            var panel = CreatePanel("Context Panel", root, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(CenterWidth + ColumnGap, 12f), new Vector2(-12f, -HeaderHeight - 24f));
            AddVerticalLayout(panel.gameObject, 10, TextAnchor.UpperLeft);

            var mine = BuildPreviewMine();
            if (templateLibraryOpen)
            {
                DrawTemplateLibraryContext(panel);
            }
            else if (generationRulesOpen)
            {
                DrawGenerationRulesContext(panel);
            }
            else if (editMode == MapStudioEditMode.Template)
            {
                DrawTemplateContext(panel);
            }
            else
            {
                DrawMineInstanceContext(panel, mine);
            }
        }

        private void DrawTemplateContext(RectTransform parent)
        {
            var fragment = GetTemplateEditFragment();
            AddText(parent, "当前模式：模板编辑", 16, FontStyle.Bold, new Vector2(380f, 26f));
            AddText(parent, "编辑的是模板库，保存后才会影响后续地图生成。", 13, FontStyle.Normal, new Vector2(380f, 40f));

            if (fragment == null)
            {
                AddText(parent, "当前没有选中模板。", 13, FontStyle.Normal, new Vector2(380f, 24f));
                return;
            }

            DrawTemplateContextInfo(parent, fragment);
            DrawTemplateContextDifficulty(parent, fragment);
            DrawTemplateContextSize(parent, fragment);
        }

        private void DrawTemplateLibraryContext(RectTransform parent)
        {
            AddText(parent, "当前模式：模板库", 16, FontStyle.Bold, new Vector2(380f, 26f));
            AddText(parent, "模板库管理可复用碎片；地图预览会按生成规则抽取这些模板。", 13, FontStyle.Normal, new Vector2(380f, 44f));

            var fragments = controller.CurrentConfig.fragments;
            var summary = CreateContextSection(parent, "模板库摘要", 148f);
            AddTextAt(summary, "模板库摘要", 15, FontStyle.Bold, 12f, 10f, 330f, 24f);
            AddTextAt(summary, FormatTemplateCounts(fragments), 13, FontStyle.Normal, 12f, 42f, 330f, 48f);
            var selected = GetSelectedFragment();
            AddTextAt(summary, selected == null ? "当前模板：未选择" : $"当前模板：{selected.id}  {FormatFragmentType(selected.type)}", 13, FontStyle.Bold, 12f, 102f, 330f, 24f);

            var usage = CreateContextSection(parent, "操作说明", 156f);
            AddTextAt(usage, "操作说明", 15, FontStyle.Bold, 12f, 10f, 330f, 24f);
            AddTextAt(usage, "1. 左侧选择已有模板或筛选类型。", 13, FontStyle.Normal, 12f, 42f, 330f, 22f);
            AddTextAt(usage, "2. 右侧选择新建预设后点击创建模板。", 13, FontStyle.Normal, 12f, 70f, 330f, 22f);
            AddTextAt(usage, "3. 编辑并保存模板后，重新生成地图预览即可参与抽取。", 13, FontStyle.Normal, 12f, 98f, 330f, 40f);
        }

        private void DrawTemplateContextInfo(RectTransform parent, FossickFragmentConfig fragment)
        {
            var section = CreateContextSection(parent, "模板属性", 144f);
            AddTextAt(section, "模板属性", 15, FontStyle.Bold, 12f, 10f, 330f, 24f);
            AddTextAt(section, $"ID {fragment.id}", 13, FontStyle.Bold, 12f, 40f, 330f, 24f);
            var typeRow = CreateRow(section, "Context Template Type Row", 330f, 34f);
            SetTopLeft(typeRow, 12f, 68f, 330f, 34f);
            AddButton(typeRow, "新手", new Vector2(76f, 30f), () => SetTemplateDraftType(FossickFragmentType.Tutorial), fragment.type == FossickFragmentType.Tutorial);
            AddButton(typeRow, "常规", new Vector2(76f, 30f), () => SetTemplateDraftType(FossickFragmentType.Regular), fragment.type == FossickFragmentType.Regular);
            AddButton(typeRow, "奖励", new Vector2(76f, 30f), () => SetTemplateDraftType(FossickFragmentType.Reward), fragment.type == FossickFragmentType.Reward);
            var poolText = fragment.type == FossickFragmentType.Regular
                ? "常规模板进入难度池。"
                : $"{FormatFragmentType(fragment.type)}模板不参与常规难度池。";
            AddTextAt(section, poolText, 12, FontStyle.Normal, 12f, 112f, 330f, 22f);
        }

        private void DrawTemplateContextDifficulty(RectTransform parent, FossickFragmentConfig fragment)
        {
            var section = CreateContextSection(parent, "难度", 104f);
            AddTextAt(section, "难度", 15, FontStyle.Bold, 12f, 10f, 330f, 24f);
            if (fragment.type != FossickFragmentType.Regular)
            {
                AddTextAt(section, "当前类型不需要设置难度。", 13, FontStyle.Normal, 12f, 48f, 330f, 28f);
                return;
            }

            var difficultyRow = CreateRow(section, "Context Difficulty Row", 330f, 34f);
            SetTopLeft(difficultyRow, 12f, 48f, 330f, 34f);
            AddButton(difficultyRow, "1", new Vector2(58f, 30f), () => SetTemplateDraftDifficulty(1), fragment.difficulty == 1);
            AddButton(difficultyRow, "2", new Vector2(58f, 30f), () => SetTemplateDraftDifficulty(2), fragment.difficulty == 2);
            AddButton(difficultyRow, "3", new Vector2(58f, 30f), () => SetTemplateDraftDifficulty(3), fragment.difficulty == 3);
        }

        private void DrawTemplateContextSize(RectTransform parent, FossickFragmentConfig fragment)
        {
            var section = CreateContextSection(parent, "尺寸", 104f);
            AddTextAt(section, "尺寸", 15, FontStyle.Bold, 12f, 10f, 330f, 24f);
            var sizeRow = CreateRow(section, "Context Size Row", 330f, 34f);
            SetTopLeft(sizeRow, 12f, 48f, 330f, 34f);
            AddButton(sizeRow, "-", new Vector2(52f, 30f), () => ResizeTemplateDraft(Mathf.Max(1, fragment.height - 1)));
            AddButton(sizeRow, $"高度 {fragment.height}", new Vector2(100f, 30f), () => { }, true);
            AddButton(sizeRow, "+", new Vector2(52f, 30f), () => ResizeTemplateDraft(Mathf.Min(24, fragment.height + 1)));
        }

        private void DrawGenerationRulesContext(RectTransform parent)
        {
            var generation = EnsureGenerationConfig();
            AddText(parent, "当前模式：生成规则", 16, FontStyle.Bold, new Vector2(380f, 26f));
            AddText(parent, generationRulesEditDirty ? "规则有未保存修改。" : "规则已保存。", 13, generationRulesEditDirty ? FontStyle.Bold : FontStyle.Normal, new Vector2(380f, 30f));
            DrawGenerationRulesSummaryCard(parent, generation);
            DrawGenerationRulesValidationCard(parent);
        }

        private void DrawGenerationRulesPanel()
        {
            var panelLeft = LeftWidth + 24f;
            var panelRight = CenterWidth;
            var panelWidth = panelRight - panelLeft;
            var panel = CreatePanel("Generation Rules Panel", root, new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(panelLeft, 12f), new Vector2(panelRight, -HeaderHeight - 24f));
            var generation = EnsureGenerationConfig();

            var title = AddText(panel, "生成规则", 20, FontStyle.Bold, new Vector2(panelWidth - 32f, 30f));
            SetTopLeft(title.GetComponent<RectTransform>(), 16f, 14f, panelWidth - 32f, 30f);
            DrawModeBadge(panel, panelWidth, 14f, 160f);
            var backButton = AddActionButton(panel, "返回地图预览", new Vector2(128f, 30f), ReturnFromGenerationRules);
            SetTopLeft(backButton, panelWidth - 144f, 14f, 128f, 30f);

            var desc = AddText(panel, "配置普通碎片随机抽取、难度配比和藏宝阁插入间隔；保存后写入规则文件，应用后刷新地图预览。", 13, FontStyle.Normal, new Vector2(panelWidth - 32f, 24f));
            SetTopLeft(desc.GetComponent<RectTransform>(), 16f, 54f, panelWidth - 32f, 24f);
            if (!string.IsNullOrEmpty(editNotice))
            {
                DrawFeedbackBanner(panel, 16f, 82f, panelWidth - 32f);
            }

            const float contentTop = 128f;
            var editorWidth = Mathf.Min(660f, panelWidth - 424f);
            var summaryWidth = 340f;
            var editor = CreateRect("Generation Rules Editor Card", panel);
            SetTopLeft(editor, 16f, contentTop, editorWidth, 650f);
            AddImage(editor.gameObject, new Color(0.1f, 0.13f, 0.15f));

            var summary = CreateRect("Generation Rules Summary Card", panel);
            SetTopLeft(summary, 16f + editorWidth + 20f, contentTop, summaryWidth, 650f);
            AddImage(summary.gameObject, new Color(0.1f, 0.13f, 0.15f));

            DrawGenerationRulesEditorCard(editor, generation, editorWidth);
            DrawGenerationRulesInlineSummary(summary, generation, summaryWidth);
        }

        private void DrawGenerationRulesEditorCard(RectTransform parent, FossickGenerationConfig generation, float width)
        {
            AddTextAt(parent, "规则编辑", 17, FontStyle.Bold, 16f, 14f, width - 32f, 26f);

            AddTextAt(parent, "普通碎片一轮", 13, FontStyle.Bold, 16f, 58f, width - 32f, 22f);
            DrawStepper(parent, 16f, 86f, "一轮普通碎片", generation.regularGroupSize, 1, value =>
            {
                generation.regularGroupSize = Mathf.Max(1, value);
                MarkGenerationRulesChanged();
            });
            AddTextAt(parent, $"当前难度合计 {GetDifficultyCountTotal(generation)} 段", 12, FontStyle.Normal, 270f, 92f, width - 286f, 22f);

            AddTextAt(parent, "难度分布", 13, FontStyle.Bold, 16f, 146f, width - 32f, 22f);
            for (var difficulty = 1; difficulty <= 3; difficulty++)
            {
                DrawDifficultyStepperRow(parent, generation, difficulty, 16f, 176f + (difficulty - 1) * 50f, width - 32f);
            }

            AddTextAt(parent, "藏宝阁插入", 13, FontStyle.Bold, 16f, 348f, width - 32f, 22f);
            DrawStepper(parent, 16f, 376f, "最小间隔", generation.rewardInsertMin, 1, value =>
            {
                generation.rewardInsertMin = Mathf.Max(1, value);
                if (generation.rewardInsertMin > generation.rewardInsertMax)
                {
                    generation.rewardInsertMax = generation.rewardInsertMin;
                }

                MarkGenerationRulesChanged();
            });
            DrawStepper(parent, 16f, 424f, "最大间隔", generation.rewardInsertMax, generation.rewardInsertMin, value =>
            {
                generation.rewardInsertMax = Mathf.Max(generation.rewardInsertMin, value);
                MarkGenerationRulesChanged();
            });
            AddTextAt(parent, GetRewardPoolSummary(), 12, FontStyle.Normal, 270f, 398f, width - 286f, 22f);

            var smallCoinDrop = EnsureSmallCoinDropConfig(generation);
            AddTextAt(parent, "普通障碍小金币", 13, FontStyle.Bold, 16f, 480f, width - 32f, 22f);
            var enabledButton = AddActionButton(parent, smallCoinDrop.enabled ? "小金币掉落 开" : "小金币掉落 关", new Vector2(136f, 30f), () =>
            {
                smallCoinDrop.enabled = !smallCoinDrop.enabled;
                MarkGenerationRulesChanged();
            }, smallCoinDrop.enabled ? ButtonTone.Primary : ButtonTone.Default);
            SetTopLeft(enabledButton, 16f, 508f, 136f, 30f);
            DrawStepper(parent, 168f, 508f, "概率‰", smallCoinDrop.chancePerMille, 0, value =>
            {
                smallCoinDrop.chancePerMille = Mathf.Clamp(value, 0, 1000);
                MarkGenerationRulesChanged();
            });
            AddTextAt(parent, $"数量池：{FormatWeightedAmounts(smallCoinDrop.amounts)}", 12, FontStyle.Normal, 16f, 548f, width - 32f, 24f);

            var actionRow = CreateRow(parent, "Generation Rules Action Row", width - 32f, 34f);
            SetTopLeft(actionRow, 16f, 596f, width - 32f, 34f);
            AddActionButton(actionRow, "放弃修改", new Vector2(104f, 30f), generationRulesEditDirty ? DiscardGenerationRulesChanges : null, generationRulesEditDirty ? ButtonTone.Danger : ButtonTone.Muted);
            AddActionButton(actionRow, "保存规则", new Vector2(104f, 30f), SaveGenerationRules, ButtonTone.Primary);
            AddActionButton(actionRow, "应用并更新预览", new Vector2(142f, 30f), ApplyGenerationRulesAndUpdatePreview, ButtonTone.Primary);
        }

        private void DrawGenerationRulesInlineSummary(RectTransform parent, FossickGenerationConfig generation, float width)
        {
            AddTextAt(parent, "摘要与校验", 17, FontStyle.Bold, 16f, 14f, width - 32f, 26f);
            var valid = GetGenerationRuleIssueCount() == 0;
            var validationCard = CreateRect("Generation Validation State", parent);
            SetTopLeft(validationCard, 16f, 58f, width - 32f, 40f);
            AddImage(validationCard.gameObject, valid ? new Color(0.14f, 0.32f, 0.23f) : new Color(0.38f, 0.29f, 0.17f));
            AddTextAt(validationCard, valid ? "校验通过" : $"有 {GetGenerationRuleIssueCount()} 个问题", 14, FontStyle.Bold, 12f, 8f, width - 56f, 24f);

            AddTextAt(parent, $"每轮普通矿井：抽取 {generation.regularGroupSize} 段", 13, FontStyle.Normal, 16f, 126f, width - 32f, 22f);
            AddTextAt(parent, $"难度配比：难度1 {GetDifficultyCount(generation, 1)}段 / 难度2 {GetDifficultyCount(generation, 2)}段 / 难度3 {GetDifficultyCount(generation, 3)}段", 13, FontStyle.Normal, 16f, 154f, width - 32f, 36f);
            AddTextAt(parent, $"藏宝阁插入：每 {generation.rewardInsertMin}-{generation.rewardInsertMax} 段普通矿井插入 1 个", 13, FontStyle.Normal, 16f, 196f, width - 32f, 36f);
            AddTextAt(parent, FormatSmallCoinDropSummary(generation.smallCoinDrop), 13, FontStyle.Normal, 16f, 238f, width - 32f, 42f);
            AddTextAt(parent, generationRulesEditDirty ? "规则保存：有未保存修改" : "规则保存：已保存", 13, generationRulesEditDirty ? FontStyle.Bold : FontStyle.Normal, 16f, 296f, width - 32f, 22f);
            AddTextAt(parent, generationRulesDirty ? "地图预览：规则已变化，需要更新预览" : "地图预览：当前预览已同步", 13, generationRulesDirty ? FontStyle.Bold : FontStyle.Normal, 16f, 324f, width - 32f, 22f);

            AddTextAt(parent, "可抽取模板池", 15, FontStyle.Bold, 16f, 374f, width - 32f, 24f);
            AddTextAt(parent, $"难度 1：{GetRegularPoolCount(1)} 个", 13, FontStyle.Normal, 16f, 396f, width - 32f, 22f);
            AddTextAt(parent, $"难度 2：{GetRegularPoolCount(2)} 个", 13, FontStyle.Normal, 16f, 424f, width - 32f, 22f);
            AddTextAt(parent, $"难度 3：{GetRegularPoolCount(3)} 个", 13, FontStyle.Normal, 16f, 452f, width - 32f, 22f);
            AddTextAt(parent, GetRewardPoolSummary(), 13, FontStyle.Normal, 16f, 480f, width - 32f, 22f);

            var issueText = GetFirstGenerationRuleIssueText();
            AddTextAt(parent, string.IsNullOrEmpty(issueText) ? "规则可用于生成矿井。" : issueText, 12, string.IsNullOrEmpty(issueText) ? FontStyle.Normal : FontStyle.Bold, 16f, 548f, width - 32f, 44f);
        }

        private void DrawGenerationRulesSummaryCard(RectTransform parent, FossickGenerationConfig generation)
        {
            var section = CreateContextSection(parent, "生成规则摘要", 176f);
            AddTextAt(section, "生成规则摘要", 15, FontStyle.Bold, 12f, 10f, 330f, 24f);
            AddTextAt(section, $"每轮普通矿井：抽取 {generation.regularGroupSize} 段", 13, FontStyle.Normal, 12f, 42f, 330f, 22f);
            AddTextAt(section, $"难度配比：难度1 {GetDifficultyCount(generation, 1)} / 难度2 {GetDifficultyCount(generation, 2)} / 难度3 {GetDifficultyCount(generation, 3)}", 13, FontStyle.Normal, 12f, 68f, 330f, 36f);
            AddTextAt(section, $"藏宝阁插入：每 {generation.rewardInsertMin}-{generation.rewardInsertMax} 段 1 个", 13, FontStyle.Normal, 12f, 104f, 330f, 22f);
            AddTextAt(section, generationRulesDirty ? "地图预览需要更新。" : "地图预览已同步。", 13, generationRulesDirty ? FontStyle.Bold : FontStyle.Normal, 12f, 134f, 330f, 22f);
        }

        private void DrawGenerationRulesValidationCard(RectTransform parent)
        {
            var section = CreateContextSection(parent, "规则校验", 128f);
            var issueCount = GetGenerationRuleIssueCount();
            AddTextAt(section, "规则校验", 15, FontStyle.Bold, 12f, 10f, 330f, 24f);
            AddTextAt(section, issueCount == 0 ? "校验：通过" : $"校验：{issueCount} 个问题", 13, issueCount == 0 ? FontStyle.Normal : FontStyle.Bold, 12f, 42f, 330f, 22f);
            AddTextAt(section, GetFirstGenerationRuleIssueText() ?? "没有发现问题。", 12, FontStyle.Normal, 12f, 72f, 330f, 42f);
        }

        private void DrawStepper(RectTransform parent, float x, float y, string label, int value, int minValue, Action<int> onChanged)
        {
            AddTextAt(parent, label, 13, FontStyle.Normal, x, y + 4f, 106f, 24f);
            var minus = AddActionButton(parent, "-", new Vector2(32f, 30f), value > minValue ? () => onChanged(value - 1) : null, value > minValue ? ButtonTone.Default : ButtonTone.Muted);
            SetTopLeft(minus, x + 116f, y, 32f, 30f);
            var valueRect = CreateRect($"{label} Value", parent);
            SetTopLeft(valueRect, x + 154f, y, 54f, 30f);
            AddImage(valueRect.gameObject, ButtonMuted);
            AddTextAt(valueRect, value.ToString(), 14, FontStyle.Bold, 0f, 3f, 54f, 24f, TextAnchor.MiddleCenter);
            var plus = AddActionButton(parent, "+", new Vector2(32f, 30f), () => onChanged(value + 1));
            SetTopLeft(plus, x + 214f, y, 32f, 30f);
        }

        private void DrawDifficultyStepperRow(RectTransform parent, FossickGenerationConfig generation, int difficulty, float x, float y, float width)
        {
            var count = GetDifficultyCount(generation, difficulty);
            DrawStepper(parent, x, y, $"难度 {difficulty}", count, 0, value =>
            {
                SetDifficultyCount(generation, difficulty, value);
                MarkGenerationRulesChanged();
            });
            AddTextAt(parent, GetRegularPoolSummary(difficulty), 12, FontStyle.Normal, x + 270f, y + 4f, width - 270f, 24f);
        }

        private void DrawMineInstanceContext(RectTransform parent, FossickGeneratedMine mine)
        {
            AddText(parent, "当前模式：地图预览", 16, FontStyle.Bold, new Vector2(380f, 26f));
            AddText(parent, $"预览种子 {mineInstanceSeed} | 当前预览 {mine.rows.Count} 行 | 可视窗口 {controller.CurrentConfig.boardWidth} x {controller.CurrentConfig.visibleHeight}", 13, FontStyle.Normal, new Vector2(380f, 40f));
            DrawMineSelectionInfo(parent, mine);
            DrawGenerationRulesSummary(parent);
            DrawValidationSummary(parent);
        }

        private void DrawGenerationRulesSummary(RectTransform parent)
        {
            var generation = mineInstanceSourceConfig == null ? null : mineInstanceSourceConfig.generation;
            if (generation == null)
            {
                AddText(parent, "当前预览：未生成", 14, FontStyle.Bold, new Vector2(380f, 24f));
                return;
            }

            AddText(parent, generationRulesDirty ? "当前预览规则（有未应用修改）" : "当前预览规则", 16, FontStyle.Bold, new Vector2(380f, 24f));
            AddText(parent, $"半随机：每轮 {generation.regularGroupSize} 段按难度配比组包，再用种子洗牌。", 13, FontStyle.Normal, new Vector2(380f, 22f));
            AddText(parent, $"难度分布 {FormatDifficultyDistribution(generation)}；每隔 {generation.rewardInsertMin}-{generation.rewardInsertMax} 段插入藏宝阁。", 13, FontStyle.Normal, new Vector2(380f, 22f));
            AddText(parent, "预览矿井按需无限向下追加；中间只展示当前预览行。", 13, FontStyle.Normal, new Vector2(380f, 22f));
        }

        private int GetDifficultyCountTotal(FossickGenerationConfig generation)
        {
            if (generation == null || generation.difficultyCounts == null)
            {
                return 0;
            }

            var total = 0;
            for (var i = 0; i < generation.difficultyCounts.Count; i++)
            {
                var entry = generation.difficultyCounts[i];
                if (entry != null && entry.count > 0)
                {
                    total += entry.count;
                }
            }

            return total;
        }

        private string FormatDifficultyDistribution(FossickGenerationConfig generation)
        {
            if (generation == null)
            {
                return "未配置";
            }

            var d1 = GetDifficultyCount(generation, 1);
            var d2 = GetDifficultyCount(generation, 2);
            var d3 = GetDifficultyCount(generation, 3);
            return $"难度1:{d1} / 难度2:{d2} / 难度3:{d3}";
        }

        private void MarkGenerationRulesChanged()
        {
            generationRulesDirty = true;
            generationRulesEditDirty = true;
            editNotice = "生成规则已修改。保存后可应用并更新预览。";
            controller.Validate();
            Build();
        }

        private void MarkTemplateLibraryChanged()
        {
            generationRulesDirty = true;
        }

        private void BeginTemplateEdit(int index, string notice)
        {
            var fragments = controller.CurrentConfig.fragments;
            if (fragments == null || fragments.Count == 0)
            {
                return;
            }

            selectedFragmentIndex = Mathf.Clamp(index, 0, fragments.Count - 1);
            templateEditSourceIndex = selectedFragmentIndex;
            templateEditDraft = CloneFragmentForMineOccurrence(fragments[templateEditSourceIndex]);
            NormalizeFragmentDifficulty(templateEditDraft);
            templateEditDirty = false;
            templateUndoStack.Clear();
            templateRedoStack.Clear();
            editMode = MapStudioEditMode.Template;
            templateLibraryOpen = false;
            generationRulesOpen = false;
            ClearPendingPaint();
            editNotice = notice;
            Build();
        }

        private FossickFragmentConfig GetTemplateEditFragment()
        {
            if (editMode != MapStudioEditMode.Template)
            {
                return null;
            }

            var source = GetSelectedFragment();
            if (source == null)
            {
                templateEditDraft = null;
                templateEditSourceIndex = -1;
                templateEditDirty = false;
                templateUndoStack.Clear();
                templateRedoStack.Clear();
                return null;
            }

            if (templateEditDraft == null || templateEditSourceIndex != selectedFragmentIndex)
            {
                templateEditSourceIndex = selectedFragmentIndex;
                templateEditDraft = CloneFragmentForMineOccurrence(source);
                NormalizeFragmentDifficulty(templateEditDraft);
                templateEditDirty = false;
                templateUndoStack.Clear();
                templateRedoStack.Clear();
            }

            return templateEditDraft;
        }

        private void RecordTemplateUndoSnapshot()
        {
            if (templateEditDraft == null)
            {
                return;
            }

            templateUndoStack.Add(JsonUtility.ToJson(templateEditDraft));
            if (templateUndoStack.Count > 50)
            {
                templateUndoStack.RemoveAt(0);
            }

            templateRedoStack.Clear();
        }

        private void MarkTemplateDraftChanged()
        {
            templateEditDirty = true;
        }

        private void UndoTemplateEdit()
        {
            if (templateUndoStack.Count == 0 || templateEditDraft == null)
            {
                return;
            }

            templateRedoStack.Add(JsonUtility.ToJson(templateEditDraft));
            var last = templateUndoStack[templateUndoStack.Count - 1];
            templateUndoStack.RemoveAt(templateUndoStack.Count - 1);
            templateEditDraft = JsonUtility.FromJson<FossickFragmentConfig>(last);
            NormalizeFragmentDifficulty(templateEditDraft);
            templateEditDirty = true;
            ClearPendingPaint();
            editNotice = "已撤销上一步模板编辑。";
            Build();
        }

        private void RedoTemplateEdit()
        {
            if (templateRedoStack.Count == 0 || templateEditDraft == null)
            {
                return;
            }

            templateUndoStack.Add(JsonUtility.ToJson(templateEditDraft));
            var next = templateRedoStack[templateRedoStack.Count - 1];
            templateRedoStack.RemoveAt(templateRedoStack.Count - 1);
            templateEditDraft = JsonUtility.FromJson<FossickFragmentConfig>(next);
            NormalizeFragmentDifficulty(templateEditDraft);
            templateEditDirty = true;
            ClearPendingPaint();
            editNotice = "已重做模板编辑。";
            Build();
        }

        private void SaveTemplateEdit()
        {
            SaveTemplateEditInternal(true);
        }

        private bool SaveTemplateEditInternal(bool rebuild)
        {
            var fragments = controller.CurrentConfig.fragments;
            if (templateEditDraft == null || fragments == null || templateEditSourceIndex < 0 || templateEditSourceIndex >= fragments.Count)
            {
                return false;
            }

            NormalizeFragmentDifficulty(templateEditDraft);
            fragments[templateEditSourceIndex] = CloneFragmentForMineOccurrence(templateEditDraft);
            selectedFragmentIndex = templateEditSourceIndex;
            templateEditDraft = CloneFragmentForMineOccurrence(fragments[templateEditSourceIndex]);
            templateEditDirty = false;
            templateUndoStack.Clear();
            templateRedoStack.Clear();
            MarkTemplateLibraryChanged();
            controller.Validate();
            ClearPendingPaint();
            SaveProjectFiles();
            editNotice = $"已保存模板 {templateEditDraft.id}。重新生成地图预览后可看到新模板参与拼接。";
            if (rebuild)
            {
                Build();
            }

            return true;
        }

        private void DiscardTemplateEdit()
        {
            DiscardTemplateEditInternal(true);
        }

        private bool DiscardTemplateEditInternal(bool rebuild)
        {
            var fragments = controller.CurrentConfig.fragments;
            if (fragments == null || templateEditSourceIndex < 0 || templateEditSourceIndex >= fragments.Count)
            {
                editMode = MapStudioEditMode.MineInstance;
                if (rebuild)
                {
                    Build();
                }

                return false;
            }

            selectedFragmentIndex = templateEditSourceIndex;
            templateEditDraft = CloneFragmentForMineOccurrence(fragments[templateEditSourceIndex]);
            NormalizeFragmentDifficulty(templateEditDraft);
            templateEditDirty = false;
            templateUndoStack.Clear();
            templateRedoStack.Clear();
            ClearPendingPaint();
            editNotice = $"已放弃模板 {templateEditDraft.id} 的未保存修改。";
            if (rebuild)
            {
                Build();
            }

            return true;
        }

        private void ReturnFromTemplateEdit()
        {
            if (templateEditDirty)
            {
                ShowUnsavedTemplateDialog("返回地图预览", "当前模板有未保存修改，请选择保存或丢弃。", ReturnToMineInstanceAfterTemplateDecision);
                return;
            }

            ReturnToMineInstanceAfterTemplateDecision();
        }

        private void ReturnToMineInstanceAfterTemplateDecision()
        {
            editMode = MapStudioEditMode.MineInstance;
            ClearPendingPaint();
            Build();
        }

        private void OpenGeneratedPreviewFromMenu()
        {
            if (editMode == MapStudioEditMode.Template && templateEditDirty)
            {
                ShowUnsavedTemplateDialog("查看地图预览", "当前模板有未保存修改，请选择保存或丢弃。", OpenGeneratedPreviewInternal);
                return;
            }

            OpenGeneratedPreviewInternal();
        }

        private void OpenGeneratedPreviewInternal()
        {
            editMode = MapStudioEditMode.MineInstance;
            templateLibraryOpen = false;
            generationRulesOpen = false;
            ClearPendingPaint();

            if (!mineInstanceGenerated)
            {
                GenerateMineInstance(true, "已按当前模板、半随机规则和种子生成地图预览；矿井会按需无限向下延展。");
            }
            else
            {
                editNotice = "已切换到地图预览。";
                controller.Validate();
            }

            Build();
        }

        private void GenerateMineInstance(bool clearEdits, string notice)
        {
            if (controller == null || controller.CurrentConfig == null)
            {
                return;
            }

            if (clearEdits)
            {
                ClearMineInstanceEdits();
            }

            mineInstanceSourceConfig = CloneMapConfig(controller.CurrentConfig);
            StripInstanceOverrides(mineInstanceSourceConfig);
            mineInstanceSeed = controller.Seed;
            mineInstanceGenerated = true;
            generationRulesDirty = false;
            selectedMineSequenceIndex = -1;
            mineScrollNormalizedPosition = 1f;
            ClearPendingPaint();
            editNotice = notice;
            controller.Validate();
        }

        private void ClearMineInstance(string notice)
        {
            ClearMineInstanceEdits();
            mineInstanceSourceConfig = null;
            mineInstanceSeed = controller == null ? 0 : controller.Seed;
            mineInstanceGenerated = false;
            generationRulesDirty = false;
            selectedMineSequenceIndex = -1;
            ClearPendingPaint();
            editNotice = notice;
            controller.Validate();
        }

        private void ClearMineInstanceEdits()
        {
            var generation = controller == null || controller.CurrentConfig == null ? null : controller.CurrentConfig.generation;
            if (generation == null)
            {
                return;
            }

            if (generation.sequenceOverrides != null)
            {
                generation.sequenceOverrides.Clear();
            }

            if (generation.rowOverrides != null)
            {
                generation.rowOverrides.Clear();
            }
        }

        private static void StripInstanceOverrides(FossickMapConfig config)
        {
            if (config == null || config.generation == null)
            {
                return;
            }

            config.generation.sequenceOverrides = new List<FossickSequenceOverrideConfig>();
            config.generation.rowOverrides = new List<FossickRowOverrideConfig>();
        }

        private static FossickMapConfig CloneMapConfig(FossickMapConfig source)
        {
            if (source == null)
            {
                return null;
            }

            return FossickMapJsonUtility.FromJson(FossickMapJsonUtility.ToJson(source));
        }

        private FossickGenerationConfig EnsureGenerationConfig()
        {
            if (controller.CurrentConfig.generation == null)
            {
                controller.CurrentConfig.generation = new FossickGenerationConfig();
            }

            return controller.CurrentConfig.generation;
        }

        private static FossickGenerationConfig CloneGenerationConfig(FossickGenerationConfig source)
        {
            if (source == null)
            {
                return new FossickGenerationConfig();
            }

            return new FossickGenerationConfig
            {
                regularGroupSize = source.regularGroupSize,
                rewardInsertMin = source.rewardInsertMin,
                rewardInsertMax = source.rewardInsertMax,
                prefetchVisibleScreens = source.prefetchVisibleScreens,
                minimumRowsAhead = source.minimumRowsAhead,
                retainRowsBehind = source.retainRowsBehind,
                smallCoinDrop = CloneSmallCoinDrop(source.smallCoinDrop),
                difficultyCounts = source.difficultyCounts == null
                    ? new List<FossickDifficultyCount>()
                    : source.difficultyCounts.ConvertAll(count => count == null
                        ? null
                        : new FossickDifficultyCount
                        {
                            difficulty = count.difficulty,
                            count = count.count
                        })
            };
        }

        private static FossickSmallCoinDropConfig CloneSmallCoinDrop(FossickSmallCoinDropConfig source)
        {
            if (source == null)
            {
                return new FossickSmallCoinDropConfig();
            }

            return new FossickSmallCoinDropConfig
            {
                enabled = source.enabled,
                coinId = string.IsNullOrEmpty(source.coinId) ? "coin_pile" : source.coinId,
                chancePerMille = source.chancePerMille,
                amounts = source.amounts == null
                    ? new List<FossickWeightedAmountConfig>()
                    : source.amounts.ConvertAll(amount => amount == null
                        ? null
                        : new FossickWeightedAmountConfig
                        {
                            amount = amount.amount,
                            weight = amount.weight
                        })
            };
        }

        private static FossickSmallCoinDropConfig EnsureSmallCoinDropConfig(FossickGenerationConfig generation)
        {
            if (generation.smallCoinDrop == null)
            {
                generation.smallCoinDrop = new FossickSmallCoinDropConfig();
            }

            if (generation.smallCoinDrop.amounts == null)
            {
                generation.smallCoinDrop.amounts = new List<FossickWeightedAmountConfig>();
            }

            if (generation.smallCoinDrop.amounts.Count == 0)
            {
                generation.smallCoinDrop.amounts.Add(new FossickWeightedAmountConfig { amount = 5, weight = 5 });
                generation.smallCoinDrop.amounts.Add(new FossickWeightedAmountConfig { amount = 10, weight = 3 });
                generation.smallCoinDrop.amounts.Add(new FossickWeightedAmountConfig { amount = 20, weight = 1 });
            }

            if (string.IsNullOrEmpty(generation.smallCoinDrop.coinId))
            {
                generation.smallCoinDrop.coinId = "coin_pile";
            }

            return generation.smallCoinDrop;
        }

        private static string FormatSmallCoinDropSummary(FossickSmallCoinDropConfig smallCoinDrop)
        {
            if (smallCoinDrop == null || !smallCoinDrop.enabled)
            {
                return "普通障碍小金币：关闭。";
            }

            return $"普通障碍小金币：破坏无内容物土/石时 {smallCoinDrop.chancePerMille}‰ 掉落；数量池 {FormatWeightedAmounts(smallCoinDrop.amounts)}。";
        }

        private static string FormatWeightedAmounts(List<FossickWeightedAmountConfig> amounts)
        {
            if (amounts == null || amounts.Count == 0)
            {
                return "未配置";
            }

            var parts = new List<string>();
            for (var i = 0; i < amounts.Count; i++)
            {
                var entry = amounts[i];
                if (entry == null || entry.amount <= 0 || entry.weight <= 0)
                {
                    continue;
                }

                parts.Add($"{entry.amount}x{entry.weight}");
            }

            return parts.Count == 0 ? "未配置" : string.Join(" / ", parts);
        }

        private int GetDifficultyCount(FossickGenerationConfig generation, int difficulty)
        {
            if (generation == null || generation.difficultyCounts == null)
            {
                return 0;
            }

            for (var i = 0; i < generation.difficultyCounts.Count; i++)
            {
                var entry = generation.difficultyCounts[i];
                if (entry != null && entry.difficulty == difficulty)
                {
                    return Mathf.Max(0, entry.count);
                }
            }

            return 0;
        }

        private void SetDifficultyCount(FossickGenerationConfig generation, int difficulty, int count)
        {
            if (generation == null)
            {
                return;
            }

            if (generation.difficultyCounts == null)
            {
                generation.difficultyCounts = new List<FossickDifficultyCount>();
            }

            for (var i = generation.difficultyCounts.Count - 1; i >= 0; i--)
            {
                var entry = generation.difficultyCounts[i];
                if (entry == null)
                {
                    generation.difficultyCounts.RemoveAt(i);
                    continue;
                }

                if (entry.difficulty == difficulty)
                {
                    if (count <= 0)
                    {
                        generation.difficultyCounts.RemoveAt(i);
                    }
                    else
                    {
                        entry.count = count;
                    }

                    return;
                }
            }

            if (count > 0)
            {
                generation.difficultyCounts.Add(new FossickDifficultyCount
                {
                    difficulty = difficulty,
                    count = count
                });
            }
        }

        private string GetRegularPoolSummary(int difficulty)
        {
            var count = GetRegularPoolCount(difficulty);
            return $"池内常规碎片 {count}";
        }

        private int GetRegularPoolCount(int difficulty)
        {
            var count = 0;
            var fragments = controller.CurrentConfig.fragments;
            for (var i = 0; i < fragments.Count; i++)
            {
                var fragment = fragments[i];
                if (fragment != null && fragment.type == FossickFragmentType.Regular && fragment.difficulty == difficulty)
                {
                    count++;
                }
            }

            return count;
        }

        private string GetRewardPoolSummary()
        {
            var count = 0;
            var fragments = controller.CurrentConfig.fragments;
            for (var i = 0; i < fragments.Count; i++)
            {
                var fragment = fragments[i];
                if (fragment != null && fragment.type == FossickFragmentType.Reward)
                {
                    count++;
                }
            }

            return $"奖励模板：{count} 个";
        }

        private int GetGenerationRuleIssueCount()
        {
            var result = controller.LastValidation ?? controller.Validate();
            var count = 0;
            for (var i = 0; i < result.issues.Count; i++)
            {
                var issue = result.issues[i];
                if (issue != null && issue.category == FossickValidationCategory.GenerationRules)
                {
                    count++;
                }
            }

            return count;
        }

        private string GetFirstGenerationRuleIssueText()
        {
            var result = controller.LastValidation ?? controller.Validate();
            for (var i = 0; i < result.issues.Count; i++)
            {
                var issue = result.issues[i];
                if (issue != null && issue.category == FossickValidationCategory.GenerationRules)
                {
                    return FormatIssue(issue);
                }
            }

            return null;
        }

        private void DrawMineSelectionInfo(RectTransform parent, FossickGeneratedMine mine)
        {
            var span = FindGeneratedSpan(mine, selectedMineSequenceIndex);
            AddText(parent, "当前预览段", 18, FontStyle.Bold, new Vector2(380f, 28f));
            if (span == null)
            {
                AddText(parent, "未选择。点击中间矿井格子后，这里会显示来源模板。", 13, FontStyle.Normal, new Vector2(380f, 40f));
                return;
            }

            AddText(parent, $"预览段 #{span.sequenceIndex:00} | 来源模板 {span.fragmentId} | {FormatFragmentType(span.fragmentType)} | 行 {span.startRow:000}-{span.startRow + span.height - 1:000}", 13, FontStyle.Normal, new Vector2(380f, 44f));
        }

        private void DrawValidationSummary(RectTransform parent)
        {
            var validation = controller.LastValidation;
            var issueCount = validation == null || validation.issues == null ? 0 : validation.issues.Count;
            AddText(parent, validation != null && validation.HasErrors ? "校验：有错误" : "校验：通过", 16, FontStyle.Bold, new Vector2(380f, 26f));
            AddText(parent, issueCount == 0 ? "没有发现问题。" : $"发现 {issueCount} 个问题，请检查模板、生成规则或地图配置。", 13, FontStyle.Normal, new Vector2(380f, 44f));
        }

        private void DrawFeedbackBanner(RectTransform parent, float x, float y, float width)
        {
            if (string.IsNullOrEmpty(currentFeedbackMessage))
            {
                return;
            }

            var color = GetFeedbackColor(currentFeedbackKind);
            var banner = CreateRect("Feedback Banner", parent);
            SetTopLeft(banner, x, y, width, 32f);
            AddImage(banner.gameObject, new Color(color.r, color.g, color.b, 0.16f)).raycastTarget = false;

            var stripe = CreateRect("Feedback Stripe", banner);
            SetTopLeft(stripe, 0f, 0f, 4f, 32f);
            AddImage(stripe.gameObject, color).raycastTarget = false;

            var text = AddText(
                banner,
                $"{GetFeedbackLabel(currentFeedbackKind)}  {currentFeedbackMessage}",
                13,
                FontStyle.Bold,
                new Vector2(width - 20f, 32f));
            SetTopLeft(text.GetComponent<RectTransform>(), 12f, 0f, width - 20f, 32f);
            text.color = currentFeedbackKind == MapStudioFeedbackKind.Warning
                ? new Color(1f, 0.91f, 0.65f)
                : TextColor;
        }

        private void SetFeedback(string message, MapStudioFeedbackKind kind)
        {
            currentFeedbackMessage = message;
            currentFeedbackKind = kind;
        }

        private static MapStudioFeedbackKind InferFeedbackKind(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                return MapStudioFeedbackKind.Info;
            }

            if (message.Contains("错误") || message.Contains("失败") || message.Contains("无法") || message.Contains("不能") || message.Contains("超出") || message.Contains("不支持"))
            {
                return MapStudioFeedbackKind.Error;
            }

            if (message.Contains("需要") || message.Contains("未保存") || message.Contains("请先") || message.Contains("不再是") || message.Contains("不会写入") || message.Contains("不可直接编辑"))
            {
                return MapStudioFeedbackKind.Warning;
            }

            if (message.StartsWith("已", StringComparison.Ordinal) || message.Contains("保存后生效") || message.Contains("切换为"))
            {
                return MapStudioFeedbackKind.Success;
            }

            return MapStudioFeedbackKind.Info;
        }

        private static string GetFeedbackLabel(MapStudioFeedbackKind kind)
        {
            switch (kind)
            {
                case MapStudioFeedbackKind.Success:
                    return "完成";
                case MapStudioFeedbackKind.Warning:
                    return "注意";
                case MapStudioFeedbackKind.Error:
                    return "错误";
                default:
                    return "提示";
            }
        }

        private static Color GetFeedbackColor(MapStudioFeedbackKind kind)
        {
            switch (kind)
            {
                case MapStudioFeedbackKind.Success:
                    return new Color(0.26f, 0.65f, 0.42f);
                case MapStudioFeedbackKind.Warning:
                    return new Color(0.94f, 0.62f, 0.18f);
                case MapStudioFeedbackKind.Error:
                    return new Color(0.78f, 0.28f, 0.22f);
                default:
                    return new Color(0.24f, 0.47f, 0.72f);
            }
        }

        private FossickGeneratedMine BuildPreviewMine()
        {
            try
            {
                var config = BuildMineInstanceConfig();
                if (config == null)
                {
                    return new FossickGeneratedMine();
                }

                return FossickMineLayoutBuilder.Build(config, mineInstanceSeed, minePreviewRows);
            }
            catch (Exception)
            {
                return new FossickGeneratedMine();
            }
        }

        private FossickMapConfig BuildMineInstanceConfig()
        {
            if (!mineInstanceGenerated || mineInstanceSourceConfig == null)
            {
                return null;
            }

            var config = CloneMapConfig(mineInstanceSourceConfig);
            if (config == null)
            {
                return null;
            }

            if (config.generation == null)
            {
                config.generation = new FossickGenerationConfig();
            }

            return config;
        }

        private void DrawMineViewportOverlay(RectTransform parent, int width, int visibleHeight)
        {
            var rect = CreateRect("Visible Window Overlay", parent);
            SetTopLeft(rect, GridLabelWidth, 0f, width * MineCellSize, visibleHeight * MineCellSize);
            var image = AddImage(rect.gameObject, new Color(0.1f, 0.32f, 0.5f, 0.18f));
            image.raycastTarget = false;
        }

        private void DrawMinePreviewRow(RectTransform parent, IReadOnlyList<FossickCellConfig[]> mineCellRows, FossickGeneratedMineRow row, int displayY)
        {
            if (row == null || row.cells == null)
            {
                return;
            }

            for (var x = 0; x < row.cells.Length; x++)
            {
                DrawMinePreviewCell(parent, mineCellRows, row, x, displayY);
            }

            var rowLabelRect = CreateRect($"Mine Row Label {row.rowIndex}", parent);
            SetTopLeft(rowLabelRect, 0f, displayY * MineCellSize, GridLabelWidth - 4f, MineCellSize);
            var rowBackground = AddImage(rowLabelRect.gameObject, GetRowBarColor(row.rowIndex));
            rowBackground.raycastTarget = false;

            var rowTextRect = CreateRect("Row Number", rowLabelRect);
            Stretch(rowTextRect);
            var rowLabel = rowTextRect.gameObject.AddComponent<Text>();
            rowLabel.text = row.rowIndex.ToString("000");
            rowLabel.font = font;
            rowLabel.fontSize = 15;
            rowLabel.color = TextColor;
            rowLabel.alignment = TextAnchor.MiddleCenter;
            rowLabel.raycastTarget = false;

            if (row.localRow == 0)
            {
                var boundary = CreateRect($"Fragment Boundary {row.rowIndex}", parent);
                SetTopLeft(boundary, GridLabelWidth, displayY * MineCellSize, row.cells.Length * MineCellSize, 3f);
                AddImage(boundary.gameObject, row.fragment.insertedAsReward ? new Color(0.95f, 0.68f, 0.18f, 0.95f) : new Color(0.3f, 0.45f, 0.7f, 0.95f)).raycastTarget = false;
            }
        }

        private void DrawMineRowOverlay(RectTransform parent, string name, int displayY, int width, Color color)
        {
            var overlay = CreateRect(name, parent);
            SetTopLeft(overlay, GridLabelWidth, displayY * MineCellSize, width * MineCellSize, MineCellSize);
            AddImage(overlay.gameObject, color).raycastTarget = false;
        }

        private void DrawMinePreviewCell(RectTransform parent, IReadOnlyList<FossickCellConfig[]> mineCellRows, FossickGeneratedMineRow row, int x, int displayY)
        {
            var cell = row.cells[x];
            var rect = CreateRect($"Mine Cell {x},{displayY}", parent);
            SetTopLeft(rect, GridLabelWidth + x * MineCellSize, displayY * MineCellSize, MineCellSize, MineCellSize);

            var image = AddImage(rect.gameObject, GetCellInteractionColor(cell));
            image.raycastTarget = true;

            var layerRoot = CreateRect("Dynamic Cell Layers", rect);
            Stretch(layerRoot);
            DrawCellLayerPreview(layerRoot, cell, MineCellSize, false);

            var textRect = CreateRect("Mine Cell Label", rect);
            Stretch(textRect);
            var text = textRect.gameObject.AddComponent<Text>();
            text.text = GetMiniCellLabel(cell);
            text.font = font;
            text.fontSize = 14;
            text.fontStyle = FontStyle.Bold;
            text.color = TextColor;
            text.alignment = TextAnchor.MiddleCenter;
            text.raycastTarget = false;

            AddMineCellInspectEvents(rect.gameObject, row, x);
        }

        private void DrawTemplateGridEditor(RectTransform panel, FossickFragmentConfig fragment, float left, float width, float top)
        {
            var gridWidth = GridLabelWidth + fragment.width * MineCellSize;
            var viewportHeight = Mathf.Min(fragment.height, controller.CurrentConfig.visibleHeight) * MineCellSize;
            var scrollViewWidth = gridWidth + 28f;
            var stage = CreateRect("Template Stage", panel);
            SetTopLeft(stage, left, top, width, viewportHeight + 40f);

            var scrollView = CreateRect("Template Scroll View", stage);
            SetTopLeft(scrollView, Mathf.Max(0f, (width - scrollViewWidth) * 0.5f), 20f, scrollViewWidth, viewportHeight);
            AddImage(scrollView.gameObject, new Color(0.08f, 0.1f, 0.1f));

            var viewport = CreateRect("Template Viewport", scrollView);
            SetTopLeft(viewport, 0f, 0f, gridWidth, viewportHeight);
            var viewportImage = AddImage(viewport.gameObject, GetTerrainColor(FossickTerrainType.Empty));
            viewportImage.raycastTarget = true;
            var mask = viewport.gameObject.AddComponent<Mask>();
            mask.showMaskGraphic = true;

            var content = CreateRect("Template Content", viewport);
            SetTopLeft(content, 0f, 0f, gridWidth, fragment.height * MineCellSize);

            var rows = BuildConfigRows(fragment);
            var rewardBackgroundRoot = CreateRect("Template Reward Background Regions", content);
            SetTopLeft(rewardBackgroundRoot, GridLabelWidth, 0f, fragment.width * MineCellSize, fragment.height * MineCellSize);
            DrawRewardBackgroundRegions(rewardBackgroundRoot, rows, fragment.width, fragment.height, MineCellSize);

            var terrainRoot = CreateRect("Template Terrain Visuals", content);
            SetTopLeft(terrainRoot, GridLabelWidth, 0f, fragment.width * MineCellSize, fragment.height * MineCellSize);
            terrainRoot.gameObject.AddComponent<RectMask2D>();
            DrawTerrainSmoothGrid(terrainRoot, rows, fragment.width, fragment.height, MineCellSize);
            DrawSingleTerrainSprites(terrainRoot, rows, fragment.width, fragment.height, MineCellSize);

            for (var y = 0; y < fragment.height; y++)
            {
                DrawTemplateGridRow(content, fragment, y);
            }

            var scrollbar = CreateVerticalScrollbar(scrollView, gridWidth + 8f, 0f, 16f, viewportHeight);
            var scrollRect = scrollView.gameObject.AddComponent<ScrollRect>();
            scrollRect.viewport = viewport;
            scrollRect.content = content;
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            scrollRect.scrollSensitivity = MineCellSize;
            scrollRect.verticalScrollbar = scrollbar;
            scrollRect.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.Permanent;
            scrollRect.verticalNormalizedPosition = mineScrollNormalizedPosition;
            scrollRect.onValueChanged.AddListener(position =>
            {
                mineScrollNormalizedPosition = position.y;
            });
        }

        private void DrawTemplateGridRow(RectTransform parent, FossickFragmentConfig fragment, int y)
        {
            var rowLabelRect = CreateRect($"Template Row Label {y}", parent);
            SetTopLeft(rowLabelRect, 0f, y * MineCellSize, GridLabelWidth - 4f, MineCellSize);
            var rowLabel = rowLabelRect.gameObject.AddComponent<Text>();
            rowLabel.text = y.ToString("000");
            rowLabel.font = font;
            rowLabel.fontSize = 15;
            rowLabel.color = TextColor;
            rowLabel.alignment = TextAnchor.MiddleLeft;

            for (var x = 0; x < fragment.width; x++)
            {
                DrawTemplateGridCell(parent, fragment, x, y);
            }
        }

        private void DrawTemplateGridCell(RectTransform parent, FossickFragmentConfig fragment, int x, int y)
        {
            var cell = FindOrCreateCell(fragment, x, y);
            var rect = CreateRect($"Template Cell {x},{y}", parent);
            SetTopLeft(rect, GridLabelWidth + x * MineCellSize, y * MineCellSize, MineCellSize, MineCellSize);

            var image = AddImage(rect.gameObject, GetCellInteractionColor(cell));
            image.raycastTarget = true;

            var layerRoot = CreateRect("Dynamic Cell Layers", rect);
            Stretch(layerRoot);
            DrawCellLayerPreview(layerRoot, cell, MineCellSize, false);

            var textRect = CreateRect("Template Cell Label", rect);
            Stretch(textRect);
            var text = textRect.gameObject.AddComponent<Text>();
            text.text = GetMiniCellLabel(cell);
            text.font = font;
            text.fontSize = 14;
            text.fontStyle = FontStyle.Bold;
            text.color = TextColor;
            text.alignment = TextAnchor.MiddleCenter;
            text.raycastTarget = false;

            RegisterCellSelectionRect(GetTemplateSelectionKey(fragment.id, x, y), rect);

            AddTemplateCellPaintEvents(rect.gameObject, fragment, cell, image, text, layerRoot);
        }

        private static FossickCellConfig[][] BuildConfigRows(FossickGeneratedMine mine)
        {
            if (mine == null || mine.rows == null)
            {
                return new FossickCellConfig[0][];
            }

            var rows = new FossickCellConfig[mine.rows.Count][];
            for (var y = 0; y < mine.rows.Count; y++)
            {
                var source = mine.rows[y];
                rows[y] = source == null || source.cells == null ? new FossickCellConfig[0] : source.cells;
            }

            return rows;
        }

        private static FossickCellConfig[][] BuildConfigRows(FossickFragmentConfig fragment)
        {
            if (fragment == null || fragment.width <= 0 || fragment.height <= 0)
            {
                return new FossickCellConfig[0][];
            }

            var rows = new FossickCellConfig[fragment.height][];
            for (var y = 0; y < fragment.height; y++)
            {
                rows[y] = new FossickCellConfig[fragment.width];
            }

            if (fragment.cells != null)
            {
                for (var i = 0; i < fragment.cells.Count; i++)
                {
                    var cell = fragment.cells[i];
                    if (cell != null && cell.x >= 0 && cell.x < fragment.width && cell.y >= 0 && cell.y < fragment.height)
                    {
                        rows[cell.y][cell.x] = cell;
                    }
                }
            }

            return rows;
        }

        private void AddTemplateCellPaintEvents(GameObject target, FossickFragmentConfig fragment, FossickCellConfig cell, Image image, Text text, RectTransform layerRoot)
        {
            var trigger = target.AddComponent<EventTrigger>();
            AddEventTriggerEntry(trigger, EventTriggerType.PointerDown, _ =>
            {
                isDragPainting = true;
                BeginPaintSelection();
                SelectTemplateCell(fragment, cell, target.transform);
                if (!PaintTemplateCell(fragment, cell, image, text, layerRoot))
                {
                    isDragPainting = false;
                }
            });
            AddEventTriggerEntry(trigger, EventTriggerType.PointerEnter, _ =>
            {
                if (isDragPainting && Input.GetMouseButton(0))
                {
                    SelectTemplateCell(fragment, cell, target.transform);
                    PaintTemplateCell(fragment, cell, image, text, layerRoot);
                }
            });
            AddEventTriggerEntry(trigger, EventTriggerType.PointerUp, _ =>
            {
                FinishDragPainting();
            });
        }

        private void AddMineCellInspectEvents(GameObject target, FossickGeneratedMineRow row, int x)
        {
            var trigger = target.AddComponent<EventTrigger>();
            AddEventTriggerEntry(trigger, EventTriggerType.PointerDown, _ =>
            {
                selectedMineSequenceIndex = row == null || row.fragment == null ? -1 : row.fragment.sequenceIndex;
                var fragmentId = row == null || row.fragment == null ? 0 : row.fragment.fragmentId;
                editNotice = row == null
                    ? "未选中有效预览格。"
                    : $"当前格 ({x},{row.rowIndex:000}) 来自碎片 {fragmentId}，预览内容不可直接编辑。";
                Build();
            });
        }

        private static void AddEventTriggerEntry(EventTrigger trigger, EventTriggerType eventType, Action<BaseEventData> callback)
        {
            var entry = new EventTrigger.Entry
            {
                eventID = eventType
            };
            entry.callback.AddListener(data => callback(data));
            trigger.triggers.Add(entry);
        }

        private void SelectTemplateCell(FossickFragmentConfig fragment, FossickCellConfig cell, Transform target)
        {
            if (fragment == null || cell == null)
            {
                return;
            }

            AddPaintSelection(GetTemplateSelectionKey(fragment.id, cell.x, cell.y), target);
        }

        private void BeginPaintSelection()
        {
            selectedPaintCells.Clear();
            ClearLiveSelectionHighlights();
        }

        private void AddPaintSelection(string key, Transform target)
        {
            if (string.IsNullOrEmpty(key) || target == null)
            {
                return;
            }

            if (target is RectTransform rect)
            {
                selectedPaintCellRects[key] = rect;
            }

            selectedPaintCells.Add(key);
            RedrawLiveSelectionHighlights();
        }

        private void RegisterCellSelectionRect(string key, RectTransform rect)
        {
            if (string.IsNullOrEmpty(key) || rect == null)
            {
                return;
            }

            selectedPaintCellRects[key] = rect;

            if (selectedPaintCells.Contains(key))
            {
                selectedPaintCellHighlights[key] = DrawSelectionHighlight(rect, key);
            }
        }

        private void RedrawLiveSelectionHighlights()
        {
            ClearLiveSelectionHighlights();

            foreach (var key in selectedPaintCells)
            {
                if (selectedPaintCellRects.TryGetValue(key, out var rect) && rect != null)
                {
                    selectedPaintCellHighlights[key] = DrawSelectionHighlight(rect, key);
                }
            }
        }

        private void ClearLiveSelectionHighlights()
        {
            foreach (var item in selectedPaintCellHighlights)
            {
                DestroySelectionObject(item.Value);
            }

            selectedPaintCellHighlights.Clear();
        }

        private GameObject DrawSelectionHighlight(RectTransform parent, string key)
        {
            var rootRect = CreateRect("Selected Cell Highlight", parent);
            Stretch(rootRect);
            rootRect.SetAsLastSibling();

            if (ShouldDrawSelectionEdge(key, 0, -1))
            {
                AddSelectionEdge(rootRect, "Top", 0f, 0f, MineCellSize, 4f);
            }

            if (ShouldDrawSelectionEdge(key, 0, 1))
            {
                AddSelectionEdge(rootRect, "Bottom", 0f, MineCellSize - 4f, MineCellSize, 4f);
            }

            if (ShouldDrawSelectionEdge(key, -1, 0))
            {
                AddSelectionEdge(rootRect, "Left", 0f, 0f, 4f, MineCellSize);
            }

            if (ShouldDrawSelectionEdge(key, 1, 0))
            {
                AddSelectionEdge(rootRect, "Right", MineCellSize - 4f, 0f, 4f, MineCellSize);
            }

            return rootRect.gameObject;
        }

        private bool ShouldDrawSelectionEdge(string key, int dx, int dy)
        {
            return !TryGetNeighborSelectionKey(key, dx, dy, out var neighborKey) || !selectedPaintCells.Contains(neighborKey);
        }

        private static bool TryGetNeighborSelectionKey(string key, int dx, int dy, out string neighborKey)
        {
            neighborKey = null;
            if (string.IsNullOrEmpty(key))
            {
                return false;
            }

            var parts = key.Split(':');
            if (parts.Length == 4 && parts[0] == "t")
            {
                if (int.TryParse(parts[1], out var id) && int.TryParse(parts[2], out var x) && int.TryParse(parts[3], out var y))
                {
                    neighborKey = GetTemplateSelectionKey(id, x + dx, y + dy);
                    return true;
                }
            }

            return false;
        }

        private static string GetTemplateSelectionKey(int fragmentId, int x, int y)
        {
            return $"t:{fragmentId}:{x}:{y}";
        }

        private static void DestroySelectionObject(GameObject go)
        {
            if (go == null)
            {
                return;
            }

            go.SetActive(false);
            if (Application.isPlaying)
            {
                Destroy(go);
            }
            else
            {
                DestroyImmediate(go);
            }
        }

        private void AddSelectionEdge(RectTransform parent, string name, float x, float y, float width, float height)
        {
            var rect = CreateRect($"Selected Cell Edge {name}", parent);
            SetTopLeft(rect, x, y, width, height);
            AddImage(rect.gameObject, new Color(1f, 0.78f, 0.14f, 0.96f)).raycastTarget = false;
        }

        private void DrawCellLayerPreview(RectTransform parent, FossickCellConfig cell, float size, bool compact)
        {
            var reward = GetReward(cell);
            if (reward != null && reward.type != FossickElementType.None)
            {
                var rect = CreateRect("Reward Layer", parent);
                var inset = size * 0.2f;
                SetTopLeft(rect, inset, inset, size - inset * 2f, size - inset * 2f);
                var sprite = cell.terrain == FossickTerrainType.Empty
                    ? FossickArtLibrary.GetRewardSprite(reward)
                    : FossickArtLibrary.GetTerrainAttachmentSprite(reward, cell.terrain);
                var image = AddImage(rect.gameObject, sprite == null ? GetRewardColor(reward.type) : Color.white);
                image.raycastTarget = false;
                if (sprite != null)
                {
                    image.sprite = sprite;
                    image.preserveAspect = true;
                }
            }

            if (HasDecoration(cell))
            {
                var rect = CreateRect("Decoration Layer", parent);
                var inset = size * 0.08f;
                SetTopLeft(rect, size - size * 0.35f - inset, inset, size * 0.28f, size * 0.28f);
                AddImage(rect.gameObject, new Color(0.18f, 0.55f, 0.24f, 0.85f)).raycastTarget = false;
            }

            if (showFogInEditor && cell.fog == FossickFogType.Covered)
            {
                var rect = CreateRect("Fog Layer", parent);
                SetTopLeft(rect, 0f, 0f, size, size);
                AddImage(rect.gameObject, new Color(0f, 0f, 0f, compact ? 0.22f : 0.28f)).raycastTarget = false;
            }
        }

        private void DrawRewardBackgroundRegions(RectTransform parent, IReadOnlyList<FossickCellConfig[]> rows, int width, int height, float cellSize)
        {
            if (parent == null || rows == null || width <= 0 || height <= 0)
            {
                return;
            }

            var regions = BuildRewardBackgroundRegions(rows, width, height);
            for (var i = 0; i < regions.Count; i++)
            {
                var region = regions[i];
                var rect = CreateRect($"Reward Background Region {region.id}", parent);
                var left = region.startX * cellSize;
                var top = region.startY * cellSize;
                var regionWidth = (region.endX - region.startX + 1) * cellSize;
                var regionHeight = (region.endY - region.startY + 1) * cellSize;
                SetTopLeft(rect, left, top, regionWidth, regionHeight);

                var sprite = FossickArtLibrary.GetBackgroundSprite(region.id);
                var image = AddImage(rect.gameObject, sprite == null ? new Color(0.8f, 0.55f, 0.12f, 0.28f) : Color.white);
                image.raycastTarget = false;
                if (sprite != null)
                {
                    image.sprite = sprite;
                    image.type = Image.Type.Simple;
                    image.preserveAspect = false;
                }
            }
        }

        private static List<RewardBackgroundRegion> BuildRewardBackgroundRegions(IReadOnlyList<FossickCellConfig[]> rows, int width, int height)
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

        private static List<RewardBackgroundRegion> BuildFixedRewardBackgroundRegions(IReadOnlyList<FossickCellConfig[]> rows, int width, int height)
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

                    var id = GetRewardBackgroundId(rows, x, y);
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
                    width = 7;
                    height = 2;
                    return true;
                default:
                    return false;
            }
        }

        private static bool HasRewardBackgroundArea(IReadOnlyList<FossickCellConfig[]> rows, int startX, int startY, int width, int height, string id)
        {
            for (var y = startY; y < startY + height; y++)
            {
                for (var x = startX; x < startX + width; x++)
                {
                    if (GetRewardBackgroundId(rows, x, y) != id)
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        private static List<RewardBackgroundRegion> CollectRewardBackgroundSpans(IReadOnlyList<FossickCellConfig[]> rows, int width, int y, bool[,] covered)
        {
            var spans = new List<RewardBackgroundRegion>();
            var x = 0;
            while (x < width)
            {
                if (covered != null && y >= 0 && y < covered.GetLength(0) && x >= 0 && x < covered.GetLength(1) && covered[y, x])
                {
                    x++;
                    continue;
                }

                var id = GetRewardBackgroundId(rows, x, y);
                if (string.IsNullOrEmpty(id) || TryGetRewardBackgroundSize(id, out _, out _))
                {
                    x++;
                    continue;
                }

                var startX = x;
                while (x + 1 < width
                    && (covered == null || y < 0 || y >= covered.GetLength(0) || x + 1 < 0 || x + 1 >= covered.GetLength(1) || !covered[y, x + 1])
                    && GetRewardBackgroundId(rows, x + 1, y) == id)
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

        private static string GetRewardBackgroundId(IReadOnlyList<FossickCellConfig[]> rows, int x, int y)
        {
            if (rows == null || y < 0 || y >= rows.Count)
            {
                return null;
            }

            var row = rows[y];
            if (row == null || x < 0 || x >= row.Length)
            {
                return null;
            }

            return row[x] == null ? null : row[x].rewardBackgroundId;
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

        private void DrawBrushModeTabs(RectTransform parent)
        {
            brushPaletteView.DrawBrushModeTabs(parent, CreateBrushPaletteState(), CreateBrushPaletteCallbacks());
        }

        private void DrawBrushPalette(RectTransform parent)
        {
            brushPaletteView.DrawBrushes(parent, CreateBrushPaletteState(), CreateBrushPaletteCallbacks());
        }

        private FossickBrushPaletteView.State CreateBrushPaletteState()
        {
            return new FossickBrushPaletteView.State
            {
                selectedBrushMode = selectedBrushMode,
                selectedTerrain = selectedTerrain,
                selectedRewardType = selectedRewardType,
                selectedRewardId = selectedRewardId,
                selectedRewardAmount = selectedRewardAmountOverride,
                selectedRewardBackgroundId = selectedRewardBackgroundId,
                selectedRewardBackgroundWidth = selectedRewardBackgroundWidth,
                selectedRewardBackgroundHeight = selectedRewardBackgroundHeight,
                selectedDecorationId = selectedDecorationId,
                selectedFog = selectedFog
            };
        }

        private FossickBrushPaletteView.Callbacks CreateBrushPaletteCallbacks()
        {
            return new FossickBrushPaletteView.Callbacks
            {
                selectBrushMode = SelectBrushMode,
                selectTerrain = SelectTerrainBrush,
                selectReward = SelectRewardBrush,
                selectRewardBackground = SelectRewardBackgroundBrush,
                selectDecoration = SelectDecorationBrush,
                selectFog = SelectFogBrush
            };
        }

        private void SelectBrushMode(FossickBrushMode mode)
        {
            selectedBrushMode = mode;
            EnsureBrushForMode(mode);
            ClearPendingPaint();
            editNotice = $"画笔模式已切换为 {FormatBrushMode(mode)}。";
            Build();
        }

        private void SelectTerrainBrush(FossickTerrainType terrain)
        {
            selectedBrushMode = FossickBrushMode.Terrain;
            selectedTerrain = terrain;
            ClearPendingPaint();
            editNotice = $"画笔已切换为 {FormatTerrain(terrain)}。";
            Build();
        }

        private void SelectRewardBrush(FossickElementType type, string id, string label, int amountOverride)
        {
            selectedRewardType = type;
            selectedRewardId = id;
            selectedRewardAmountOverride = amountOverride;
            ClearPendingPaint();
            editNotice = $"{FormatBrushMode(selectedBrushMode)}画笔已切换为 {label}。";
            Build();
        }

        private void SelectRewardBackgroundBrush(string id, string label, int width, int height)
        {
            selectedBrushMode = FossickBrushMode.RewardBackground;
            selectedRewardBackgroundId = id;
            selectedRewardBackgroundWidth = width;
            selectedRewardBackgroundHeight = height;
            ClearPendingPaint();
            editNotice = string.IsNullOrEmpty(id)
                ? "藏宝阁画笔已切换为清空。"
                : $"藏宝阁画笔已切换为 {label}，点击左上角格子放置区域。";
            Build();
        }

        private void SelectDecorationBrush(string id, string label)
        {
            selectedBrushMode = FossickBrushMode.Decoration;
            selectedDecorationId = id;
            ClearPendingPaint();
            editNotice = $"装饰画笔已切换为 {label}。";
            Build();
        }

        private void SelectFogBrush(FossickFogType fog, string label)
        {
            selectedBrushMode = FossickBrushMode.Fog;
            selectedFog = fog;
            ClearPendingPaint();
            editNotice = $"阴影画笔已切换为 {label}。";
            Build();
        }

        private void ToggleFogVisibility()
        {
            showFogInEditor = !showFogInEditor;
            ClearPendingPaint();
            editNotice = showFogInEditor ? "编辑器中已显示阴影；地图数据未改变。" : "编辑器中已隐藏阴影；地图数据未改变。";
            Build();
        }

        private void EnsureBrushForMode(FossickBrushMode mode)
        {
            if (mode == FossickBrushMode.Reward && selectedRewardType == FossickElementType.Item)
            {
                selectedRewardType = FossickElementType.Ore;
                selectedRewardId = "ore_copper";
                selectedRewardAmountOverride = 0;
            }
            else if (mode == FossickBrushMode.Tool && selectedRewardType != FossickElementType.Item && selectedRewardType != FossickElementType.None)
            {
                selectedRewardType = FossickElementType.Item;
                selectedRewardId = "pickaxe";
                selectedRewardAmountOverride = 0;
            }
            else if (mode == FossickBrushMode.RewardBackground && selectedRewardBackgroundWidth <= 0 && !string.IsNullOrEmpty(selectedRewardBackgroundId))
            {
                selectedRewardBackgroundId = TreasureRoomLargeId;
                selectedRewardBackgroundWidth = 7;
                selectedRewardBackgroundHeight = 2;
            }
        }

        private void DrawTerrainSmoothGrid(RectTransform parent, IReadOnlyList<FossickCellConfig[]> rows, int width, int height, float cellSize)
        {
            if (rows == null || width <= 0 || height <= 0)
            {
                return;
            }

            DrawTerrainSmoothGridLayer(parent, rows, width, height, cellSize, FossickTerrainType.Dirt);
            DrawTerrainSmoothGridLayer(parent, rows, width, height, cellSize, FossickTerrainType.Stone);
            DrawTerrainSmoothGridLayer(parent, rows, width, height, cellSize, FossickTerrainType.Unbreakable);
        }

        private void DrawSingleTerrainSprites(RectTransform parent, IReadOnlyList<FossickCellConfig[]> rows, int width, int height, float cellSize)
        {
            if (parent == null || rows == null || width <= 0 || height <= 0)
            {
                return;
            }

            for (var y = 0; y < height; y++)
            {
                var row = y < rows.Count ? rows[y] : null;
                if (row == null)
                {
                    continue;
                }

                for (var x = 0; x < width && x < row.Length; x++)
                {
                    var cell = row[x];
                    if (cell == null || cell.terrain == FossickTerrainType.Empty || FossickArtLibrary.HasAutoTileSprites(cell.terrain))
                    {
                        continue;
                    }

                    var sprite = FossickArtLibrary.GetTerrainSprite(cell.terrain);
                    if (sprite == null)
                    {
                        continue;
                    }

                    var rect = CreateRect($"{cell.terrain} Terrain Sprite {x},{y}", parent);
                    SetTopLeft(rect, x * cellSize, y * cellSize, cellSize, cellSize);
                    var image = AddImage(rect.gameObject, Color.white);
                    image.raycastTarget = false;
                    image.sprite = sprite;
                    image.preserveAspect = true;
                }
            }
        }

        private void DrawTerrainSmoothGridLayer(RectTransform parent, IReadOnlyList<FossickCellConfig[]> rows, int width, int height, float cellSize, FossickTerrainType terrain)
        {
            if (!FossickArtLibrary.HasAutoTileSprites(terrain))
            {
                return;
            }

            for (var cornerY = 0; cornerY <= height; cornerY++)
            {
                for (var cornerX = 0; cornerX <= width; cornerX++)
                {
                    var assetIndex = FossickArtLibrary.ResolveConfigCornerAssetIndex(rows, cornerX, cornerY, terrain);
                    DrawTerrainSmoothAsset(parent, cornerX, cornerY, assetIndex, terrain, cellSize);
                }
            }
        }

        private void DrawTerrainSmoothAsset(RectTransform parent, int x, int y, int assetIndex, FossickTerrainType terrain, float cellSize)
        {
            var spriteIndex = ResolveTerrainVisualVariant(assetIndex, x, y, terrain);
            var sprite = FossickArtLibrary.GetAutoTileSprite(terrain, spriteIndex);
            if (spriteIndex <= 0 || sprite == null)
            {
                return;
            }

            var rect = CreateRect($"{terrain} Smooth Corner {x},{y}-{spriteIndex}", parent);
            SetTopLeft(
                rect,
                (x - 0.5f) * cellSize - SmoothTileOverlap * 0.5f,
                (y - 0.5f) * cellSize - SmoothTileOverlap * 0.5f,
                cellSize + SmoothTileOverlap,
                cellSize + SmoothTileOverlap);

            var image = AddImage(rect.gameObject, Color.white);
            image.raycastTarget = false;
            image.sprite = sprite;
            image.type = Image.Type.Simple;
            image.preserveAspect = false;
        }

        private int ResolveTerrainVisualVariant(int assetIndex, int x, int y, FossickTerrainType terrain)
        {
            return assetIndex;
        }

        private static Color GetCellInteractionColor(FossickCellConfig cell)
        {
            return new Color(1f, 1f, 1f, 0f);
        }

        private void AddCellClickArea(RectTransform parent, FossickFragmentConfig fragment, FossickCellConfig cell)
        {
            var rect = CreateRect($"Cell Click {cell.x},{cell.y}", parent);
            SetTopLeft(rect, cell.x * CellSize, cell.y * CellSize, CellSize, CellSize);

            var image = AddImage(rect.gameObject, new Color(0f, 0f, 0f, 0f));
            image.raycastTarget = true;

            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(() =>
            {
                ClearPendingPaint();
                editNotice = null;
                PaintCell(cell);
                Build();
            });

            AddCellDebugOverlay(rect, fragment, cell);
        }

        private void AddCellDebugOverlay(RectTransform parent, FossickFragmentConfig fragment, FossickCellConfig cell)
        {
            var label = GetCellLabel(cell);
            var text = AddText(parent, label, 12, FontStyle.Bold, new Vector2(CellSize, CellSize), TextAnchor.MiddleCenter);
            Stretch(text.GetComponent<RectTransform>());
            text.raycastTarget = false;
        }

        private void OpenTemplateLibrary()
        {
            if (editMode == MapStudioEditMode.Template && templateEditDirty)
            {
                ShowUnsavedTemplateDialog("打开模板库", "当前模板有未保存修改，请选择保存或丢弃。", OpenTemplateLibraryInternal);
                return;
            }

            OpenTemplateLibraryInternal();
        }

        private void OpenTemplateLibraryInternal()
        {
            templateLibraryOpen = true;
            generationRulesOpen = false;
            ClearPendingPaint();
            Build();
        }

        private void CloseTemplateLibrary()
        {
            templateLibraryOpen = false;
            editMode = MapStudioEditMode.MineInstance;
            ClearPendingPaint();
            Build();
        }

        private void OpenGenerationRules()
        {
            templateLibraryOpen = false;
            generationRulesOpen = true;
            generationRulesSnapshot = CloneGenerationConfig(EnsureGenerationConfig());
            generationRulesDirtySnapshot = generationRulesDirty;
            generationRulesEditDirty = false;
            ClearPendingPaint();
            Build();
        }

        private void CloseGenerationRules()
        {
            generationRulesOpen = false;
            Build();
        }

        private void ReturnFromGenerationRules()
        {
            if (!generationRulesEditDirty)
            {
                ExitGenerationRulesToPreview();
                return;
            }

            ShowOperationDialog(
                "返回地图预览前需要处理修改",
                "当前生成规则有未保存修改。返回前请选择保存或丢弃。",
                "保存",
                SaveGenerationRulesAndReturn,
                "丢弃",
                DiscardGenerationRulesChangesAndReturn,
                "取消",
                null);
        }

        private void ExitGenerationRulesToPreview()
        {
            generationRulesOpen = false;
            editMode = MapStudioEditMode.MineInstance;
            ClearPendingPaint();
            Build();
        }

        private void SaveGenerationRulesAndReturn()
        {
            controller.Validate();
            SaveProjectFiles();
            generationRulesSnapshot = CloneGenerationConfig(EnsureGenerationConfig());
            generationRulesDirtySnapshot = generationRulesDirty;
            generationRulesEditDirty = false;
            editNotice = "生成规则已保存，已返回地图预览。";
            ExitGenerationRulesToPreview();
        }

        private void DiscardGenerationRulesChangesAndReturn()
        {
            if (generationRulesSnapshot != null)
            {
                controller.CurrentConfig.generation = CloneGenerationConfig(generationRulesSnapshot);
            }

            generationRulesDirty = generationRulesDirtySnapshot;
            generationRulesEditDirty = false;
            controller.Validate();
            editNotice = "已放弃生成规则修改，已返回地图预览。";
            ExitGenerationRulesToPreview();
        }

        private void SaveGenerationRules()
        {
            controller.Validate();
            SaveProjectFiles();
            generationRulesSnapshot = CloneGenerationConfig(EnsureGenerationConfig());
            generationRulesDirtySnapshot = generationRulesDirty;
            generationRulesEditDirty = false;
            editNotice = "生成规则已保存。需要刷新矿井时，请点击“应用并更新预览”。";
            Build();
        }

        private void DiscardGenerationRulesChanges()
        {
            if (generationRulesSnapshot != null)
            {
                controller.CurrentConfig.generation = CloneGenerationConfig(generationRulesSnapshot);
            }

            generationRulesDirty = generationRulesDirtySnapshot;
            generationRulesEditDirty = false;
            controller.Validate();
            editNotice = "已放弃生成规则的未保存修改。";
            Build();
        }

        private void ApplyGenerationRulesAndUpdatePreview()
        {
            controller.Validate();
            GenerateMineInstance(true, "已按当前生成规则更新矿井预览。");
            generationRulesSnapshot = CloneGenerationConfig(EnsureGenerationConfig());
            generationRulesDirtySnapshot = generationRulesDirty;
            Build();
        }

        private void DrawTemplateLibraryPanel()
        {
            var panelLeft = LeftWidth + 24f;
            var panelRight = CenterWidth;
            var panelWidth = panelRight - panelLeft;
            var panel = CreatePanel("Template Library Panel", root, new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(panelLeft, 12f), new Vector2(panelRight, -HeaderHeight - 24f));

            var title = AddText(panel, "模板库", 20, FontStyle.Bold, new Vector2(panelWidth - 32f, 30f));
            SetTopLeft(title.GetComponent<RectTransform>(), 16f, 14f, panelWidth - 32f, 30f);
            DrawModeBadge(panel, panelWidth, 14f, 160f);
            var close = AddActionButton(panel, "返回地图预览", new Vector2(128f, 30f), CloseTemplateLibrary);
            SetTopLeft(close, panelWidth - 144f, 14f, 128f, 30f);

            var desc = AddText(panel, "这里管理可复用碎片模板；地图预览会按规则抽取这些模板，不在这里修改预览结果。", 14, FontStyle.Normal, new Vector2(panelWidth - 32f, 26f));
            SetTopLeft(desc.GetComponent<RectTransform>(), 16f, 54f, panelWidth - 32f, 26f);
            if (!string.IsNullOrEmpty(editNotice))
            {
                DrawFeedbackBanner(panel, 16f, 82f, panelWidth - 32f);
            }

            const float contentTop = 128f;
            var existingWidth = Mathf.Min(520f, panelWidth * 0.52f);
            var presetWidth = Mathf.Min(500f, panelWidth - existingWidth - 52f);

            var existingCard = CreateRect("Template Library Existing Card", panel);
            SetTopLeft(existingCard, 16f, contentTop, existingWidth, 552f);
            AddImage(existingCard.gameObject, new Color(0.1f, 0.13f, 0.15f));

            var presetCard = CreateRect("Template Library Preset Card", panel);
            SetTopLeft(presetCard, 36f + existingWidth, contentTop, presetWidth, 552f);
            AddImage(presetCard.gameObject, new Color(0.1f, 0.13f, 0.15f));

            var existingTitle = AddText(existingCard, "已有模板", 18, FontStyle.Bold, new Vector2(existingWidth - 32f, 28f));
            SetTopLeft(existingTitle.GetComponent<RectTransform>(), 16f, 16f, existingWidth - 32f, 28f);
            DrawTemplateLibraryFilters(existingCard);
            DrawExistingTemplateCards(existingCard, existingWidth);

            var presetTitle = AddText(presetCard, "新建预设", 18, FontStyle.Bold, new Vector2(presetWidth - 32f, 28f));
            SetTopLeft(presetTitle.GetComponent<RectTransform>(), 16f, 16f, presetWidth - 32f, 28f);
            DrawTemplatePresetCard(presetCard, 16f, 60f, "空白", CreateBlankFragmentPreview(), TemplatePresetType.Blank);
            DrawTemplatePresetCard(presetCard, 180f, 60f, "全土", CreateFilledRegularFragmentPreview(), TemplatePresetType.FilledRegular);
            DrawTemplatePresetCard(presetCard, 344f, 60f, "奖励房", CreateRewardRoomFragmentPreview(), TemplatePresetType.RewardRoom);

            var create = AddActionButton(presetCard, "创建模板", new Vector2(240f, 34f), CreateSelectedTemplatePreset, ButtonTone.Primary);
            SetTopLeft(create, 16f, 240f, 240f, 34f);
        }

        private void DrawTemplateLibraryFilters(RectTransform parent)
        {
            DrawTemplateLibraryFilterButton(parent, 16f, "全部", TemplateLibraryFilter.All);
            DrawTemplateLibraryFilterButton(parent, 98f, "新手", TemplateLibraryFilter.Tutorial);
            DrawTemplateLibraryFilterButton(parent, 180f, "常规", TemplateLibraryFilter.Regular);
            DrawTemplateLibraryFilterButton(parent, 262f, "奖励", TemplateLibraryFilter.Reward);
        }

        private void DrawTemplateLibraryFilterButton(RectTransform parent, float x, string label, TemplateLibraryFilter filter)
        {
            var button = AddButton(parent, label, new Vector2(72f, 30f), () =>
            {
                templateLibraryFilter = filter;
                Build();
            }, templateLibraryFilter == filter);
            SetTopLeft(button, x, 56f, 72f, 30f);
        }

        private void DrawExistingTemplateCards(RectTransform parent, float width)
        {
            var fragments = controller.CurrentConfig.fragments;
            if (fragments == null)
            {
                return;
            }

            var visibleIndex = 0;
            for (var i = 0; i < fragments.Count && visibleIndex < 6; i++)
            {
                var index = i;
                var fragment = fragments[i];
                if (fragment == null)
                {
                    continue;
                }

                if (!MatchesTemplateLibraryFilter(fragment))
                {
                    continue;
                }

                var col = visibleIndex % 2;
                var row = visibleIndex / 2;
                visibleIndex++;
                DrawTemplateCard(parent, 16f + col * 250f, 104f + row * 112f, fragment, selectedFragmentIndex == index, index);
            }

            if (visibleIndex == 0)
            {
                AddText(parent, "当前筛选下没有模板。", 13, FontStyle.Normal, new Vector2(420f, 24f));
            }

            var actionY = 456f;
            var edit = AddActionButton(parent, "编辑模板", new Vector2(150f, 34f), () =>
            {
                BeginTemplateEdit(selectedFragmentIndex, "已进入模板编辑。修改会先保存在临时副本中。");
            }, ButtonTone.Primary);
            SetTopLeft(edit, 16f, actionY, 150f, 34f);

            var copy = AddActionButton(parent, "复制模板", new Vector2(150f, 34f), () =>
            {
                templateLibraryOpen = false;
                CopySelectedFragment();
            });
            SetTopLeft(copy, 176f, actionY, 150f, 34f);

            var delete = AddActionButton(parent, "删除模板", new Vector2(150f, 34f), () =>
            {
                templateLibraryOpen = false;
                RequestDeleteSelectedFragment();
            }, ButtonTone.Danger);
            SetTopLeft(delete, 16f, actionY + 44f, 150f, 34f);
        }

        private bool MatchesTemplateLibraryFilter(FossickFragmentConfig fragment)
        {
            if (fragment == null || templateLibraryFilter == TemplateLibraryFilter.All)
            {
                return true;
            }

            switch (templateLibraryFilter)
            {
                case TemplateLibraryFilter.Tutorial:
                    return fragment.type == FossickFragmentType.Tutorial;
                case TemplateLibraryFilter.Regular:
                    return fragment.type == FossickFragmentType.Regular;
                case TemplateLibraryFilter.Reward:
                    return fragment.type == FossickFragmentType.Reward;
                default:
                    return true;
            }
        }

        private void DrawTemplateCard(RectTransform parent, float x, float y, FossickFragmentConfig fragment, bool selected, int fragmentIndex)
        {
            var card = CreatePanel($"Template Card {fragment.id}", parent, new Vector2(0f, 1f), new Vector2(0f, 1f), Vector2.zero, Vector2.zero);
            SetTopLeft(card, x, y, 226f, 96f);
            card.GetComponent<Image>().color = selected ? new Color(0.23f, 0.47f, 0.72f) : Panel;
            var cardButton = card.gameObject.AddComponent<Button>();
            cardButton.targetGraphic = card.GetComponent<Image>();
            cardButton.onClick.AddListener(() => SelectTemplateFromLibrary(fragmentIndex, fragment));

            var preview = CreateRect("Template Preview", card);
            SetTopLeft(preview, 12f, 12f, 112f, 72f);
            AddImage(preview.gameObject, new Color(0.08f, 0.1f, 0.1f));
            DrawFragmentThumbnail(preview, fragment, 112f, 72f);

            var label = AddText(card, $"{fragment.id}\n{FormatFragmentType(fragment.type)}{(fragment.type == FossickFragmentType.Regular ? " D" + fragment.difficulty : string.Empty)}", 13, FontStyle.Bold, new Vector2(82f, 48f), TextAnchor.MiddleLeft);
            SetTopLeft(label.GetComponent<RectTransform>(), 136f, 16f, 82f, 48f);
        }

        private void SelectTemplateFromLibrary(int fragmentIndex, FossickFragmentConfig fragment)
        {
            selectedFragmentIndex = Mathf.Clamp(fragmentIndex, 0, controller.CurrentConfig.fragments.Count - 1);
            templateEditDraft = null;
            templateEditSourceIndex = -1;
            templateEditDirty = false;
            templateUndoStack.Clear();
            templateRedoStack.Clear();
            ClearPendingPaint();
            editNotice = fragment == null ? "已选择模板。" : $"已选择模板 {fragment.id}。";
            Build();
        }

        private void DrawTemplatePresetCard(RectTransform parent, float x, float y, string title, FossickFragmentConfig previewFragment, TemplatePresetType preset)
        {
            var card = CreatePanel(title, parent, new Vector2(0f, 1f), new Vector2(0f, 1f), Vector2.zero, Vector2.zero);
            SetTopLeft(card, x, y, 136f, 142f);
            card.GetComponent<Image>().color = selectedTemplatePreset == preset ? new Color(0.23f, 0.47f, 0.72f) : Panel;
            var button = card.gameObject.AddComponent<Button>();
            button.targetGraphic = card.GetComponent<Image>();
            button.onClick.AddListener(() =>
            {
                selectedTemplatePreset = preset;
                Build();
            });

            var name = AddText(card, title, 15, FontStyle.Bold, new Vector2(112f, 24f), TextAnchor.MiddleCenter);
            SetTopLeft(name.GetComponent<RectTransform>(), 12f, 12f, 112f, 24f);

            var preview = CreateRect("Preset Preview", card);
            SetTopLeft(preview, 12f, 44f, 112f, 72f);
            AddImage(preview.gameObject, new Color(0.08f, 0.1f, 0.1f));
            DrawFragmentThumbnail(preview, previewFragment, 112f, 72f);
        }

        private void CreateSelectedTemplatePreset()
        {
            templateLibraryOpen = false;
            switch (selectedTemplatePreset)
            {
                case TemplatePresetType.FilledRegular:
                    AddFilledRegularFragment();
                    break;
                case TemplatePresetType.RewardRoom:
                    AddRewardFragment();
                    break;
                default:
                    AddBlankRegularFragment();
                    break;
            }
        }

        private void DrawFragmentThumbnail(RectTransform parent, FossickFragmentConfig fragment, float width, float height)
        {
            if (fragment == null || fragment.width <= 0 || fragment.height <= 0)
            {
                return;
            }

            var cell = Mathf.Min(width / fragment.width, height / fragment.height);
            var left = (width - fragment.width * cell) * 0.5f;
            var top = (height - fragment.height * cell) * 0.5f;
            for (var y = 0; y < fragment.height; y++)
            {
                for (var x = 0; x < fragment.width; x++)
                {
                    var source = FindOrCreateCell(fragment, x, y);
                    var rect = CreateRect($"Thumbnail {x},{y}", parent);
                    SetTopLeft(rect, left + x * cell, top + y * cell, Mathf.Max(1f, cell - 1f), Mathf.Max(1f, cell - 1f));
                    AddImage(rect.gameObject, GetCellBaseColor(source)).raycastTarget = false;
                    DrawCellLayerPreview(rect, source, Mathf.Max(1f, cell - 1f), true);
                }
            }
        }

        private void AddBlankRegularFragment()
        {
            var config = controller.CurrentConfig;
            var nextId = GetNextFragmentId(2000);
            config.fragments.Add(CreateBlankFragment(nextId));
            selectedFragmentIndex = config.fragments.Count - 1;
            editMode = MapStudioEditMode.Template;
            ClearPendingPaint();
            editNotice = $"已从模板库创建空白常规模板 {nextId}。它会进入难度 1 常规池，是否出现在矿井由生成规则决定。";
            MarkTemplateLibraryChanged();
            controller.Validate();
            Build();
        }

        private FossickFragmentConfig CreateBlankFragmentPreview()
        {
            return CreateBlankFragment(-1);
        }

        private FossickFragmentConfig CreateFilledRegularFragmentPreview()
        {
            return CreateFilledRegularFragment(-1);
        }

        private FossickFragmentConfig CreateRewardRoomFragmentPreview()
        {
            return CreateRewardRoomFragment(-1);
        }

        private void AddFilledRegularFragment()
        {
            var config = controller.CurrentConfig;
            var nextId = GetNextFragmentId(2000);
            config.fragments.Add(CreateFilledRegularFragment(nextId));
            selectedFragmentIndex = config.fragments.Count - 1;
            editMode = MapStudioEditMode.Template;
            ClearPendingPaint();
            editNotice = $"已从模板库创建全土常规模板 {nextId}，它会进入难度 1 常规池；是否出现在当前矿井取决于种子和抽取规则。";
            MarkTemplateLibraryChanged();
            controller.Validate();
            Build();
        }

        private void AddRewardFragment()
        {
            var config = controller.CurrentConfig;
            var nextId = GetNextFragmentId(3000);
            config.fragments.Add(CreateRewardRoomFragment(nextId));
            selectedFragmentIndex = config.fragments.Count - 1;
            editMode = MapStudioEditMode.Template;
            ClearPendingPaint();
            editNotice = $"已从模板库创建奖励房模板 {nextId}，它会进入奖励插入池；每隔配置数量的常规碎片后可能被插入。";
            MarkTemplateLibraryChanged();
            controller.Validate();
            Build();
        }

        private void CopySelectedFragment()
        {
            var fragment = GetSelectedFragment();
            if (fragment == null)
            {
                return;
            }

            var copy = CreateBlankFragment(GetNextFragmentId(fragment.id + 10000));
            copy.type = fragment.type;
            copy.difficulty = fragment.difficulty;
            copy.weight = fragment.weight;
            copy.height = fragment.height;
            copy.cells.Clear();
            for (var i = 0; i < fragment.cells.Count; i++)
            {
                var cell = fragment.cells[i];
                    copy.cells.Add(new FossickCellConfig
                    {
                        x = cell.x,
                        y = cell.y,
                        backgroundId = cell.backgroundId,
                        rewardBackgroundId = cell.rewardBackgroundId,
                        terrain = cell.terrain,
                        hp = cell.hp,
                        reward = cell.reward,
                        decorations = cell.decorations == null ? new List<string>() : new List<string>(cell.decorations),
                        fog = cell.fog
                    });
            }

            controller.CurrentConfig.fragments.Add(copy);
            selectedFragmentIndex = controller.CurrentConfig.fragments.Count - 1;
            editMode = MapStudioEditMode.Template;
            ClearPendingPaint();
            editNotice = $"已复制为新模板 {copy.id}，用途：{FormatTemplatePool(copy)}。";
            MarkTemplateLibraryChanged();
            controller.Validate();
            Build();
        }

        private void DeleteSelectedFragment()
        {
            var fragments = controller.CurrentConfig.fragments;
            if (fragments == null || fragments.Count <= 1)
            {
                return;
            }

            fragments.RemoveAt(selectedFragmentIndex);
            selectedFragmentIndex = Mathf.Clamp(selectedFragmentIndex, 0, fragments.Count - 1);
            ClearPendingPaint();
            editNotice = "已删除选中模板。";
            MarkTemplateLibraryChanged();
            controller.Validate();
            Build();
        }

        private void RequestDeleteSelectedFragment()
        {
            var selected = GetSelectedFragment();
            if (selected == null)
            {
                return;
            }

            ShowOperationDialog(
                "删除模板",
                "删除后该模板不会再参与后续生成。已生成的预览可重新生成。",
                "删除",
                DeleteSelectedFragment,
                cancelLabel: "取消");
        }

        private void ExportJson()
        {
            SaveProjectFiles();
            ClearPendingPaint();
            editNotice = exportStatus;
            Build();
        }

        private void OpenDataFolder()
        {
            SaveProjectFiles();
            ClearPendingPaint();

            var folder = FossickMapProjectFileService.GetEditableMapsFolder();
            Directory.CreateDirectory(folder);
            Application.OpenURL(new Uri(folder).AbsoluteUri);
            editNotice = $"已打开数据目录：{folder}";
            Build();
        }

        private void SaveProjectFiles()
        {
            var project = FossickMapProjectConfig.FromRuntimeConfig(controller.CurrentConfig, mineInstanceSeed);
            FossickMapProjectFileService.SaveEditableProject(project);
            exportStatus = $"已保存到：{FossickMapProjectFileService.GetEditableMapsFolder()}";

#if UNITY_EDITOR
            AssetDatabase.Refresh();
#endif
        }

        private void PlayPreviewScene()
        {
            if (templateEditDirty && !SaveTemplateEditInternal(false))
            {
                ShowOperationDialog(
                    "无法试玩",
                    "当前模板修改无法保存，请先检查模板状态。",
                    "知道了",
                    Build,
                    cancelLabel: null);
                return;
            }

            controller.Validate();
            if (controller.LastValidation != null && controller.LastValidation.HasErrors)
            {
                ShowOperationDialog(
                    "校验未通过",
                    "当前模板或生成规则还有错误。请先修复校验问题，再进入试玩。",
                    "知道了",
                    Build,
                    cancelLabel: null);
                return;
            }

            SaveProjectFiles();
            ClearPendingPaint();
            editNotice = "已保存当前配置，正在进入 Preview 场景试玩。";

#if UNITY_EDITOR
            if (Application.isPlaying)
            {
                SceneManager.LoadScene(PreviewSceneName);
                return;
            }

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                Build();
                return;
            }

            EditorSceneManager.OpenScene(PreviewScenePath, OpenSceneMode.Single);
            EditorApplication.isPlaying = true;
#else
            SceneManager.LoadScene(PreviewSceneName);
#endif
        }

        private FossickFragmentConfig GetSelectedFragment()
        {
            var fragments = controller.CurrentConfig.fragments;
            if (fragments == null || fragments.Count == 0)
            {
                return null;
            }

            selectedFragmentIndex = Mathf.Clamp(selectedFragmentIndex, 0, fragments.Count - 1);
            return fragments[selectedFragmentIndex];
        }

        private FossickFragmentConfig CreateBlankFragment(int id)
        {
            var config = controller.CurrentConfig;
            var fragment = new FossickFragmentConfig
            {
                id = id,
                type = FossickFragmentType.Regular,
                difficulty = 1,
                width = config.boardWidth,
                height = config.visibleHeight
            };

            ResizeFragment(fragment, fragment.height);
            return fragment;
        }

        private FossickFragmentConfig CreateFilledRegularFragment(int id)
        {
            var fragment = CreateBlankFragment(id);
            fragment.type = FossickFragmentType.Regular;
            fragment.difficulty = 1;
            FillFragmentTerrain(fragment, FossickTerrainType.Dirt);
            return fragment;
        }

        private FossickFragmentConfig CreateRewardRoomFragment(int id)
        {
            var config = controller.CurrentConfig;
            var fragment = new FossickFragmentConfig
            {
                id = id,
                type = FossickFragmentType.Reward,
                difficulty = 0,
                width = config.boardWidth,
                height = 2
            };

            ResizeFragment(fragment, fragment.height);
            for (var i = 0; i < fragment.cells.Count; i++)
            {
                var cell = fragment.cells[i];
                cell.rewardBackgroundId = TreasureRoomLargeId;
                cell.fog = FossickFogType.None;
                if (cell.y == fragment.height / 2 && cell.x > 0 && cell.x < fragment.width - 1)
                {
                    cell.reward = CreateReward(FossickElementType.Coin);
                }
            }

            return fragment;
        }

        private void FillFragmentTerrain(FossickFragmentConfig fragment, FossickTerrainType terrain)
        {
            if (fragment == null)
            {
                return;
            }

            for (var y = 0; y < fragment.height; y++)
            {
                for (var x = 0; x < fragment.width; x++)
                {
                    var cell = FindOrCreateCell(fragment, x, y);
                    cell.terrain = terrain;
                    cell.hp = terrain == FossickTerrainType.Stone ? 2 : terrain == FossickTerrainType.Dirt ? 1 : 0;
                    cell.fog = terrain == FossickTerrainType.Empty ? FossickFogType.None : FossickFogType.Covered;
                }
            }
        }

        private int GetNextFragmentId(int start)
        {
            var id = Mathf.Max(1, start);
            while (FragmentIdExists(id))
            {
                id++;
            }

            return id;
        }

        private bool FragmentIdExists(int id)
        {
            var fragments = controller.CurrentConfig.fragments;
            if (fragments == null)
            {
                return false;
            }

            for (var i = 0; i < fragments.Count; i++)
            {
                if (fragments[i] != null && fragments[i].id == id)
                {
                    return true;
                }
            }

            return false;
        }

        private void ResizeFragment(FossickFragmentConfig fragment, int newHeight)
        {
            fragment.height = newHeight;
            for (var y = 0; y < fragment.height; y++)
            {
                for (var x = 0; x < fragment.width; x++)
                {
                    FindOrCreateCell(fragment, x, y);
                }
            }

            fragment.cells.RemoveAll(cell => cell == null || cell.y >= fragment.height || cell.x >= fragment.width);
            controller.Validate();
        }

        private bool TryPaintRewardBackgroundArea(FossickFragmentConfig fragment, int startX, int startY)
        {
            if (!IsRewardBackgroundAreaBrush())
            {
                return false;
            }

            if (fragment == null)
            {
                return true;
            }

            if (!CanPlaceRewardBackgroundArea(startX, startY, fragment.width, fragment.height))
            {
                editNotice = $"藏宝阁 {selectedRewardBackgroundWidth}x{selectedRewardBackgroundHeight} 超出当前模板范围，请从更靠左或更靠上的格子开始放置。";
                return true;
            }

            var rows = BuildConfigRows(fragment);
            ClearIntersectingRewardBackgroundRooms(rows, startX, startY, selectedRewardBackgroundWidth, selectedRewardBackgroundHeight);
            for (var y = startY; y < startY + selectedRewardBackgroundHeight; y++)
            {
                for (var x = startX; x < startX + selectedRewardBackgroundWidth; x++)
                {
                    ApplyRewardBackgroundCell(FindOrCreateCell(fragment, x, y));
                }
            }

            isDragPainting = false;
            selectedPaintCells.Clear();
            ClearLiveSelectionHighlights();
            editNotice = $"已在模板 {fragment.id} 放置 {FormatCurrentBrush()}，范围 ({startX},{startY}) - ({startX + selectedRewardBackgroundWidth - 1},{startY + selectedRewardBackgroundHeight - 1})。";
            controller.Validate();
            Build();
            return true;
        }

        private bool IsRewardBackgroundAreaBrush()
        {
            return selectedBrushMode == FossickBrushMode.RewardBackground
                && !string.IsNullOrEmpty(selectedRewardBackgroundId)
                && selectedRewardBackgroundWidth > 0
                && selectedRewardBackgroundHeight > 0;
        }

        private bool CanPlaceRewardBackgroundArea(int startX, int startY, int width, int height)
        {
            return startX >= 0
                && startY >= 0
                && startX + selectedRewardBackgroundWidth <= width
                && startY + selectedRewardBackgroundHeight <= height;
        }

        private void ApplyRewardBackgroundCell(FossickCellConfig cell)
        {
            if (cell == null)
            {
                return;
            }

            cell.rewardBackgroundId = selectedRewardBackgroundId;
            cell.terrain = FossickTerrainType.Empty;
            cell.hp = 0;
            cell.fog = FossickFogType.None;
            if (IsOreReward(GetReward(cell)))
            {
                cell.reward = null;
            }
        }

        private static void ClearRewardBackgroundCell(FossickCellConfig cell)
        {
            if (cell != null)
            {
                cell.rewardBackgroundId = null;
            }
        }

        private static void ClearIntersectingRewardBackgroundRooms(IReadOnlyList<FossickCellConfig[]> rows, int startX, int startY, int width, int height)
        {
            var rooms = FindIntersectingRewardBackgroundRooms(rows, GetRowsWidth(rows), rows == null ? 0 : rows.Count, startX, startY, width, height);
            for (var i = 0; i < rooms.Count; i++)
            {
                var region = rooms[i];
                for (var y = region.startY; y <= region.endY; y++)
                {
                    for (var x = region.startX; x <= region.endX; x++)
                    {
                        ClearRewardBackgroundCell(GetCell(rows, x, y));
                    }
                }
            }
        }

        private static int GetRowsWidth(IReadOnlyList<FossickCellConfig[]> rows)
        {
            if (rows == null)
            {
                return 0;
            }

            for (var i = 0; i < rows.Count; i++)
            {
                if (rows[i] != null)
                {
                    return rows[i].Length;
                }
            }

            return 0;
        }

        private static List<RewardBackgroundRegion> FindIntersectingRewardBackgroundRooms(IReadOnlyList<FossickCellConfig[]> rows, int mapWidth, int mapHeight, int startX, int startY, int width, int height)
        {
            var rooms = BuildFixedRewardBackgroundRegions(rows, mapWidth, mapHeight);
            var result = new List<RewardBackgroundRegion>();
            var endX = startX + width - 1;
            var endY = startY + height - 1;
            for (var i = 0; i < rooms.Count; i++)
            {
                var room = rooms[i];
                if (room.startX <= endX && room.endX >= startX && room.startY <= endY && room.endY >= startY)
                {
                    result.Add(room);
                }
            }

            return result;
        }

        private static bool IsInsideAnyRegion(int x, int y, List<RewardBackgroundRegion> regions)
        {
            if (regions == null)
            {
                return false;
            }

            for (var i = 0; i < regions.Count; i++)
            {
                var region = regions[i];
                if (x >= region.startX && x <= region.endX && y >= region.startY && y <= region.endY)
                {
                    return true;
                }
            }

            return false;
        }

        private static FossickCellConfig GetCell(IReadOnlyList<FossickCellConfig[]> rows, int x, int y)
        {
            if (rows == null || y < 0 || y >= rows.Count)
            {
                return null;
            }

            var row = rows[y];
            return row == null || x < 0 || x >= row.Length ? null : row[x];
        }

        private void PaintCell(FossickCellConfig cell, bool validate = true)
        {
            if (selectedBrushMode == FossickBrushMode.Terrain)
            {
                cell.terrain = selectedTerrain;
                cell.hp = GetDefaultTerrainHp(selectedTerrain);
                if (!CanAttachOre(cell) && IsOreReward(GetReward(cell)))
                {
                    cell.reward = null;
                    editNotice = "当前格不再是可挖掘地形，已移除矿石。";
                }
            }
            else if (selectedBrushMode == FossickBrushMode.Reward || selectedBrushMode == FossickBrushMode.Tool)
            {
                var reward = CreateSelectedReward();
                if (IsOreReward(reward) && !CanAttachOre(cell))
                {
                    editNotice = "矿石只能埋在可挖掘地形（土或石头）上，不能放在空格或基岩上。";
                    return;
                }

                cell.reward = reward;
            }
            else if (selectedBrushMode == FossickBrushMode.Decoration)
            {
                cell.decorations = string.IsNullOrEmpty(selectedDecorationId)
                    ? new List<string>()
                    : new List<string> { selectedDecorationId };
            }
            else if (selectedBrushMode == FossickBrushMode.Fog)
            {
                cell.fog = selectedFog;
            }
            else if (selectedBrushMode == FossickBrushMode.RewardBackground)
            {
                cell.rewardBackgroundId = string.IsNullOrEmpty(selectedRewardBackgroundId) ? null : selectedRewardBackgroundId;
            }

            if (validate)
            {
                controller.Validate();
            }
        }

        private static FossickElementConfig CreateReward(FossickElementType type)
        {
            if (type == FossickElementType.None)
            {
                return null;
            }

            return new FossickElementConfig
            {
                type = type,
                id = GetDefaultRewardId(type),
                amount = GetDefaultRewardAmount(type)
            };
        }

        private FossickElementConfig CreateSelectedReward()
        {
            if (selectedRewardType == FossickElementType.None)
            {
                return null;
            }

            return new FossickElementConfig
            {
                type = selectedRewardType,
                id = string.IsNullOrEmpty(selectedRewardId) ? GetDefaultRewardId(selectedRewardType) : selectedRewardId,
                amount = selectedRewardAmountOverride > 0
                    ? selectedRewardAmountOverride
                    : GetDefaultRewardAmount(selectedRewardType, selectedRewardId)
            };
        }

        private bool PaintTemplateCell(FossickFragmentConfig fragment, FossickCellConfig cell, Image image, Text text, RectTransform layerRoot)
        {
            if (fragment == null || cell == null)
            {
                return false;
            }

            RecordTemplateUndoSnapshot();
            if (TryPaintRewardBackgroundArea(fragment, cell.x, cell.y))
            {
                MarkTemplateDraftChanged();
                editNotice = $"正在编辑模板 {fragment.id} 的藏宝阁区域。保存后生效。";
                return true;
            }

            PaintCell(cell, false);
            UpdateMineCellVisual(image, text, layerRoot, cell);
            editNotice = $"正在编辑模板 {fragment.id} 的格子 ({cell.x},{cell.y})。保存前不会改动模板库。";
            MarkTemplateDraftChanged();
            return true;
        }

        private void FinishDragPainting()
        {
            if (!isDragPainting)
            {
                return;
            }

            isDragPainting = false;
            selectedPaintCells.Clear();
            ClearLiveSelectionHighlights();
            controller.Validate();
            Build();
        }

        private Color GetRowBarColor(int rowIndex)
        {
            return new Color(0.07f, 0.09f, 0.09f, 1f);
        }

        private void UpdateMineCellVisual(Image image, Text text, RectTransform layerRoot, FossickCellConfig cell)
        {
            if (image != null)
            {
                image.sprite = null;
                image.color = GetCellInteractionColor(cell);
            }

            RefreshCellLayerPreview(layerRoot, cell);

            if (text != null)
            {
                text.text = GetMiniCellLabel(cell);
            }
        }

        private void RefreshCellLayerPreview(RectTransform layerRoot, FossickCellConfig cell)
        {
            if (layerRoot == null)
            {
                return;
            }

            for (var i = layerRoot.childCount - 1; i >= 0; i--)
            {
                var child = layerRoot.GetChild(i);
                child.gameObject.SetActive(false);
                if (Application.isPlaying)
                {
                    Destroy(child.gameObject);
                }
                else
                {
                    DestroyImmediate(child.gameObject);
                }
            }

            DrawCellLayerPreview(layerRoot, cell, MineCellSize, false);
        }

        private bool IsPendingPaint(int fragmentId, int x, int y)
        {
            return pendingPaintFragmentId == fragmentId && pendingPaintX == x && pendingPaintY == y;
        }

        private void ClearPendingPaint()
        {
            pendingPaintFragmentId = -1;
            pendingPaintX = -1;
            pendingPaintY = -1;
        }

        private static FossickGeneratedFragmentSpan FindGeneratedSpan(FossickGeneratedMine mine, int sequenceIndex)
        {
            if (mine == null || mine.fragments == null || sequenceIndex < 0)
            {
                return null;
            }

            for (var i = 0; i < mine.fragments.Count; i++)
            {
                var span = mine.fragments[i];
                if (span != null && span.sequenceIndex == sequenceIndex)
                {
                    return span;
                }
            }

            return null;
        }

        private static string FormatMineSelection(FossickGeneratedFragmentSpan span)
        {
            if (span == null)
            {
                return "当前预览段 未选择";
            }

            return $"当前预览段 #{span.sequenceIndex:00}";
        }

        private static string FormatTemplateCounts(List<FossickFragmentConfig> fragments)
        {
            if (fragments == null || fragments.Count == 0)
            {
                return "模板数量：0";
            }

            var tutorial = 0;
            var regular = 0;
            var reward = 0;
            for (var i = 0; i < fragments.Count; i++)
            {
                var fragment = fragments[i];
                if (fragment == null)
                {
                    continue;
                }

                if (fragment.type == FossickFragmentType.Tutorial)
                {
                    tutorial++;
                }
                else if (fragment.type == FossickFragmentType.Reward)
                {
                    reward++;
                }
                else
                {
                    regular++;
                }
            }

            return $"共 {fragments.Count} 个模板\n新手 {tutorial}  常规 {regular}  奖励 {reward}";
        }

        private static string FormatTemplatePool(FossickFragmentConfig fragment)
        {
            if (fragment == null)
            {
                return "未选择模板";
            }

            if (fragment.type == FossickFragmentType.Tutorial)
            {
                return "新手初始模板，按 ID 顺序拼接在矿井开头";
            }

            if (fragment.type == FossickFragmentType.Reward)
            {
                return "奖励模板，进入奖励插入池";
            }

            return $"常规模板，进入难度 {fragment.difficulty} 抽取池";
        }

        private string FormatMineInstanceSummary()
        {
            if (!mineInstanceGenerated || mineInstanceSourceConfig == null)
            {
                return "状态：未生成地图预览";
            }

            var generation = mineInstanceSourceConfig.generation;
            var dirty = generationRulesDirty ? "\n模板或规则已修改" : "\n模板和规则已同步";
            if (generation == null)
            {
                return $"预览 {minePreviewRows} 行 / {controller.CurrentConfig.boardWidth} x {controller.CurrentConfig.visibleHeight}{dirty}";
            }

            return $"预览 {minePreviewRows} 行 / {controller.CurrentConfig.boardWidth} x {controller.CurrentConfig.visibleHeight}{dirty}";
        }

        private static FossickFragmentConfig CloneFragmentForMineOccurrence(FossickFragmentConfig source)
        {
            if (source == null)
            {
                return null;
            }

            var clone = new FossickFragmentConfig
            {
                id = source.id,
                type = source.type,
                difficulty = source.difficulty,
                weight = source.weight,
                width = source.width,
                height = source.height,
                tags = source.tags == null ? new List<string>() : new List<string>(source.tags),
                cells = new List<FossickCellConfig>()
            };

            if (source.cells != null)
            {
                for (var i = 0; i < source.cells.Count; i++)
                {
                    var cell = source.cells[i];
                    if (cell == null)
                    {
                        continue;
                    }

                    clone.cells.Add(new FossickCellConfig
                    {
                        x = cell.x,
                        y = cell.y,
                        backgroundId = cell.backgroundId,
                        rewardBackgroundId = cell.rewardBackgroundId,
                        terrain = cell.terrain,
                        hp = cell.hp,
                        reward = cell.reward,
                        decorations = cell.decorations == null ? new List<string>() : new List<string>(cell.decorations),
                        fog = cell.fog
                    });
                }
            }

            return clone;
        }

        private static FossickFragmentType NextFragmentType(FossickFragmentType current)
        {
            return current == FossickFragmentType.Reward ? FossickFragmentType.Tutorial : current + 1;
        }

        private static void NormalizeFragmentDifficulty(FossickFragmentConfig fragment)
        {
            if (fragment == null)
            {
                return;
            }

            fragment.difficulty = fragment.type == FossickFragmentType.Regular
                ? Mathf.Max(1, fragment.difficulty)
                : 0;
        }

        private static FossickCellConfig FindOrCreateCell(FossickFragmentConfig fragment, int x, int y)
        {
            for (var i = 0; i < fragment.cells.Count; i++)
            {
                var cell = fragment.cells[i];
                if (cell != null && cell.x == x && cell.y == y)
                {
                    return cell;
                }
            }

            var created = new FossickCellConfig
            {
                x = x,
                y = y,
                terrain = FossickTerrainType.Empty,
                hp = 0,
                fog = FossickFogType.None
            };
            fragment.cells.Add(created);
            return created;
        }

        private static FossickCellConfig FindCell(FossickFragmentConfig fragment, int x, int y)
        {
            if (fragment == null || fragment.cells == null)
            {
                return null;
            }

            for (var i = 0; i < fragment.cells.Count; i++)
            {
                var cell = fragment.cells[i];
                if (cell != null && cell.x == x && cell.y == y)
                {
                    return cell;
                }
            }

            return null;
        }

        private RectTransform CreatePanel(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            var rect = CreateRect(name, parent);
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
            AddImage(rect.gameObject, Panel);
            return rect;
        }

        private RectTransform CreateRow(Transform parent, string name, float width, float height)
        {
            var row = CreateRect(name, parent);
            row.sizeDelta = new Vector2(width, height);
            AddHorizontalLayout(row.gameObject, 4, TextAnchor.MiddleLeft);
            return row;
        }

        private RectTransform CreateContextSection(Transform parent, string name, float height)
        {
            var section = CreateRect(name, parent);
            section.sizeDelta = new Vector2(380f, height);
            AddImage(section.gameObject, new Color(0.14f, 0.17f, 0.19f));
            return section;
        }

        private RectTransform CreateRect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go.GetComponent<RectTransform>();
        }

        private RectTransform AddButton(Transform parent, string label, Vector2 size, Action onClick, bool selected = false, Color? tint = null)
        {
            var rect = CreateRect(label, parent);
            rect.sizeDelta = size;
            AddImage(rect.gameObject, selected ? ButtonSelected : tint ?? ButtonDefault);

            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = rect.GetComponent<Image>();
            button.interactable = onClick != null;
            if (onClick != null)
            {
                button.onClick.AddListener(() => onClick.Invoke());
            }

            AddText(rect, label, 13, FontStyle.Bold, size, TextAnchor.MiddleCenter);
            return rect;
        }

        private RectTransform AddActionButton(Transform parent, string label, Vector2 size, Action onClick, ButtonTone tone = ButtonTone.Default)
        {
            return AddButton(parent, label, size, onClick, false, GetButtonToneColor(tone));
        }

        private static Color GetButtonToneColor(ButtonTone tone)
        {
            switch (tone)
            {
                case ButtonTone.Primary:
                    return ButtonPrimary;
                case ButtonTone.Danger:
                    return ButtonDanger;
                case ButtonTone.Muted:
                    return ButtonMuted;
                default:
                    return ButtonDefault;
            }
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
            text.color = TextColor;
            text.alignment = anchor;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            return text;
        }

        private Text AddTextAt(Transform parent, string value, int size, FontStyle style, float x, float y, float width, float height, TextAnchor anchor = TextAnchor.MiddleLeft)
        {
            var text = AddText(parent, value, size, style, new Vector2(width, height), anchor);
            SetTopLeft(text.GetComponent<RectTransform>(), x, y, width, height);
            return text;
        }

        private Scrollbar CreateVerticalScrollbar(Transform parent, float x, float y, float width, float height)
        {
            var rect = CreateRect("Vertical Scrollbar", parent);
            SetTopLeft(rect, x, y, width, height);
            AddImage(rect.gameObject, new Color(0.18f, 0.2f, 0.22f));

            var handle = CreateRect("Handle", rect);
            Stretch(handle);
            var handleImage = AddImage(handle.gameObject, new Color(0.42f, 0.48f, 0.56f));

            var scrollbar = rect.gameObject.AddComponent<Scrollbar>();
            scrollbar.direction = Scrollbar.Direction.BottomToTop;
            scrollbar.targetGraphic = handleImage;
            scrollbar.handleRect = handle;
            return scrollbar;
        }

        private RectTransform CreateVerticalScrollContent(RectTransform parent, string name)
        {
            const float padding = 8f;
            const float scrollbarWidth = 12f;
            const float scrollbarGap = 6f;

            var viewport = CreateRect($"{name} Viewport", parent);
            Stretch(viewport);
            viewport.offsetMin = new Vector2(padding, padding);
            viewport.offsetMax = new Vector2(-(padding + scrollbarWidth + scrollbarGap), -padding);
            var viewportImage = AddImage(viewport.gameObject, new Color(0f, 0f, 0f, 0f));
            viewportImage.raycastTarget = true;
            var mask = viewport.gameObject.AddComponent<Mask>();
            mask.showMaskGraphic = false;

            var content = CreateRect($"{name} Content", viewport);
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.anchoredPosition = Vector2.zero;
            content.sizeDelta = Vector2.zero;
            AddVerticalLayout(content.gameObject, 8, TextAnchor.UpperLeft);
            var fitter = content.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var scrollbarRect = CreateRect($"{name} Scrollbar", parent);
            scrollbarRect.anchorMin = new Vector2(1f, 0f);
            scrollbarRect.anchorMax = new Vector2(1f, 1f);
            scrollbarRect.pivot = new Vector2(1f, 0.5f);
            scrollbarRect.offsetMin = new Vector2(-(padding + scrollbarWidth), padding);
            scrollbarRect.offsetMax = new Vector2(-padding, -padding);
            AddImage(scrollbarRect.gameObject, new Color(0.15f, 0.17f, 0.19f));

            var handle = CreateRect($"{name} Scrollbar Handle", scrollbarRect);
            Stretch(handle);
            var handleImage = AddImage(handle.gameObject, new Color(0.4f, 0.46f, 0.54f));

            var scrollbar = scrollbarRect.gameObject.AddComponent<Scrollbar>();
            scrollbar.direction = Scrollbar.Direction.BottomToTop;
            scrollbar.targetGraphic = handleImage;
            scrollbar.handleRect = handle;

            var scrollRect = parent.gameObject.AddComponent<ScrollRect>();
            scrollRect.viewport = viewport;
            scrollRect.content = content;
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            scrollRect.scrollSensitivity = 34f;
            scrollRect.verticalScrollbar = scrollbar;
            scrollRect.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.Permanent;
            scrollRect.verticalNormalizedPosition = 1f;
            scrollbar.value = 1f;
            return content;
        }

        private static void AddSpacer(Transform parent, float flexibleWidth)
        {
            var spacer = new GameObject("Spacer", typeof(RectTransform), typeof(LayoutElement));
            spacer.transform.SetParent(parent, false);
            spacer.GetComponent<LayoutElement>().flexibleWidth = flexibleWidth;
        }

        private static void AddVerticalSpace(Transform parent, float height)
        {
            var spacer = new GameObject("Vertical Space", typeof(RectTransform), typeof(LayoutElement));
            spacer.transform.SetParent(parent, false);
            var element = spacer.GetComponent<LayoutElement>();
            element.minHeight = height;
            element.preferredHeight = height;
        }

        private static Image AddImage(GameObject go, Color color)
        {
            var image = go.GetComponent<Image>() ?? go.AddComponent<Image>();
            image.color = color;
            return image;
        }

        private static void AddHorizontalLayout(GameObject go, int padding, TextAnchor alignment)
        {
            var layout = go.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(padding, padding, padding, padding);
            layout.spacing = padding;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            layout.childAlignment = alignment;
        }

        private static void AddVerticalLayout(GameObject go, int padding, TextAnchor alignment)
        {
            var layout = go.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(padding, padding, padding, padding);
            layout.spacing = padding;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            layout.childAlignment = alignment;
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

        private static void EnsureEventSystem()
        {
            if (FindObjectOfType<EventSystem>() != null)
            {
                return;
            }

            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
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

        private static Color GetCellBaseColor(FossickCellConfig cell)
        {
            if (cell != null && cell.terrain != FossickTerrainType.Empty)
            {
                return GetTerrainColor(cell.terrain);
            }

            if (cell != null && !string.IsNullOrEmpty(cell.rewardBackgroundId))
            {
                return new Color(0.58f, 0.38f, 0.08f);
            }

            if (cell != null && !string.IsNullOrEmpty(cell.backgroundId))
            {
                return new Color(0.12f, 0.1f, 0.08f);
            }

            return cell == null ? new Color(0.18f, 0.26f, 0.28f) : GetTerrainColor(cell.terrain);
        }

        private static Color GetRewardColor(FossickElementType type)
        {
            switch (type)
            {
                case FossickElementType.Coin:
                    return new Color(1f, 0.72f, 0.12f, 0.9f);
                case FossickElementType.Ore:
                    return new Color(0.95f, 0.55f, 0.18f, 0.9f);
                case FossickElementType.Item:
                    return new Color(0.85f, 0.2f, 0.18f, 0.9f);
                case FossickElementType.Chest:
                    return new Color(0.95f, 0.48f, 0.08f, 0.9f);
                case FossickElementType.Collection:
                    return new Color(0.75f, 0.32f, 1f, 0.9f);
                default:
                    return new Color(1f, 1f, 1f, 0.75f);
            }
        }

        private static string FormatFragmentType(FossickFragmentType type)
        {
            switch (type)
            {
                case FossickFragmentType.Tutorial:
                    return "新手";
                case FossickFragmentType.Regular:
                    return "常规";
                case FossickFragmentType.Reward:
                    return "奖励";
                default:
                    return type.ToString();
            }
        }

        private static string FormatTerrain(FossickTerrainType terrain)
        {
            switch (terrain)
            {
                case FossickTerrainType.Empty:
                    return "空格";
                case FossickTerrainType.Dirt:
                    return "土";
                case FossickTerrainType.Stone:
                    return "石头";
                case FossickTerrainType.Unbreakable:
                    return "基岩";
                case FossickTerrainType.Explosives:
                    return "炸药箱";
                default:
                    return terrain.ToString();
            }
        }

        private static int GetDefaultTerrainHp(FossickTerrainType terrain)
        {
            switch (terrain)
            {
                case FossickTerrainType.Dirt:
                case FossickTerrainType.Explosives:
                    return 1;
                case FossickTerrainType.Stone:
                    return 2;
                default:
                    return 0;
            }
        }

        private static string FormatBrushMode(FossickBrushMode mode)
        {
            switch (mode)
            {
                case FossickBrushMode.RewardBackground:
                    return "藏宝阁";
                case FossickBrushMode.Terrain:
                    return "挖掘物材质";
                case FossickBrushMode.Reward:
                    return "奖励堆";
                case FossickBrushMode.Tool:
                    return "道具";
                case FossickBrushMode.Decoration:
                    return "装饰";
                case FossickBrushMode.Fog:
                    return "阴影";
                default:
                    return mode.ToString();
            }
        }

        private string FormatCurrentBrush()
        {
            switch (selectedBrushMode)
            {
                case FossickBrushMode.Terrain:
                    return FormatTerrain(selectedTerrain);
                case FossickBrushMode.Reward:
                case FossickBrushMode.Tool:
                    return FormatRewardBrush(selectedRewardType, selectedRewardId);
                case FossickBrushMode.Decoration:
                    return string.IsNullOrEmpty(selectedDecorationId) ? "清空装饰" : selectedDecorationId;
                case FossickBrushMode.Fog:
                    return selectedFog == FossickFogType.Covered ? "迷雾" : "无阴影";
                case FossickBrushMode.RewardBackground:
                    if (string.IsNullOrEmpty(selectedRewardBackgroundId))
                    {
                        return "清空藏宝阁";
                    }

                    return $"{FormatRewardBackgroundBrush(selectedRewardBackgroundId)} {selectedRewardBackgroundWidth}x{selectedRewardBackgroundHeight}";
                default:
                    return selectedBrushMode.ToString();
            }
        }

        private static string FormatRewardBackgroundBrush(string id)
        {
            switch (id)
            {
                case TreasureRoomSmallId:
                    return "小藏宝阁";
                case TreasureRoomMediumId:
                    return "中藏宝阁";
                case TreasureRoomLargeId:
                    return "大藏宝阁";
                default:
                    return id;
            }
        }

        private static string FormatRewardType(FossickElementType type)
        {
            switch (type)
            {
                case FossickElementType.None:
                    return "清空奖励";
                case FossickElementType.Ore:
                    return "矿石";
                case FossickElementType.Coin:
                    return "金币";
                case FossickElementType.Collection:
                    return "收藏品";
                case FossickElementType.Item:
                    return "道具";
                case FossickElementType.Chest:
                    return "宝箱";
                default:
                    return type.ToString();
            }
        }

        private static string FormatRewardBrush(FossickElementType type, string id)
        {
            if (type == FossickElementType.Item)
            {
                switch (id)
                {
                    case "pickaxe":
                        return "矿镐道具";
                    case "dynamite":
                        return "雷管道具";
                    case "tnt":
                        return "炸药道具";
                    case "radar":
                        return "雷达道具";
                }
            }

            if (type == FossickElementType.Coin)
            {
                return "藏宝阁金币";
            }

            if (type == FossickElementType.Ore)
            {
                switch (id)
                {
                    case "ore_copper":
                        return "铜矿";
                    case "ore_silver":
                        return "银矿";
                    case "ore_gold":
                        return "金矿";
                    case "ore_gem":
                        return "宝石矿";
                }
            }

            return FormatRewardType(type);
        }

        private static string GetDefaultRewardId(FossickElementType type)
        {
            switch (type)
            {
                case FossickElementType.Ore:
                    return "ore_copper";
                case FossickElementType.Coin:
                    return "coin_pile";
                case FossickElementType.Collection:
                    return "collection_piece";
                case FossickElementType.Item:
                    return "tool_box";
                case FossickElementType.Chest:
                    return "treasure_chest";
                default:
                    return "reward";
            }
        }

        private static int GetDefaultRewardAmount(FossickElementType type)
        {
            return GetDefaultRewardAmount(type, GetDefaultRewardId(type));
        }

        private static int GetDefaultRewardAmount(FossickElementType type, string id)
        {
            switch (type)
            {
                case FossickElementType.Coin:
                    return 100;
                case FossickElementType.Ore:
                    return GetOreScoreValue(id);
                default:
                    return 1;
            }
        }

        private static int GetOreScoreValue(string id)
        {
            switch (id)
            {
                case "ore_copper":
                    return 10;
                case "ore_silver":
                    return 20;
                case "ore_gem":
                    return 30;
                case "ore_gold":
                    return 50;
                default:
                    return 10;
            }
        }

        private static FossickElementConfig GetReward(FossickCellConfig cell)
        {
            return cell == null ? null : cell.reward;
        }

        private static bool IsOreReward(FossickElementConfig reward)
        {
            return reward != null && reward.type == FossickElementType.Ore;
        }

        private static bool CanAttachOre(FossickCellConfig cell)
        {
            return cell != null
                && (cell.terrain == FossickTerrainType.Dirt || cell.terrain == FossickTerrainType.Stone)
                && cell.hp > 0;
        }

        private static bool HasDecoration(FossickCellConfig cell)
        {
            if (cell == null)
            {
                return false;
            }

            return HasValidDecoration(cell.decorations);
        }

        private static bool HasValidDecoration(List<string> ids)
        {
            if (ids == null)
            {
                return false;
            }

            for (var i = 0; i < ids.Count; i++)
            {
                if (!IsReservedElementArtId(ids[i]))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsReservedElementArtId(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return false;
            }

            switch (id)
            {
                case "small_rock":
                case "gold_pile":
                case "pickaxe":
                case "dynamite":
                case "tnt":
                case "radar":
                case "coin_pile":
                case "ore_copper":
                case "ore_silver":
                case "ore_gold":
                case "ore_gem":
                case "treasure_chest":
                case "collection_piece":
                    return true;
            }

            int numericId;
            return int.TryParse(id, out numericId) && numericId >= 1 && numericId <= 37;
        }

        private static string GetCellLabel(FossickCellConfig cell)
        {
            var reward = GetReward(cell);
            if (reward != null && reward.type != FossickElementType.None)
            {
                return FormatRewardMarker(reward.type) + "\n" + cell.x + "," + cell.y;
            }

            if (cell.terrain == FossickTerrainType.Dirt)
            {
                return "D\n" + cell.x + "," + cell.y;
            }

            if (cell.terrain == FossickTerrainType.Stone)
            {
                return "S" + cell.hp + "\n" + cell.x + "," + cell.y;
            }

            if (cell.terrain == FossickTerrainType.Unbreakable)
            {
                return "基\n" + cell.x + "," + cell.y;
            }

            if (cell.terrain == FossickTerrainType.Explosives)
            {
                return "爆\n" + cell.x + "," + cell.y;
            }

            return ".\n" + cell.x + "," + cell.y;
        }

        private static string GetMiniCellLabel(FossickCellConfig cell)
        {
            var reward = GetReward(cell);
            if (reward != null && reward.type != FossickElementType.None)
            {
                return FormatRewardMarker(reward.type);
            }

            if (cell.terrain == FossickTerrainType.Dirt)
            {
                return "D";
            }

            if (cell.terrain == FossickTerrainType.Stone)
            {
                return "S";
            }

            if (cell.terrain == FossickTerrainType.Unbreakable)
            {
                return "基";
            }

            if (cell.terrain == FossickTerrainType.Explosives)
            {
                return "爆";
            }

            return ".";
        }

        private static string FormatRewardMarker(FossickElementType type)
        {
            switch (type)
            {
                case FossickElementType.Coin:
                    return "G";
                case FossickElementType.Ore:
                    return "O";
                case FossickElementType.Item:
                    return "T";
                case FossickElementType.Chest:
                    return "C";
                case FossickElementType.Collection:
                    return "P";
                default:
                    return "$";
            }
        }

        private static string FormatIssue(FossickValidationIssue issue)
        {
            var location = issue.fragmentId == 0 ? string.Empty : $" 碎片={issue.fragmentId}";
            if (issue.x >= 0 && issue.y >= 0)
            {
                location += $" 格子=({issue.x},{issue.y})";
            }

            return $"[{FormatSeverity(issue.severity)}]{location} {TranslateValidationMessage(issue.message)}";
        }

        private static string FormatSeverity(FossickValidationSeverity severity)
        {
            return severity == FossickValidationSeverity.Error ? "错误" : "警告";
        }

        private static string TranslateValidationMessage(string message)
        {
            switch (message)
            {
                case "Map config is null.":
                    return "地图配置为空。";
                case "Activity must be Fossick.":
                    return "活动名称必须是 Fossick。";
                case "Board width and visible height must be greater than zero.":
                    return "棋盘宽度和可视高度必须大于 0。";
                case "Generation config is missing.":
                    return "缺少生成配置。";
                case "Regular group size must be greater than zero.":
                    return "常规碎片组大小必须大于 0。";
                case "Reward insert range is invalid.":
                    return "奖励碎片插入范围无效。";
                case "At least one difficulty count is required.":
                    return "至少需要配置一个难度数量。";
                case "Difficulty count entry is null.":
                    return "难度数量配置为空。";
                case "Difficulty must be greater than zero.":
                    return "难度必须大于 0。";
                case "Difficulty count must be greater than zero.":
                    return "难度数量必须大于 0。";
                case "At least one fragment is required.":
                    return "至少需要一个碎片。";
                case "Fragment entry is null.":
                    return "碎片配置为空。";
                case "Fragment height must be greater than zero.":
                    return "碎片高度必须大于 0。";
                case "Regular fragment must have a difficulty greater than zero.":
                    return "常规碎片必须配置大于 0 的难度。";
                case "No tutorial fragments found.":
                    return "没有找到新手碎片。";
                case "No reward fragments found.":
                    return "没有找到奖励碎片。";
                case "Fragment cells are missing. Empty cells will be assumed.":
                    return "碎片格子数据缺失，将按空格处理。";
                case "Cell entry is null.":
                    return "格子配置为空。";
                case "Cell coordinate is out of bounds.":
                    return "格子坐标越界。";
                case "Cell coordinate is duplicated.":
                    return "格子坐标重复。";
                case "Cell hp cannot be negative.":
                    return "格子血量不能为负数。";
                case "Breakable terrain should have hp greater than zero.":
                    return "可破坏地形的血量应该大于 0。";
                case "Terrain object hp cannot be negative.":
                    return "地形对象血量不能为负数。";
                case "Terrain object cannot overlap terrain.":
                    return "地形对象不能和土、石头或基岩叠放。";
                case "Reward amount cannot be negative.":
                    return "奖励数量不能为负数。";
                case "Reward is buried under unbreakable terrain.":
                    return "奖励被埋在基岩下。";
                case "Ore must be attached to diggable terrain.":
                    return "矿石只能埋在可挖掘地形上。";
                case "Buried reward must be attached to diggable terrain.":
                    return "埋藏奖励或道具只能放在可挖掘地形上。";
                case "Reward cannot overlap terrain object.":
                    return "奖励不能和地形对象叠放。";
                case "Reward background is usually reserved for reward fragments.":
                    return "奖励层背景通常只应放在奖励碎片中。";
                case "Reward background region shape is invalid.":
                    return "藏宝阁背景区域形状不符合固定规格。";
                default:
                    return message
                        .Replace("Fragment id", "碎片 ID")
                        .Replace("is duplicated.", "重复。")
                        .Replace("Fragment width", "碎片宽度")
                        .Replace("must match board width", "必须匹配棋盘宽度")
                        .Replace("Difficulty counts total", "难度数量总和")
                        .Replace("but regular group size is", "但常规碎片组大小是")
                        .Replace("Difficulty", "难度")
                        .Replace("is duplicated in generation config.", "在生成配置中重复。")
                        .Replace("Generation requires difficulty", "生成配置需要难度")
                        .Replace("but no regular fragment uses it.", "但没有常规碎片使用该难度。")
                        .Replace("Unknown reward background id", "未知藏宝阁背景 ID");
            }
        }
    }
}
