using System;
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
