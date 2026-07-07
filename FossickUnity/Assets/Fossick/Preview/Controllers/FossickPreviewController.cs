using Fossick.Core.Application;
using Fossick.Core.Application.Results;
using Fossick.Core.Application.Commands;
using Fossick.Core.Definition.Config;
using Fossick.Core.Generation;
using Fossick.Core.State;
using Fossick.Core.Mine;
using Fossick.Core.Definition.Serialization;
using Fossick.Core.Systems;
using System.Collections.Generic;
using UnityEngine;

namespace Fossick.Preview.Controllers
{
    public sealed class FossickPreviewController : MonoBehaviour
    {
        [SerializeField] private TextAsset mapJson;
        [SerializeField] private TextAsset fragmentLibraryJson;
        [SerializeField] private TextAsset generationRulesJson;
        [SerializeField] private TextAsset mapDefinitionJson;
        [SerializeField] private int seed = 12345;

        private FossickToolSystem toolSystem;
        private FossickMapConfig config;
        private FossickGameplaySession session;
        private readonly List<string> actionLog = new List<string>();

        public FossickMine Mine => session == null ? null : session.State.Mine;
        public FossickToolType SelectedTool { get; private set; } = FossickToolType.Pickaxe;
        public FossickProgressState Progress => session == null ? null : session.State.Progress;
        public FossickInventoryState Inventory => session == null ? null : session.State.Inventory;
        public FossickRewardState Rewards => session == null ? null : session.State.Rewards;
        public bool UnlimitedTools { get; private set; } = true;
        public IReadOnlyList<string> ActionLog => actionLog;

        private void Awake()
        {
            config = LoadConfig();
            toolSystem = new FossickToolSystem(config.tools);

            BuildMine();
        }

        public FossickActionResult Dig(int x, int y)
        {
            return UseTool(x, y);
        }

        public void SelectTool(FossickToolType toolType)
        {
            SelectedTool = toolType;
        }

        public IReadOnlyList<FossickToolTarget> GetPreviewTargets(int x, int y)
        {
            var result = new List<FossickToolTarget>();
            if (Mine == null || toolSystem == null)
            {
                return result;
            }

            var absoluteTarget = new FossickPosition(x, Mine.TopVisibleRow + y);
            var targets = toolSystem.GetTargets(Mine, SelectedTool, absoluteTarget);
            for (var i = 0; i < targets.Count; i++)
            {
                var target = targets[i];
                result.Add(new FossickToolTarget
                {
                    x = target.x,
                    y = target.y - Mine.TopVisibleRow
                });
            }

            return result;
        }

        public FossickActionResult UseTool(int x, int y)
        {
            if (session == null || Mine == null)
            {
                return null;
            }

            var absoluteTarget = new FossickPosition(x, Mine.TopVisibleRow + y);
            var result = session.Execute(new FossickUseToolCommand(SelectedTool, absoluteTarget));
            AppendActionLog(result);
            return result;
        }

        public void ResetPreview()
        {
            actionLog.Clear();
            BuildMine();
            AddLog("已重置灰盒预览。");
        }

        private void BuildMine()
        {
            session = new FossickGameplaySession(config, seed);
            if (UnlimitedTools && session.State.Inventory != null)
            {
                session.State.Inventory.pickaxes = 9999;
                session.State.Inventory.dynamite = 9999;
                session.State.Inventory.tnt = 9999;
                session.State.Inventory.radar = 9999;
            }
        }

        private FossickMapConfig LoadConfig()
        {
            var editableProject = FossickMapProjectFileService.LoadEditableProject();
            if (editableProject != null)
            {
                if (editableProject.mapDefinition != null)
                {
                    seed = editableProject.mapDefinition.seed;
                }

                return editableProject.ToRuntimeConfig();
            }

            if (fragmentLibraryJson != null && generationRulesJson != null && mapDefinitionJson != null)
            {
                var project = new FossickMapProjectConfig
                {
                    fragmentLibrary = FossickMapJsonUtility.FragmentLibraryFromJson(fragmentLibraryJson.text),
                    generationRules = FossickMapJsonUtility.GenerationRulesFromJson(generationRulesJson.text),
                    mapDefinition = FossickMapJsonUtility.MapDefinitionFromJson(mapDefinitionJson.text)
                };

                if (project.mapDefinition != null)
                {
                    seed = project.mapDefinition.seed;
                }

                return project.ToRuntimeConfig();
            }

            if (mapJson != null)
            {
                return FossickMapJsonUtility.FromJson(mapJson.text);
            }

            return FossickSampleMapFactory.CreateDefaultConfig();
        }

        private void AppendActionLog(FossickActionResult result)
        {
            if (result == null)
            {
                return;
            }

            var prefix = result.isCollectOnly ? "拾取" : FormatToolName(result.toolType) + $" ({result.targetX},{result.targetY})";
            if (result.steps.Count == 0 || result.steps[0].type == FossickActionStepType.InvalidTarget)
            {
                AddLog(prefix + " 无效目标。");
                return;
            }

            AddLog(prefix + " 执行。");
            for (var i = 0; i < result.rewards.Count; i++)
            {
                var reward = result.rewards[i];
                if (reward == null)
                {
                    continue;
                }

                AddLog($"获得 {FormatElementName(reward.elementType)} +{reward.amount}");
            }

            if (result.scrolled)
            {
                AddLog($"矿井连续下移 {result.scrollCount} 行，当前深度 {result.depthAfterAction}");
            }
        }

        private void AddLog(string message)
        {
            if (string.IsNullOrEmpty(message))
            {
                return;
            }

            actionLog.Insert(0, message);
            while (actionLog.Count > 10)
            {
                actionLog.RemoveAt(actionLog.Count - 1);
            }
        }

        private static string FormatToolName(FossickToolType toolType)
        {
            switch (toolType)
            {
                case FossickToolType.Dynamite:
                    return "雷管";
                case FossickToolType.Tnt:
                    return "炸药";
                case FossickToolType.Radar:
                    return "雷达";
                default:
                    return "矿镐";
            }
        }

        private static string FormatElementName(Fossick.Core.Definition.Config.FossickElementType elementType)
        {
            switch (elementType)
            {
                case Fossick.Core.Definition.Config.FossickElementType.Coin:
                    return "金币";
                case Fossick.Core.Definition.Config.FossickElementType.Collection:
                    return "收藏品";
                case Fossick.Core.Definition.Config.FossickElementType.Item:
                    return "道具";
                case Fossick.Core.Definition.Config.FossickElementType.Chest:
                    return "宝箱";
                default:
                    return "矿石";
            }
        }
    }
}
