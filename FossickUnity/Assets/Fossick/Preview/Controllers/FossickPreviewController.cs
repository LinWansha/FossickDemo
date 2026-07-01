using Fossick.Core.Actions;
using Fossick.Core.Board;
using Fossick.Core.Config;
using Fossick.Core.Generation;
using Fossick.Core.Gameplay;
using Fossick.Core.Rewards;
using Fossick.Core.Save;
using Fossick.Core.Serialization;
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

        private FossickActionResolver actionResolver = new FossickActionResolver();
        private FossickMapConfig config;
        private FossickGameplaySession session;
        private FossickSaveState savedState;
        private readonly List<string> actionLog = new List<string>();

        public FossickBoard Board => session == null ? null : session.Board;
        public FossickToolType SelectedTool { get; private set; } = FossickToolType.Pickaxe;
        public FossickProgressState Progress => session == null ? null : session.Progress;
        public FossickInventoryState Inventory => session == null ? null : session.Inventory;
        public FossickRewardState Rewards => session == null ? null : session.Rewards;
        public bool HasSave => savedState != null;
        public bool UnlimitedTools { get; private set; } = true;
        public IReadOnlyList<string> ActionLog => actionLog;

        private void Awake()
        {
            config = LoadConfig();
            actionResolver = new FossickActionResolver(config.tools);

            BuildBoard();
        }

        public FossickActionResult Dig(int x, int y)
        {
            var result = UseTool(x, y);
            return result == null ? null : result.action;
        }

        public void SelectTool(FossickToolType toolType)
        {
            SelectedTool = toolType;
        }

        public IReadOnlyList<FossickToolTarget> GetPreviewTargets(int x, int y)
        {
            return actionResolver.GetToolPreview(Board, SelectedTool, x, y);
        }

        public FossickGameplayActionResult UseTool(int x, int y)
        {
            var result = session.UseTool(SelectedTool, x, y);
            AppendActionLog(result);
            return result;
        }

        public void Save()
        {
            savedState = session == null ? null : session.CreateSaveState();
            AddLog("已保存当前进度。");
        }

        public void ReloadSaved()
        {
            if (savedState == null)
            {
                return;
            }

            seed = savedState.seed;
            BuildBoard(savedState.topVisibleRow + config.visibleHeight, savedState);
            AddLog("已重载保存进度。");
        }

        public void ResetPreview()
        {
            savedState = null;
            actionLog.Clear();
            BuildBoard();
            AddLog("已重置灰盒预览。");
        }

        private void BuildBoard(int minimumRows = -1, FossickSaveState restore = null)
        {
            var rows = minimumRows < 0 ? config.visibleHeight : minimumRows;
            session = restore == null
                ? new FossickGameplaySession(config, seed, rows, UnlimitedTools)
                : FossickGameplaySession.Restore(config, restore, rows, UnlimitedTools);
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

        private void AppendActionLog(FossickGameplayActionResult gameplayResult)
        {
            if (gameplayResult == null)
            {
                return;
            }

            if (gameplayResult.notEnoughTool)
            {
                AddLog(FormatToolName(gameplayResult.toolType) + " 数量不足。");
                return;
            }

            var result = gameplayResult.action;
            if (result == null)
            {
                return;
            }

            var prefix = FormatToolName(result.toolType) + $" ({result.targetX},{result.targetY})";
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

            if (gameplayResult.scoreAfter != gameplayResult.scoreBefore)
            {
                AddLog($"积分 {gameplayResult.scoreBefore} -> {gameplayResult.scoreAfter}");
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

        private static string FormatElementName(Fossick.Core.Config.FossickElementType elementType)
        {
            switch (elementType)
            {
                case Fossick.Core.Config.FossickElementType.Coin:
                    return "金币";
                case Fossick.Core.Config.FossickElementType.Score:
                    return "积分";
                case Fossick.Core.Config.FossickElementType.Collection:
                    return "收藏品";
                case Fossick.Core.Config.FossickElementType.Item:
                    return "道具";
                case Fossick.Core.Config.FossickElementType.Chest:
                    return "宝箱";
                default:
                    return "矿石";
            }
        }
    }
}
