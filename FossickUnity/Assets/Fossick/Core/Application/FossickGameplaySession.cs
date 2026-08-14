using System;
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
        private readonly FossickGravitySystem gravitySystem;

        public FossickGameplaySession(
            FossickMapConfig config,
            int seed,
            FossickInventoryData initialInventory,
            IFossickRewardProvider rewardProvider)
        {
            this.config = config;
            this.seed = seed;

            pickupSystem = new FossickPickupSystem();
            toolSystem = new FossickToolSystem();
            digSystem = new FossickDigSystem();
            visibilitySystem = new FossickVisibilitySystem();
            scrollSystem = new FossickScrollSystem(visibilitySystem);
            rewardSystem = new FossickRewardSystem(rewardProvider);
            generationSystem = new FossickGenerationSystem(this.config);
            gravitySystem = new FossickGravitySystem();

            var generationData = new FossickGenerationData(seed);
            var generatedMine = FossickMineLayoutBuilder.Build(this.config, generationData, generationSystem.GetGenerationBufferRows());
            var mine = FossickRuntimeObjectFactory.CreateMine(this.config, generatedMine, rewardProvider);
            Data = new FossickGameplayData(
                seed,
                mine,
                initialInventory,
                new FossickRewardData(),
                new FossickProgressData(),
                generationData);
            visibilitySystem.RefreshFromOpenSpace(Data.Mine, null);
        }

        public FossickGameplaySession(
            FossickMapConfig config,
            FossickGameplayData data,
            IFossickRewardProvider rewardProvider)
            : this(config, GetSeed(data), GetInventory(data), rewardProvider)
        {
            Restore(data);
        }

        public FossickGameplayData Data { get; }

        public bool InfiniteTools { get; set; }

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
            Data.rewards.collectionDiscoveredItems = Data.Rewards.CreateCollectionDiscoveredSaveList();
            Data.mineData.loadedRows.Clear();

            var rows = Data.Mine.Rows;
            for (var i = 0; i < rows.Count; i++)
            {
                var row = rows[i];
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

        public IReadOnlyList<FossickToolTarget> GetToolTargets(FossickToolType toolType, FossickPosition target)
        {
            return toolSystem.GetTargets(Data.Mine, toolType, target);
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
            else
            {
                MarkInvalid(result, 0, 0, "Command is not implemented in FossickGameplaySession yet.");
            }

            if (result.isApplied)
            {
                EnsureGeneratedRowsAhead();
                gravitySystem.Settle(
                    Data.Mine,
                    result,
                    requiredRows => generationSystem.EnsureRows(Data.Mine, Data.Generation, requiredRows));
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

            if (!InfiniteTools && !Data.Inventory.ConsumeTool(command.ToolType))
            {
                MarkInvalid(result, command.Target.x, command.Target.y, "Selected tool is not available.");
                return;
            }

            result.isApplied = true;
            result.toolConsumed = true;
            result.countsForSettlementToolUsage = true;
            result.steps.Add(new FossickActionStep
            {
                type = FossickActionStepType.ToolConsumed,
                x = command.Target.x,
                y = command.Target.y
            });

            for (var i = 0; i < targets.Count; i++)
            {
                result.affectedCells.Add(new FossickToolTarget
                {
                    x = targets[i].x,
                    y = targets[i].y
                });
            }

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
            var triggeredObjects = new HashSet<string>();
            var explosiveTargets = new List<FossickPosition>();
            for (var i = 0; i < targets.Count; i++)
            {
                var target = targets[i];
                var cell = Data.Mine.GetCellAtAbsoluteRow(target.x, target.y);
                if (cell != null && cell.HasTriggerableTerrain)
                {
                    explosiveTargets.Add(cell.Position);
                    continue;
                }

                var invalidWhenNoEffect = command.ToolType == FossickToolType.Pickaxe && i == 0;
                ApplyCellEffect(cell, result, invalidWhenNoEffect, damage, triggeredObjects);
            }

            for (var i = 0; i < explosiveTargets.Count; i++)
            {
                var target = explosiveTargets[i];
                TriggerExplosivesTerrain(
                    Data.Mine.GetCellAtAbsoluteRow(target.x, target.y),
                    result,
                    triggeredObjects);
            }
        }

        private void ApplyCellEffect(
            FossickCell cell,
            FossickActionResult result,
            bool invalidWhenNoEffect,
            int damage,
            HashSet<string> triggeredObjects,
            int sourceX = -1,
            int sourceY = -1)
        {
            if (cell != null && cell.HasTriggerableTerrain)
            {
                TriggerExplosivesTerrain(cell, result, triggeredObjects, sourceX, sourceY);
                return;
            }

            var terrainBefore = cell == null || cell.Terrain == null ? FossickTerrainType.Empty : cell.Terrain.Terrain;
            var hadEmbeddedContent = cell != null && cell.FossickEmbeddedContent != null;
            var hasSupportBelowBefore = HasSupportBelow(cell);
            var deltaStartIndex = result.cellDeltas.Count;
            var applied = digSystem.ApplyCellEffect(
                cell,
                result,
                invalidWhenNoEffect,
                damage,
                hasSupportBelowBefore);
            SetDeltaSource(result, deltaStartIndex, sourceX, sourceY);
            if (!applied || cell == null || hadEmbeddedContent || cell.Terrain != null || cell.HasCollectablePickup)
            {
                return;
            }

            rewardSystem.TrySpawnCoinDrop(cell, terrainBefore, result);
        }

        private bool HasSupportBelow(FossickCell cell)
        {
            if (cell == null)
            {
                return false;
            }

            var below = Data.Mine.GetCellAtAbsoluteRow(cell.Position.x, cell.Position.y + 1);
            return below != null && (below.Terrain != null || below.HasCollectablePickup);
        }

        private void TriggerExplosivesTerrain(
            FossickCell cell,
            FossickActionResult result,
            HashSet<string> triggeredObjects,
            int sourceX = -1,
            int sourceY = -1)
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

            var deltaStartIndex = result.cellDeltas.Count;
            if (!digSystem.TriggerExplosivesTerrain(cell, result))
            {
                return;
            }
            SetDeltaSource(result, deltaStartIndex, sourceX, sourceY);

            for (var y = cell.Position.y - FossickExplosivesTerrain.BlastRadius; y <= cell.Position.y + FossickExplosivesTerrain.BlastRadius; y++)
            {
                for (var x = cell.Position.x - FossickExplosivesTerrain.BlastRadius; x <= cell.Position.x + FossickExplosivesTerrain.BlastRadius; x++)
                {
                    if (x == cell.Position.x && y == cell.Position.y)
                    {
                        continue;
                    }

                    ApplyCellEffect(
                        Data.Mine.GetCellAtAbsoluteRow(x, y),
                        result,
                        false,
                        FossickExplosivesTerrain.BlastDamage,
                        triggeredObjects,
                        cell.Position.x,
                        cell.Position.y);
                }
            }
        }

        private static void SetDeltaSource(
            FossickActionResult result,
            int startIndex,
            int sourceX,
            int sourceY)
        {
            if (sourceX < 0 || sourceY < 0)
            {
                return;
            }

            for (var i = startIndex; i < result.cellDeltas.Count; i++)
            {
                var delta = result.cellDeltas[i];
                delta.source = FossickCellDeltaSource.ExplosiveCrate;
                delta.sourceX = sourceX;
                delta.sourceY = sourceY;
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

        private void EnsureGeneratedRowsAhead()
        {
            generationSystem.EnsureGeneratedRowsAhead(Data.Mine, Data.Generation);
        }

        private void Restore(FossickGameplayData sourceData)
        {
            var mineData = sourceData.mineData;
            if (sourceData.boardWidth != Data.Mine.Spec.width ||
                sourceData.visibleHeight != Data.Mine.Spec.visibleHeight)
            {
                throw new InvalidOperationException("Fossick gameplay save dimensions do not match the current map config.");
            }

            CopyGenerationData(sourceData.Generation, Data.Generation);
            Data.Inventory.pickaxes = sourceData.Inventory.pickaxes;
            Data.Inventory.dynamite = sourceData.Inventory.dynamite;
            Data.Inventory.tnt = sourceData.Inventory.tnt;
            Data.Inventory.radar = sourceData.Inventory.radar;
            Data.Rewards.score = sourceData.Rewards.score;
            Data.Rewards.coins = sourceData.Rewards.coins;
            Data.Rewards.collectionDrawCount = sourceData.Rewards.collectionDrawCount;
            Data.Rewards.LoadCollectionSaveList(sourceData.Rewards.collectionItems);
            Data.Rewards.LoadCollectionDiscoveredSaveList(sourceData.Rewards.collectionDiscoveredItems);
            Data.Progress.depth = sourceData.Progress.depth;
            Data.Progress.oreFound = sourceData.Progress.oreFound;
            Data.Progress.collectionFound = sourceData.Progress.collectionFound;
            Data.Progress.toolUsed = sourceData.Progress.toolUsed;

            Data.Mine.RestoreRows(
                mineData.loadedStartRow,
                mineData.loadedRows,
                mineData.topVisibleRow);
            Data.Mine.RebuildRewardBackgroundRegions(config, Data.Generation.generatedFragmentIds);
            EnsureGeneratedRowsAhead();
        }

        private static int GetSeed(FossickGameplayData data)
        {
            data.Validate();
            return data.seed;
        }

        private static FossickInventoryData GetInventory(FossickGameplayData data)
        {
            return data.inventory;
        }

        private static FossickCellData CreateCellData(FossickCell cell)
        {
            var saved = new FossickCellData();

            saved.x = cell.Position.x;
            saved.y = cell.Position.y;
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

        private static FossickElementConfig CreateElementConfig(FossickEntityPayload payload)
        {
            if (payload == null)
            {
                return null;
            }

            return new FossickElementConfig
            {
                type = payload.ElementType,
                id = payload.Id
            };
        }

        private static void CopyGenerationData(FossickGenerationData source, FossickGenerationData target)
        {
            target.seed = source.seed;
            target.randomState = source.randomState;
            target.sequenceIndex = source.sequenceIndex;
            target.tutorialGenerated = source.tutorialGenerated;
            target.regularGeneratedCount = source.regularGeneratedCount;
            target.regularSinceLastReward = source.regularSinceLastReward;
            target.nextRewardAfterRegularCount = source.nextRewardAfterRegularCount;
            target.groupIndex = source.groupIndex;
            target.pendingRegularFragmentIds = new List<int>(source.pendingRegularFragmentIds);
            target.generatedFragmentIds = new List<int>(source.generatedFragmentIds);
            target.rewardInsertedAfterRegularCounts = new List<int>(source.rewardInsertedAfterRegularCounts);
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
