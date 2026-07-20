using System.Collections.Generic;
using Fossick.Core.Application.Commands;
using Fossick.Core.Application.Results;
using Fossick.Core.Definition.Config;
using Fossick.Core.Generation;
using Fossick.Core.Data;
using Fossick.Core.Mine;
using Fossick.Core.Mine.Objects;
using Fossick.Core.Systems;

namespace Fossick.Core.Application
{
    public sealed class FossickGameplaySession
    {
        private readonly FossickMapConfig config;
        private readonly int seed;
        private readonly FossickPickupSystem pickupSystem;
        private readonly FossickToolSystem toolSystem;
        private readonly FossickDigSystem digSystem;
        private readonly FossickVisibilitySystem visibilitySystem;
        private readonly FossickScrollSystem scrollSystem;
        private readonly FossickRewardSystem rewardSystem;
        private readonly FossickGenerationSystem generationSystem;

        public FossickGameplaySession(FossickMapConfig config, int seed, FossickInventoryData initialInventory)
        {
            this.config = config ?? FossickSampleMapFactory.CreateDefaultConfig();
            this.seed = seed;
            initialInventory = initialInventory ?? new FossickInventoryData();

            pickupSystem = new FossickPickupSystem();
            toolSystem = new FossickToolSystem(this.config.tools);
            digSystem = new FossickDigSystem();
            visibilitySystem = new FossickVisibilitySystem();
            scrollSystem = new FossickScrollSystem(visibilitySystem);
            rewardSystem = new FossickRewardSystem(this.config.generation == null ? null : this.config.generation.smallCoinDrop, seed);
            generationSystem = new FossickGenerationSystem(this.config);

            var generationData = new FossickGenerationData(seed);
            var generatedMine = FossickMineLayoutBuilder.Build(this.config, generationData, generationSystem.GetGenerationBufferRows());
            var mine = FossickRuntimeObjectFactory.CreateMine(this.config, generatedMine);
            Data = new FossickGameplayData(
                seed,
                mine,
                initialInventory,
                new FossickRewardData(),
                new FossickProgressData(),
                generationData);
            visibilitySystem.RefreshFromOpenSpace(Data.Mine, null);
        }

        public FossickGameplaySession(FossickMapConfig config, FossickGameplayData data)
            : this(config, data == null ? 0 : data.seed, data == null ? null : data.Inventory)
        {
            Restore(data);
        }

        public FossickGameplayData Data { get; }

        public FossickGameplayData CaptureGameplayData()
        {
            Data.schemaVersion = FossickGameplayData.CurrentSchemaVersion;
            Data.seed = seed;
            Data.boardWidth = Data.Mine.Spec.width;
            Data.visibleHeight = Data.Mine.Spec.visibleHeight;
            Data.mineData.loadedStartRow = Data.Mine.FirstLoadedRow;
            Data.mineData.topVisibleRow = Data.Mine.TopVisibleRow;
            Data.mineData.depth = Data.Mine.Depth;
            Data.rewards.collectionItems = Data.Rewards.CreateCollectionSaveList();
            Data.mineData.loadedRows.Clear();

            var rows = Data.Mine.Rows;
            for (var i = 0; i < rows.Count; i++)
            {
                var row = rows[i];
                if (row == null)
                {
                    continue;
                }

                var savedRow = new FossickMineRowData { rowIndex = row.Depth };
                for (var x = 0; x < row.Cells.Count; x++)
                {
                    savedRow.cells.Add(CreateCellData(row.Cells[x]));
                }

                Data.mineData.loadedRows.Add(savedRow);
            }

            return Data;
        }

        public FossickSnapshot CreateSnapshot()
        {
            return new FossickSnapshot(Data.Mine.Spec, Data.Mine.TopVisibleRow, Data.Mine.Depth, Data.Mine.LoadedRowCount);
        }

        public FossickSettlementResult CreateSettlementResult()
        {
            return new FossickSettlementResult
            {
                depth = Data.Mine.Depth,
                oreFound = Data.Progress.oreFound,
                collectionFound = Data.Progress.collectionFound,
                toolUsed = Data.Progress.toolUsed,
                remainingCoinAmount = Data.Rewards.coins
            };
        }

        public FossickActionResult Execute(FossickCommand command)
        {
            var result = new FossickActionResult
            {
                targetX = 0,
                targetY = 0,
                depthBeforeAction = Data.Mine.Depth,
                depthAfterAction = Data.Mine.Depth
            };

            if (command == null)
            {
                MarkInvalid(result, 0, 0, "Command is null.");
                return result;
            }

            if (command is FossickUseToolCommand useToolCommand)
            {
                ExecuteUseTool(useToolCommand, result);
            }
            else if (command is FossickPickupCommand pickupCommand)
            {
                ExecutePickup(pickupCommand, result);
            }
            else if (command is FossickPreviewCommand previewCommand)
            {
                ExecutePreview(previewCommand, result);
                return result;
            }
            else
            {
                MarkInvalid(result, 0, 0, "Command is not implemented in FossickGameplaySession yet.");
            }

            if (result.isApplied)
            {
                visibilitySystem.RefreshFromOpenSpace(Data.Mine, result);
                scrollSystem.TryScrollUntilStable(Data.Mine, result, result.targetX, result.targetY, EnsureGeneratedRowsAhead);
                EnsureGeneratedRowsAhead();
                generationSystem.PruneRowsBehind(Data.Mine);
                rewardSystem.ApplyRewards(result, Data.Rewards, Data.Inventory);
                Data.Progress.Apply(result);
            }

            return result;
        }

        private void ExecuteUseTool(FossickUseToolCommand command, FossickActionResult result)
        {
            result.toolType = command.ToolType;
            result.targetX = command.Target.x;
            result.targetY = command.Target.y;

            var targetCell = Data.Mine.GetCellAtAbsoluteRow(command.Target.x, command.Target.y);
            if (targetCell != null && targetCell.IsVisible && targetCell.HasCollectablePickup)
            {
                pickupSystem.Collect(targetCell, result);
                return;
            }

            var targets = toolSystem.GetTargets(Data.Mine, command.ToolType, command.Target);
            if (targets.Count == 0)
            {
                MarkInvalid(result, command.Target.x, command.Target.y, "Target is not valid for the selected tool.");
                return;
            }

            if (!Data.Inventory.ConsumeTool(command.ToolType))
            {
                MarkInvalid(result, command.Target.x, command.Target.y, "Selected tool is not available.");
                return;
            }

            result.isApplied = true;
            result.toolConsumed = true;
            result.steps.Add(new FossickActionStep
            {
                type = FossickActionStepType.ToolConsumed,
                x = command.Target.x,
                y = command.Target.y
            });

            if (command.ToolType == FossickToolType.Radar)
            {
                for (var i = 0; i < targets.Count; i++)
                {
                    var target = targets[i];
                    visibilitySystem.ApplyRadarReveal(Data.Mine.GetCellAtAbsoluteRow(target.x, target.y), result);
                }

                return;
            }

            var damage = FossickToolSystem.GetToolDamage(command.ToolType);
            for (var i = 0; i < targets.Count; i++)
            {
                var target = targets[i];
                var invalidWhenNoEffect = command.ToolType == FossickToolType.Pickaxe && i == 0;
                ApplyCellEffect(Data.Mine.GetCellAtAbsoluteRow(target.x, target.y), result, invalidWhenNoEffect, damage, null);
            }
        }

        private void ApplyCellEffect(FossickCell cell, FossickActionResult result, bool invalidWhenNoEffect, int damage, HashSet<string> triggeredObjects)
        {
            if (cell != null && cell.HasTriggerableTerrain)
            {
                TriggerExplosivesTerrain(cell, result, triggeredObjects);
                return;
            }

            var terrainBefore = cell == null || cell.Terrain == null ? FossickTerrainType.Empty : cell.Terrain.Terrain;
            var hadEmbeddedContent = cell != null && cell.FossickEmbeddedContent != null;
            var applied = digSystem.ApplyCellEffect(cell, result, invalidWhenNoEffect, damage);
            if (!applied || cell == null || hadEmbeddedContent || cell.Terrain != null || cell.HasCollectablePickup)
            {
                return;
            }

            rewardSystem.TrySpawnSmallCoinDrop(cell, terrainBefore, result);
        }

        private void TriggerExplosivesTerrain(FossickCell cell, FossickActionResult result, HashSet<string> triggeredObjects)
        {
            if (cell == null || !cell.HasTriggerableTerrain)
            {
                return;
            }

            triggeredObjects ??= new HashSet<string>();
            var key = cell.Position.x + ":" + cell.Position.y;
            if (!triggeredObjects.Add(key))
            {
                return;
            }

            if (!digSystem.TriggerExplosivesTerrain(cell, result))
            {
                return;
            }

            for (var y = cell.Position.y - FossickExplosivesTerrain.BlastRadius; y <= cell.Position.y + FossickExplosivesTerrain.BlastRadius; y++)
            {
                for (var x = cell.Position.x - FossickExplosivesTerrain.BlastRadius; x <= cell.Position.x + FossickExplosivesTerrain.BlastRadius; x++)
                {
                    if (x == cell.Position.x && y == cell.Position.y)
                    {
                        continue;
                    }

                    ApplyCellEffect(Data.Mine.GetCellAtAbsoluteRow(x, y), result, false, FossickExplosivesTerrain.BlastDamage, triggeredObjects);
                }
            }
        }

        private void ExecutePickup(FossickPickupCommand command, FossickActionResult result)
        {
            result.targetX = command.Target.x;
            result.targetY = command.Target.y;

            var cell = Data.Mine.GetCellAtAbsoluteRow(command.Target.x, command.Target.y);
            if (cell == null || !cell.IsVisible || !cell.HasCollectablePickup)
            {
                MarkInvalid(result, command.Target.x, command.Target.y, "Target cell has no collectable pickup.");
                return;
            }

            pickupSystem.Collect(cell, result);
        }

        private void ExecutePreview(FossickPreviewCommand command, FossickActionResult result)
        {
            if (command.Seed != seed)
            {
                MarkInvalid(result, 0, command.StartDepth, "Preview seed does not match this gameplay session.");
                return;
            }

            var depthBefore = Data.Mine.Depth;
            generationSystem.EnsureRows(Data.Mine, Data.Generation, command.StartDepth + generationSystem.GetGenerationBufferRows());
            if (!Data.Mine.MoveWindowTo(command.StartDepth))
            {
                MarkInvalid(result, 0, command.StartDepth, "Preview depth is outside generated mine range.");
                return;
            }

            result.isApplied = true;
            result.targetX = 0;
            result.targetY = command.StartDepth;
            result.depthBeforeAction = depthBefore;
            result.depthAfterAction = Data.Mine.Depth;
            visibilitySystem.RefreshFromOpenSpace(Data.Mine, result);
        }

        private void EnsureGeneratedRowsAhead()
        {
            generationSystem.EnsureGeneratedRowsAhead(Data.Mine, Data.Generation);
        }

        private void Restore(FossickGameplayData sourceData)
        {
            sourceData?.EnsureDefaults();
            var mineData = sourceData == null ? null : sourceData.mineData;
            if (sourceData == null || mineData == null || mineData.loadedRows == null || mineData.loadedRows.Count == 0)
            {
                return;
            }

            if (sourceData.boardWidth > 0 && sourceData.boardWidth != Data.Mine.Spec.width)
            {
                return;
            }

            if (sourceData.visibleHeight > 0 && sourceData.visibleHeight != Data.Mine.Spec.visibleHeight)
            {
                return;
            }

            CopyGenerationData(sourceData.Generation, Data.Generation);
            Data.Inventory.pickaxes = sourceData.Inventory.pickaxes;
            Data.Inventory.dynamite = sourceData.Inventory.dynamite;
            Data.Inventory.tnt = sourceData.Inventory.tnt;
            Data.Inventory.radar = sourceData.Inventory.radar;
            Data.Rewards.score = sourceData.Rewards.score;
            Data.Rewards.coins = sourceData.Rewards.coins;
            Data.Rewards.LoadCollectionSaveList(sourceData.Rewards.collectionItems);
            Data.Progress.depth = sourceData.Progress.depth;
            Data.Progress.oreFound = sourceData.Progress.oreFound;
            Data.Progress.collectionFound = sourceData.Progress.collectionFound;
            Data.Progress.toolUsed = sourceData.Progress.toolUsed;

            var rows = new List<IReadOnlyList<FossickCellConfig>>();
            for (var i = 0; i < mineData.loadedRows.Count; i++)
            {
                var savedRow = mineData.loadedRows[i];
                var row = new List<FossickCellConfig>();
                if (savedRow != null && savedRow.cells != null)
                {
                    for (var j = 0; j < savedRow.cells.Count; j++)
                    {
                        row.Add(CreateCellConfig(savedRow.cells[j]));
                    }
                }

                rows.Add(row);
            }

            Data.Mine.RestoreRows(mineData.loadedStartRow, rows, mineData.topVisibleRow);
            EnsureGeneratedRowsAhead();
        }

        private static FossickCellData CreateCellData(FossickCell cell)
        {
            var saved = new FossickCellData();
            if (cell == null)
            {
                return saved;
            }

            saved.x = cell.Position.x;
            saved.y = cell.Position.y;
            saved.backgroundId = cell.BackgroundId;
            saved.rewardBackgroundId = cell.RewardBackgroundId;
            saved.terrain = cell.Terrain == null ? FossickTerrainType.Empty : cell.Terrain.Terrain;
            saved.hp = cell.Terrain == null ? 0 : cell.Terrain.Hp;
            saved.reward = CreateElementConfig(cell.FossickEmbeddedContent == null ? null : cell.FossickEmbeddedContent.Payload)
                ?? CreateElementConfig(cell.Pickup == null || cell.Pickup.Collected ? null : cell.Pickup.Payload);
            saved.fog = cell.IsVisible ? FossickFogType.None : FossickFogType.Covered;
            saved.collected = cell.Pickup != null && cell.Pickup.Collected;
            for (var i = 0; i < cell.Decorations.Count; i++)
            {
                var decoration = cell.Decorations[i];
                if (decoration != null && !string.IsNullOrEmpty(decoration.DecorationId))
                {
                    saved.decorations.Add(decoration.DecorationId);
                }
            }

            return saved;
        }

        private static FossickCellConfig CreateCellConfig(FossickCellData saved)
        {
            if (saved == null)
            {
                return new FossickCellConfig();
            }

            return new FossickCellConfig
            {
                x = saved.x,
                y = saved.y,
                backgroundId = saved.backgroundId,
                rewardBackgroundId = saved.rewardBackgroundId,
                terrain = saved.terrain,
                hp = saved.hp,
                reward = saved.collected ? null : saved.reward,
                decorations = saved.decorations == null ? new List<string>() : new List<string>(saved.decorations),
                fog = saved.fog
            };
        }

        private static FossickElementConfig CreateElementConfig(FossickRewardPayload payload)
        {
            if (payload == null)
            {
                return null;
            }

            return new FossickElementConfig
            {
                type = payload.ElementType,
                id = payload.Id,
                amount = payload.Amount
            };
        }

        private static void CopyGenerationData(FossickGenerationData source, FossickGenerationData target)
        {
            if (source == null || target == null)
            {
                return;
            }

            target.seed = source.seed;
            target.randomState = source.randomState;
            target.sequenceIndex = source.sequenceIndex;
            target.tutorialGenerated = source.tutorialGenerated;
            target.regularGeneratedCount = source.regularGeneratedCount;
            target.regularSinceLastReward = source.regularSinceLastReward;
            target.nextRewardAfterRegularCount = source.nextRewardAfterRegularCount;
            target.groupIndex = source.groupIndex;
            target.pendingRegularFragmentIds = source.pendingRegularFragmentIds == null
                ? new List<int>()
                : new List<int>(source.pendingRegularFragmentIds);
            target.generatedFragmentIds = source.generatedFragmentIds == null
                ? new List<int>()
                : new List<int>(source.generatedFragmentIds);
            target.rewardInsertedAfterRegularCounts = source.rewardInsertedAfterRegularCounts == null
                ? new List<int>()
                : new List<int>(source.rewardInsertedAfterRegularCounts);
        }

        private static void MarkInvalid(FossickActionResult result, int x, int y, string reason)
        {
            result.invalidReason = reason;
            result.steps.Add(new FossickActionStep
            {
                type = FossickActionStepType.InvalidTarget,
                x = x,
                y = y
            });
        }
    }
}
