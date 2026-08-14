using Fossick.Core.Application;
using Fossick.Core.Application.Results;
using Fossick.Core.Application.Commands;
using Fossick.Core.Definition.Config;
using Fossick.Core.Generation;
using Fossick.Core.Data;
using Fossick.Core.Mine;
using Fossick.Core.Definition.Serialization;
using Fossick.MapStudio;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Fossick.Preview.Controllers
{
    public sealed class FossickPreviewController : MonoBehaviour
    {
        private const string SaveFolder = "Fossick/Preview";
        private const string SaveFileName = "FossickGameplaySave.json";

        [SerializeField] private TextAsset fragmentLibraryJson;
        [SerializeField] private TextAsset generationRulesJson;
        [SerializeField] private TextAsset mapDefinitionJson;
        [SerializeField] private int seed = 12345;

        private readonly IFossickRewardProvider rewardProvider = new FossickPreviewRewardProvider();
        private FossickMapConfig config;
        private FossickGameplaySession session;
        private readonly List<string> actionLog = new List<string>();

        public FossickMine Mine => session == null ? null : session.Data.Mine;
        public FossickToolType SelectedTool { get; private set; } = FossickToolType.Pickaxe;
        public FossickProgressData Progress => session == null ? null : session.Data.Progress;
        public FossickInventoryData Inventory => session == null ? null : session.Data.Inventory;
        public FossickRewardData Rewards => session == null ? null : session.Data.Rewards;
        public bool UnlimitedTools { get; private set; } = true;
        public IReadOnlyList<string> ActionLog => actionLog;

        private void Awake()
        {
            config = LoadConfig();
            BuildMine();
        }

        private void OnDisable()
        {
            SavePreviewData();
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused)
            {
                SavePreviewData();
            }
        }

        private void OnApplicationQuit()
        {
            SavePreviewData();
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
            if (Mine == null || session == null)
            {
                return result;
            }

            var absoluteTarget = new FossickPosition(x, Mine.TopVisibleRow + y);
            var targets = session.GetToolTargets(SelectedTool, absoluteTarget);
            for (var i = 0; i < targets.Count; i++)
            {
                var target = targets[i];
                result.Add(new FossickToolTarget
                {
                    x = target.x,
                    y = target.y
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
            if (result != null && result.isApplied)
            {
                SavePreviewData();
            }

            return result;
        }

        public void ResetPreview()
        {
            actionLog.Clear();
            if (FossickMapEditorBridge.IsOfficialMapTesting)
            {
                FossickMapEditorBridge.SaveTestGameplayData(null);
            }
            else
            {
                DeletePreviewData();
            }

            BuildMine();
            SavePreviewData();
            AddLog("已重置灰盒预览。");
        }

        public void SavePreviewData()
        {
            if (session == null)
            {
                return;
            }

            var gameplayData = session.CaptureGameplayData();
            if (FossickMapEditorBridge.IsOfficialMapTesting)
            {
                FossickMapEditorBridge.SaveTestGameplayData(gameplayData);
                return;
            }

            var folder = GetSaveFolderPath();
            if (!Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
            }

            File.WriteAllText(GetSaveFilePath(), JsonUtility.ToJson(gameplayData, true));
        }

        private void BuildMine()
        {
            if (config == null)
            {
                session = null;
                AddLog("地图配置未就绪。");
                return;
            }

            var savedData = FossickMapEditorBridge.IsOfficialMapTesting
                ? FossickMapEditorBridge.TestGameplayData
                : LoadPreviewData();
            session = savedData == null
                ? new FossickGameplaySession(config, seed, CreatePreviewInventory(), rewardProvider)
                : new FossickGameplaySession(config, savedData, rewardProvider);
            session.InfiniteTools = UnlimitedTools;

            EnsureUnlimitedTools();

            if (savedData != null)
            {
                AddLog($"已读取试玩进度，当前深度 {session.Data.Mine.Depth}。");
            }
        }

        private void EnsureUnlimitedTools()
        {
            if (!UnlimitedTools || session == null || session.Data.Inventory == null)
            {
                return;
            }

            session.Data.Inventory.pickaxes = 9999;
            session.Data.Inventory.dynamite = 9999;
            session.Data.Inventory.tnt = 9999;
            session.Data.Inventory.radar = 9999;
        }

        private static FossickInventoryData CreatePreviewInventory()
        {
            return new FossickInventoryData
            {
                pickaxes = 9999,
                dynamite = 9999,
                tnt = 9999,
                radar = 9999
            };
        }

        private FossickMapConfig LoadConfig()
        {
            if (FossickMapEditorBridge.IsOfficialMapTesting && FossickMapEditorBridge.TestMapConfig != null)
            {
                seed = FossickMapEditorBridge.TestSeed;
                return FossickMapEditorBridge.TestMapConfig;
            }

            var editableProject = FossickMapProjectFileService.LoadEditableProject(FossickMapEditorBridge.ActSubType);
            if (editableProject != null)
            {
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

                return project.ToRuntimeConfig();
            }

            return null;
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

        private FossickGameplayData LoadPreviewData()
        {
            var path = GetSaveFilePath();
            if (!File.Exists(path))
            {
                return null;
            }

            try
            {
                var savedData = JsonUtility.FromJson<FossickGameplayData>(File.ReadAllText(path));
                if (savedData == null || savedData.schemaVersion != FossickGameplayData.CurrentSchemaVersion)
                {
                    return null;
                }

                if (savedData.seed != seed)
                {
                    return null;
                }

                if (savedData.boardWidth > 0 && savedData.boardWidth != config.boardWidth)
                {
                    return null;
                }

                if (savedData.visibleHeight > 0 && savedData.visibleHeight != config.visibleHeight)
                {
                    return null;
                }

                return savedData;
            }
            catch
            {
                return null;
            }
        }

        private static void DeletePreviewData()
        {
            var path = GetSaveFilePath();
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }

        private static string GetSaveFolderPath()
        {
            return Path.Combine(Application.persistentDataPath, SaveFolder);
        }

        private static string GetSaveFilePath()
        {
            return Path.Combine(GetSaveFolderPath(), SaveFileName);
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
