using System;
using Fossick.Core.Validation;
using UnityEngine;
using UnityEngine.UI;

namespace Fossick.MapStudio.Views
{
    public sealed partial class FossickMapStudioView
    {
        private bool operationDialogOpen;
        private string operationDialogTitle;
        private string operationDialogMessage;
        private string operationDialogPrimaryLabel;
        private string operationDialogSecondaryLabel;
        private string operationDialogCancelLabel;
        private Action operationDialogPrimaryAction;
        private Action operationDialogSecondaryAction;
        private Action operationDialogCancelAction;
        private bool validationDialogOpen;
        private float validationScrollNormalizedPosition = 1f;

        private void ShowUnsavedTemplateDialog(string actionName, string message, Action continueAction)
        {
            ShowOperationDialog(
                actionName,
                message,
                "保存",
                () =>
                {
                    if (SaveTemplateEditInternal(false))
                    {
                        continueAction?.Invoke();
                    }
                    else
                    {
                        Build();
                    }
                },
                "丢弃",
                () =>
                {
                    if (DiscardTemplateEditInternal(false))
                    {
                        continueAction?.Invoke();
                    }
                    else
                    {
                        Build();
                    }
                },
                "取消",
                null);
        }

        private void ShowOperationDialog(
            string title,
            string message,
            string primaryLabel,
            Action primaryAction,
            string secondaryLabel = null,
            Action secondaryAction = null,
            string cancelLabel = "取消",
            Action cancelAction = null)
        {
            operationDialogOpen = true;
            operationDialogTitle = title;
            operationDialogMessage = message;
            operationDialogPrimaryLabel = primaryLabel;
            operationDialogPrimaryAction = primaryAction;
            operationDialogSecondaryLabel = secondaryLabel;
            operationDialogSecondaryAction = secondaryAction;
            operationDialogCancelLabel = cancelLabel;
            operationDialogCancelAction = cancelAction;
            Build();
        }

        private void ShowValidationResults()
        {
            controller.Validate();
            validationDialogOpen = true;
            validationScrollNormalizedPosition = 1f;
            Build();
        }

        private void DrawValidationDialog()
        {
            var result = controller.LastValidation ?? controller.Validate();
            var issues = result.issues;
            var errorCount = 0;
            var warningCount = 0;
            for (var i = 0; i < issues.Count; i++)
            {
                var issue = issues[i];
                if (issue == null)
                {
                    continue;
                }

                if (issue.severity == FossickValidationSeverity.Error)
                {
                    errorCount++;
                }
                else if (issue.severity == FossickValidationSeverity.Warning)
                {
                    warningCount++;
                }
            }

            var shade = CreateRect("Validation Dialog Shade", root);
            Stretch(shade);
            AddImage(shade.gameObject, new Color(0f, 0f, 0f, 0.58f)).raycastTarget = true;

            var panel = CreatePanel("Validation Dialog", root, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            panel.pivot = new Vector2(0.5f, 0.5f);
            panel.anchoredPosition = Vector2.zero;
            panel.sizeDelta = new Vector2(920f, 720f);

            AddTextAt(panel, "校验结果", 22, FontStyle.Bold, 28f, 24f, 620f, 34f);
            var summary = issues.Count == 0
                ? "校验通过，没有发现问题。"
                : $"共 {issues.Count} 个问题：{errorCount} 个错误，{warningCount} 个警告。";
            AddTextAt(panel, summary, 14, issues.Count == 0 ? FontStyle.Normal : FontStyle.Bold, 28f, 66f, 700f, 28f);

            var close = AddActionButton(panel, "关闭", new Vector2(120f, 34f), CloseValidationDialog, ButtonTone.Default);
            SetTopLeft(close, 772f, 24f, 120f, 34f);

            const float viewportX = 28f;
            const float viewportY = 112f;
            const float viewportWidth = 864f;
            const float viewportHeight = 548f;
            const float scrollbarWidth = 14f;
            const float scrollbarGap = 8f;
            const float rowHeight = 76f;
            const float rowGap = 8f;
            const float contentPadding = 8f;

            var scrollView = CreateRect("Validation Scroll View", panel);
            SetTopLeft(scrollView, viewportX, viewportY, viewportWidth, viewportHeight);

            var viewport = CreateRect("Validation Viewport", scrollView);
            SetTopLeft(viewport, 0f, 0f, viewportWidth - scrollbarWidth - scrollbarGap, viewportHeight);
            var viewportImage = AddImage(viewport.gameObject, new Color(0.08f, 0.1f, 0.11f));
            viewportImage.raycastTarget = true;
            viewport.gameObject.AddComponent<RectMask2D>();

            var contentHeight = issues.Count == 0
                ? viewportHeight
                : Mathf.Max(viewportHeight, contentPadding * 2f + issues.Count * rowHeight + Mathf.Max(0, issues.Count - 1) * rowGap);
            var content = CreateRect("Validation Content", viewport);
            SetTopLeft(content, 0f, 0f, viewportWidth - scrollbarWidth - scrollbarGap, contentHeight);

            if (issues.Count == 0)
            {
                AddTextAt(content, "当前三个地图数据文件相互匹配，可以正常生成地图。", 15, FontStyle.Normal, 24f, 24f, 760f, 30f);
            }
            else
            {
                for (var i = 0; i < issues.Count; i++)
                {
                    DrawValidationIssueRow(
                        content,
                        issues[i],
                        contentPadding + i * (rowHeight + rowGap),
                        viewportWidth - scrollbarWidth - scrollbarGap - contentPadding * 2f,
                        rowHeight);
                }
            }

            var scrollbar = CreateVerticalScrollbar(
                scrollView,
                viewportWidth - scrollbarWidth,
                0f,
                scrollbarWidth,
                viewportHeight);
            var scrollRect = scrollView.gameObject.AddComponent<ScrollRect>();
            scrollRect.viewport = viewport;
            scrollRect.content = content;
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            scrollRect.scrollSensitivity = 34f;
            scrollRect.verticalScrollbar = scrollbar;
            scrollRect.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.Permanent;
            scrollRect.verticalNormalizedPosition = validationScrollNormalizedPosition;
            scrollbar.value = validationScrollNormalizedPosition;
            scrollRect.onValueChanged.AddListener(position => validationScrollNormalizedPosition = position.y);

            AddTextAt(panel, "错误必须修复；警告不会阻止使用，但建议确认内容是否符合预期。", 12, FontStyle.Normal, 28f, 672f, 700f, 24f);
        }

        private void DrawValidationIssueRow(RectTransform parent, FossickValidationIssue issue, float y, float width, float height)
        {
            if (issue == null)
            {
                return;
            }

            var isError = issue.severity == FossickValidationSeverity.Error;
            var color = isError ? new Color(0.48f, 0.23f, 0.2f) : new Color(0.45f, 0.34f, 0.16f);
            var row = CreateRect("Validation Issue", parent);
            SetTopLeft(row, 0f, y, width, height);
            AddImage(row.gameObject, new Color(color.r, color.g, color.b, 0.42f));

            var stripe = CreateRect("Severity Stripe", row);
            SetTopLeft(stripe, 0f, 0f, 5f, height);
            AddImage(stripe.gameObject, color).raycastTarget = false;

            AddTextAt(row, FormatSeverity(issue.severity), 13, FontStyle.Bold, 16f, 10f, 52f, 24f);
            AddTextAt(row, FormatValidationLocation(issue), 12, FontStyle.Normal, 76f, 10f, 570f, 24f);
            AddTextAt(row, TranslateValidationMessage(issue.message), 13, FontStyle.Normal, 16f, 39f, 650f, 28f);

            if (CanLocateValidationIssue(issue))
            {
                var locate = AddActionButton(row, "定位", new Vector2(88f, 32f), () => LocateValidationIssue(issue), ButtonTone.Primary);
                SetTopLeft(locate, width - 104f, 22f, 88f, 32f);
            }
        }

        private bool CanLocateValidationIssue(FossickValidationIssue issue)
        {
            if (issue == null)
            {
                return false;
            }

            if (issue.category == FossickValidationCategory.GenerationRules)
            {
                return true;
            }

            if (issue.fragmentId == 0 || controller.CurrentConfig.fragments == null)
            {
                return false;
            }

            for (var i = 0; i < controller.CurrentConfig.fragments.Count; i++)
            {
                var fragment = controller.CurrentConfig.fragments[i];
                if (fragment != null && fragment.id == issue.fragmentId)
                {
                    return true;
                }
            }

            return false;
        }

        private void LocateValidationIssue(FossickValidationIssue issue)
        {
            validationDialogOpen = false;
            if (issue.category == FossickValidationCategory.GenerationRules)
            {
                OpenGenerationRules();
                return;
            }

            var fragments = controller.CurrentConfig.fragments;
            for (var i = 0; i < fragments.Count; i++)
            {
                var fragment = fragments[i];
                if (fragment == null || fragment.id != issue.fragmentId)
                {
                    continue;
                }

                BeginTemplateEdit(i, issue.x >= 0 && issue.y >= 0
                    ? $"已定位校验问题：模板 {issue.fragmentId}，格子 ({issue.x},{issue.y})。"
                    : $"已定位校验问题：模板 {issue.fragmentId}。");
                if (issue.x >= 0 && issue.y >= 0)
                {
                    selectedPaintCells.Add(GetTemplateSelectionKey(issue.fragmentId, issue.x, issue.y));
                    Build();
                }

                return;
            }

            Build();
        }

        private static string FormatValidationLocation(FossickValidationIssue issue)
        {
            var category = issue.category == FossickValidationCategory.GenerationRules
                ? "生成规则"
                : issue.category == FossickValidationCategory.MapDefinition
                    ? "地图配置"
                    : "模板";
            if (issue.fragmentId == 0)
            {
                return category;
            }

            return issue.x >= 0 && issue.y >= 0
                ? $"{category} {issue.fragmentId} · 格子 ({issue.x},{issue.y})"
                : $"{category} {issue.fragmentId}";
        }

        private void CloseValidationDialog()
        {
            validationDialogOpen = false;
            Build();
        }

        private void DrawOperationDialog()
        {
            var shade = CreateRect("Operation Dialog Shade", root);
            Stretch(shade);
            AddImage(shade.gameObject, new Color(0f, 0f, 0f, 0.52f)).raycastTarget = true;

            var panel = CreatePanel("Operation Dialog", root, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            panel.pivot = new Vector2(0.5f, 0.5f);
            panel.anchoredPosition = Vector2.zero;
            panel.sizeDelta = new Vector2(560f, 260f);

            var title = AddText(panel, operationDialogTitle ?? "提示", 22, FontStyle.Bold, new Vector2(500f, 34f));
            SetTopLeft(title.GetComponent<RectTransform>(), 30f, 28f, 500f, 34f);

            var message = AddText(panel, operationDialogMessage ?? string.Empty, 15, FontStyle.Normal, new Vector2(500f, 72f));
            SetTopLeft(message.GetComponent<RectTransform>(), 30f, 78f, 500f, 72f);

            const float buttonTop = 188f;
            const float buttonHeight = 36f;
            var hasCancel = !string.IsNullOrEmpty(operationDialogCancelLabel);
            var hasSecondary = !string.IsNullOrEmpty(operationDialogSecondaryLabel);
            var hasPrimary = !string.IsNullOrEmpty(operationDialogPrimaryLabel);

            if (hasCancel)
            {
                var cancel = AddButton(panel, operationDialogCancelLabel, new Vector2(126f, buttonHeight), () =>
                {
                    var action = operationDialogCancelAction;
                    ClearOperationDialogState();
                    action?.Invoke();
                    Build();
                });
                SetTopLeft(cancel, 30f, buttonTop, 126f, buttonHeight);
            }

            if (hasSecondary)
            {
                var secondary = AddButton(panel, operationDialogSecondaryLabel, new Vector2(150f, buttonHeight), () =>
                {
                    var action = operationDialogSecondaryAction;
                    ClearOperationDialogState();
                    action?.Invoke();
                }, false, ButtonDanger);
                SetTopLeft(secondary, 230f, buttonTop, 150f, buttonHeight);
            }

            if (hasPrimary)
            {
                var primaryX = hasCancel || hasSecondary ? 388f : 205f;
                var primary = AddButton(panel, operationDialogPrimaryLabel, new Vector2(150f, buttonHeight), () =>
                {
                    var action = operationDialogPrimaryAction;
                    ClearOperationDialogState();
                    action?.Invoke();
                }, false, ButtonPrimary);
                SetTopLeft(primary, primaryX, buttonTop, 150f, buttonHeight);
            }
        }

        private void ClearOperationDialogState()
        {
            operationDialogOpen = false;
            operationDialogTitle = null;
            operationDialogMessage = null;
            operationDialogPrimaryLabel = null;
            operationDialogSecondaryLabel = null;
            operationDialogCancelLabel = null;
            operationDialogPrimaryAction = null;
            operationDialogSecondaryAction = null;
            operationDialogCancelAction = null;
        }
    }
}
