using UnityEngine;

namespace Fossick.MapStudio.Views
{
    public sealed partial class FossickMapStudioView
    {
        private void DrawTemplateMenuSection(RectTransform panel)
        {
            AddText(panel, "模板管理", 17, FontStyle.Bold, new Vector2(LeftButtonWidth, 24f));

            var fragments = controller.CurrentConfig.fragments;
            AddText(panel, FormatTemplateCounts(fragments), 12, FontStyle.Normal, new Vector2(LeftButtonWidth, 44f));

            var selected = GetSelectedFragment();
            AddText(
                panel,
                selected == null ? "当前模板：未选择" : $"当前模板：{selected.id}  {FormatFragmentType(selected.type)}",
                12,
                FontStyle.Bold,
                new Vector2(LeftButtonWidth, 24f));
            AddButton(panel, "打开模板库", new Vector2(LeftButtonWidth, 32f), OpenTemplateLibrary, templateLibraryOpen);
        }

        private void DrawGenerationMenuSection(RectTransform panel)
        {
            AddText(panel, "生成配置", 17, FontStyle.Bold, new Vector2(LeftButtonWidth, 24f));
            AddButton(panel, "生成规则", new Vector2(LeftButtonWidth, 32f), OpenGenerationRules, generationRulesOpen);
            AddText(
                panel,
                generationRulesDirty ? "模板或规则已修改，需要重新生成地图预览。" : "模板和规则与当前地图预览一致。",
                12,
                generationRulesDirty ? FontStyle.Bold : FontStyle.Normal,
                new Vector2(LeftButtonWidth, 36f));
        }

        private void DrawMineInstanceMenuSection(RectTransform panel)
        {
            AddText(panel, "地图预览", 17, FontStyle.Bold, new Vector2(LeftButtonWidth, 24f));
            AddText(panel, FormatMineInstanceSummary(), 12, FontStyle.Normal, new Vector2(LeftButtonWidth, 56f));

            AddButton(panel, "生成地图预览", new Vector2(LeftButtonWidth, 32f), () =>
            {
                GenerateMineInstance(true, "已按当前模板和半随机规则生成地图预览；矿井会按需无限向下延展。");
                Build();
            });
            AddButton(panel, "关闭预览", new Vector2(LeftButtonWidth, 32f), () =>
            {
                ClearMineInstance("已关闭当前地图预览。");
                Build();
            });
        }
    }
}
