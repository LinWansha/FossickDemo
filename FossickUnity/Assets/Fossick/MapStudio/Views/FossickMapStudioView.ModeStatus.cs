using UnityEngine;
using UnityEngine.UI;

namespace Fossick.MapStudio.Views
{
    public sealed partial class FossickMapStudioView
    {
        private void DrawModeBadge(RectTransform parent, float panelWidth, float y, float rightReserved = 0f)
        {
            var badgeWidth = 260f;
            var badgeHeight = 30f;
            var badge = CreateRect("Mode Badge", parent);
            SetTopLeft(badge, Mathf.Max(16f, panelWidth - rightReserved - badgeWidth - 16f), y, badgeWidth, badgeHeight);
            AddImage(badge.gameObject, ButtonMuted);

            var stripe = CreateRect("Mode Badge Stripe", badge);
            SetTopLeft(stripe, 0f, 0f, 4f, badgeHeight);
            AddImage(stripe.gameObject, GetCurrentModeColor()).raycastTarget = false;

            var title = AddText(badge, $"当前模式：{GetCurrentModeTitle()}", 13, FontStyle.Bold, new Vector2(badgeWidth - 16f, badgeHeight), TextAnchor.MiddleLeft);
            SetTopLeft(title.GetComponent<RectTransform>(), 12f, 0f, badgeWidth - 16f, badgeHeight);
            title.horizontalOverflow = HorizontalWrapMode.Overflow;
        }

        private void DrawLeftMenuModeSummary(RectTransform parent)
        {
            AddText(parent, "当前状态", 16, FontStyle.Bold, new Vector2(LeftButtonWidth, 22f));

            var card = CreateRect("Mode Summary Card", parent);
            card.sizeDelta = new Vector2(LeftButtonWidth, 62f);
            AddImage(card.gameObject, ButtonMuted);

            var stripe = CreateRect("Mode Summary Stripe", card);
            SetTopLeft(stripe, 0f, 0f, 4f, 62f);
            AddImage(stripe.gameObject, GetCurrentModeColor()).raycastTarget = false;

            var title = AddText(card, GetCurrentModeTitle(), 14, FontStyle.Bold, new Vector2(LeftButtonWidth - 16f, 22f));
            SetTopLeft(title.GetComponent<RectTransform>(), 12f, 7f, LeftButtonWidth - 18f, 22f);

            var detail = AddText(card, GetCurrentModeDescription(), 12, FontStyle.Normal, new Vector2(LeftButtonWidth - 16f, 28f));
            SetTopLeft(detail.GetComponent<RectTransform>(), 12f, 31f, LeftButtonWidth - 18f, 28f);
        }

        private string GetCurrentModeTitle()
        {
            if (templateLibraryOpen)
            {
                return "模板库";
            }

            if (generationRulesOpen)
            {
                return "生成规则";
            }

            return editMode == MapStudioEditMode.Template ? "模板编辑" : "地图预览";
        }

        private string GetCurrentModeDescription()
        {
            if (templateLibraryOpen)
            {
                return "管理可复用模板，不直接修改矿井。";
            }

            if (generationRulesOpen)
            {
                return "调整随机拼接规则，生成后生效。";
            }

            return editMode == MapStudioEditMode.Template
                ? "编辑模板库内容，保存后参与生成。"
                : "查看半随机生成结果和来源模板。";
        }

        private Color GetCurrentModeColor()
        {
            if (templateLibraryOpen)
            {
                return new Color(0.17f, 0.31f, 0.43f);
            }

            if (generationRulesOpen)
            {
                return new Color(0.23f, 0.35f, 0.28f);
            }

            return editMode == MapStudioEditMode.Template
                ? new Color(0.24f, 0.36f, 0.48f)
                : new Color(0.22f, 0.24f, 0.27f);
        }
    }
}
