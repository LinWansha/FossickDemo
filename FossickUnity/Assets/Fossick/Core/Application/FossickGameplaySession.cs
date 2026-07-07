using Fossick.Core.Application.Commands;
using Fossick.Core.Application.Results;
using Fossick.Core.Definition.Config;
using Fossick.Core.Generation;
using Fossick.Core.State;
using Fossick.Core.Mine;
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

        public FossickGameplaySession(FossickMapConfig config, int seed)
        {
            this.config = config ?? FossickSampleMapFactory.CreateDefaultConfig();
            this.seed = seed;

            pickupSystem = new FossickPickupSystem();
            toolSystem = new FossickToolSystem(this.config.tools);
            digSystem = new FossickDigSystem(this.config.generation == null ? null : this.config.generation.smallCoinDrop, seed);
            visibilitySystem = new FossickVisibilitySystem();
            scrollSystem = new FossickScrollSystem(visibilitySystem);
            rewardSystem = new FossickRewardSystem();
            generationSystem = new FossickGenerationSystem(this.config);

            var generationState = new FossickGenerationState(seed);
            var generatedMine = FossickMineLayoutBuilder.Build(this.config, generationState, generationSystem.GetGenerationBufferRows());
            var mine = FossickRuntimeObjectFactory.CreateMine(this.config, generatedMine);
            State = new FossickRuntimeState(
                mine,
                FossickInventoryState.FromConfig(this.config.gameplay),
                new FossickRewardState(),
                new FossickProgressState(),
                generationState);
            visibilitySystem.RefreshFromOpenSpace(State.Mine, null);
        }

        public FossickRuntimeState State { get; }

        public FossickSnapshot CreateSnapshot()
        {
            return new FossickSnapshot(State.Mine.Spec, State.Mine.TopVisibleRow, State.Mine.Depth, State.Mine.LoadedRowCount);
        }

        public FossickSettlementResult CreateSettlementResult()
        {
            return new FossickSettlementResult
            {
                depth = State.Mine.Depth,
                oreFound = State.Progress.oreFound,
                collectionFound = State.Progress.collectionFound,
                toolUsed = State.Progress.toolUsed,
                remainingCoinAmount = State.Rewards.coins
            };
        }

        public FossickActionResult Execute(FossickCommand command)
        {
            var context = new FossickActionContext(config, State, command, seed);
            var result = new FossickActionResult
            {
                targetX = 0,
                targetY = 0,
                depthBeforeAction = context.State.Mine.Depth,
                depthAfterAction = context.State.Mine.Depth
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
                visibilitySystem.RefreshFromOpenSpace(State.Mine, result);
                scrollSystem.TryScrollUntilStable(State.Mine, result, result.targetX, result.targetY, EnsureGeneratedRowsAhead);
                EnsureGeneratedRowsAhead();
                generationSystem.PruneRowsBehind(State.Mine);
                rewardSystem.ApplyRewards(result, State.Rewards, State.Inventory);
                State.Progress.Apply(result);
            }

            return result;
        }

        private void ExecuteUseTool(FossickUseToolCommand command, FossickActionResult result)
        {
            result.toolType = command.ToolType;
            result.targetX = command.Target.x;
            result.targetY = command.Target.y;

            var targetCell = State.Mine.GetCellAtAbsoluteRow(command.Target.x, command.Target.y);
            if (targetCell != null && targetCell.IsVisible && targetCell.HasCollectablePickup)
            {
                pickupSystem.Collect(targetCell, result);
                return;
            }

            var targets = toolSystem.GetTargets(State.Mine, command.ToolType, command.Target);
            if (targets.Count == 0)
            {
                MarkInvalid(result, command.Target.x, command.Target.y, "Target is not valid for the selected tool.");
                return;
            }

            if (!State.Inventory.ConsumeTool(command.ToolType))
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
                    visibilitySystem.ApplyRadarReveal(State.Mine.GetCellAtAbsoluteRow(target.x, target.y), result);
                }

                return;
            }

            var damage = FossickToolSystem.GetToolDamage(command.ToolType);
            for (var i = 0; i < targets.Count; i++)
            {
                var target = targets[i];
                var invalidWhenNoEffect = command.ToolType == FossickToolType.Pickaxe && i == 0;
                digSystem.ApplyCellEffect(State.Mine.GetCellAtAbsoluteRow(target.x, target.y), result, invalidWhenNoEffect, damage);
            }
        }

        private void ExecutePickup(FossickPickupCommand command, FossickActionResult result)
        {
            result.targetX = command.Target.x;
            result.targetY = command.Target.y;

            var cell = State.Mine.GetCellAtAbsoluteRow(command.Target.x, command.Target.y);
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

            var depthBefore = State.Mine.Depth;
            generationSystem.EnsureRows(State.Mine, State.Generation, command.StartDepth + generationSystem.GetGenerationBufferRows());
            if (!State.Mine.MoveWindowTo(command.StartDepth))
            {
                MarkInvalid(result, 0, command.StartDepth, "Preview depth is outside generated mine range.");
                return;
            }

            result.isApplied = true;
            result.targetX = 0;
            result.targetY = command.StartDepth;
            result.depthBeforeAction = depthBefore;
            result.depthAfterAction = State.Mine.Depth;
            visibilitySystem.RefreshFromOpenSpace(State.Mine, result);
        }

        private void EnsureGeneratedRowsAhead()
        {
            generationSystem.EnsureGeneratedRowsAhead(State.Mine, State.Generation);
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
