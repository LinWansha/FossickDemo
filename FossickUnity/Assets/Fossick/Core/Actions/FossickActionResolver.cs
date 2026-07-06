using System.Collections.Generic;
using Fossick.Core.Board;
using Fossick.Core.Config;
using Fossick.Core.Generation;

namespace Fossick.Core.Actions
{
    public sealed class FossickActionResolver
    {
        private readonly FossickToolRulesConfig toolRules;
        private readonly FossickSmallCoinDropConfig smallCoinDrop;
        private readonly int seed;

        public FossickActionResolver()
            : this(null)
        {
        }

        public FossickActionResolver(FossickToolRulesConfig toolRules)
            : this(toolRules, null, 0)
        {
        }

        public FossickActionResolver(FossickToolRulesConfig toolRules, FossickSmallCoinDropConfig smallCoinDrop, int seed)
        {
            this.toolRules = toolRules ?? new FossickToolRulesConfig();
            this.smallCoinDrop = smallCoinDrop ?? new FossickSmallCoinDropConfig();
            this.seed = seed;
        }

        public FossickActionResult ResolvePickaxe(FossickBoard board, int x, int y)
        {
            return ResolveTool(board, FossickToolType.Pickaxe, x, y);
        }

        public FossickActionResult ResolveCollectReward(FossickBoard board, FossickToolType sourceToolType, int x, int y)
        {
            var result = new FossickActionResult
            {
                toolType = sourceToolType,
                targetX = x,
                targetY = y,
                isCollectOnly = true,
                depthBeforeAction = board == null ? 0 : board.Depth,
                depthAfterAction = board == null ? 0 : board.Depth
            };

            if (board == null)
            {
                MarkInvalid(result, x, y, "Board is null.");
                return result;
            }

            var cell = board.GetCell(x, y);
            if (cell == null || cell.fog != FossickFogType.None || !cell.HasSpawnedReward)
            {
                MarkInvalid(result, x, y, "Target cell has no collectable reward entity.");
                result.depthAfterAction = board.Depth;
                return result;
            }

            result.isApplied = CollectReward(cell, result);
            FinishToolOperation(board, result, sourceToolType, x, y);
            return result;
        }

        public IReadOnlyList<FossickToolTarget> GetToolPreview(FossickBoard board, FossickToolType toolType, int x, int y)
        {
            var targets = new List<FossickToolTarget>();
            if (board == null)
            {
                return targets;
            }

            AddToolTargets(board, toolType, x, y, targets);
            return targets;
        }

        public FossickActionResult ResolveTool(FossickBoard board, FossickToolType toolType, int x, int y)
        {
            var result = new FossickActionResult
            {
                toolType = toolType,
                targetX = x,
                targetY = y,
                depthBeforeAction = board == null ? 0 : board.Depth,
                depthAfterAction = board == null ? 0 : board.Depth
            };

            if (board == null)
            {
                MarkInvalid(result, x, y, "Board is null.");
                return result;
            }

            var targets = new List<FossickToolTarget>();
            if (toolType == FossickToolType.Radar)
            {
                AddVisibleWindowTargets(board, targets);
            }
            else
            {
                targets.AddRange(GetToolPreview(board, toolType, x, y));
            }

            if (targets.Count == 0)
            {
                MarkInvalid(result, x, y, "Target is not valid for selected tool.");
                result.depthAfterAction = board.Depth;
                return result;
            }

            if (toolType == FossickToolType.Pickaxe)
            {
                var target = targets[0];
                var cell = board.GetCell(target.x, target.y);
                if (!ApplyCellEffect(cell, result, true, true, true, 1))
                {
                    result.depthAfterAction = board.Depth;
                    return result;
                }
            }
            else if (toolType == FossickToolType.Radar)
            {
                AddStep(result, FossickActionStepType.ToolConsumed, x, y);
                result.toolConsumed = true;
                for (var i = 0; i < targets.Count; i++)
                {
                    var target = targets[i];
                    ApplyRadarReveal(board.GetCell(target.x, target.y), result);
                }
            }
            else
            {
                AddStep(result, FossickActionStepType.ToolConsumed, x, y);
                result.toolConsumed = true;
                for (var i = 0; i < targets.Count; i++)
                {
                    var target = targets[i];
                    ApplyCellEffect(board.GetCell(target.x, target.y), result, false, false, false, GetToolDamage(toolType));
                }
            }

            FinishToolOperation(board, result, toolType, x, y);
            return result;
        }

        private static void FinishToolOperation(FossickBoard board, FossickActionResult result, FossickToolType toolType, int x, int y)
        {
            if (toolType != FossickToolType.Radar)
            {
                AddFogRevealDeltas(board.RefreshFogFromOpenSpace(), result);
            }

            TryScroll(board, result, x, y);
            result.depthAfterAction = board.Depth;
        }

        private static int GetToolDamage(FossickToolType toolType)
        {
            return toolType == FossickToolType.Tnt ? 2 : 1;
        }

        private bool ApplyCellEffect(FossickCellState cell, FossickActionResult result, bool consumeTool, bool invalidWhenNoEffect, bool collectSpawnedReward, int damage)
        {
            if (cell == null)
            {
                if (invalidWhenNoEffect)
                {
                    MarkInvalid(result, result.targetX, result.targetY, "Target cell is outside the board.");
                }

                return false;
            }

            var delta = new FossickCellDelta
            {
                x = cell.x,
                y = cell.y,
                terrainBefore = cell.terrain,
                terrainAfter = cell.terrain,
                hpBefore = cell.hp,
                hpAfter = cell.hp,
                fogBefore = cell.fog,
                fogAfter = cell.fog
            };
            var changed = false;

            if (cell.IsBreakable)
            {
                if (consumeTool)
                {
                    AddStep(result, FossickActionStepType.ToolConsumed, cell.x, cell.y);
                    result.toolConsumed = true;
                }

                AddStep(result, FossickActionStepType.ObstacleHit, cell.x, cell.y);
                result.isApplied = true;

                cell.hp -= damage <= 0 ? 1 : damage;
                changed = true;
                if (cell.hp <= 0)
                {
                    var canDropSmallCoin = cell.reward == null || cell.reward.type == FossickElementType.None;
                    var brokenTerrain = cell.terrain;
                    cell.terrain = FossickTerrainType.Empty;
                    cell.hp = 0;
                    AddStep(result, FossickActionStepType.ObstacleBroken, cell.x, cell.y);
                    if (cell.fog != FossickFogType.None)
                    {
                        cell.fog = FossickFogType.None;
                        AddStep(result, FossickActionStepType.FogRevealed, cell.x, cell.y);
                    }

                    if (!delta.rewardCollected)
                    {
                        delta.rewardRevealed = cell.HasCollectableReward;
                        delta.elementRevealed = delta.rewardRevealed;
                        if (delta.rewardRevealed)
                        {
                            AddRewardRevealedStep(result, cell);
                        }
                    }

                    if (canDropSmallCoin && TryCreateSmallCoinDrop(cell, brokenTerrain, out var smallCoin))
                    {
                        cell.reward = smallCoin;
                        cell.collected = false;
                        delta.rewardRevealed = true;
                        delta.elementRevealed = true;
                        AddRewardRevealedStep(result, cell);
                    }
                }
            }
            else if (!cell.HasObstacle)
            {
                delta.rewardCollected = collectSpawnedReward && CollectReward(cell, result);
                changed = delta.rewardCollected;
                if (delta.rewardCollected)
                {
                    result.isApplied = true;
                }

                if (!delta.rewardCollected && invalidWhenNoEffect)
                {
                    MarkInvalid(result, cell.x, cell.y, "Target cell has no diggable terrain or collectable reward.");
                }
            }
            else
            {
                if (invalidWhenNoEffect)
                {
                    MarkInvalid(result, cell.x, cell.y, "Target terrain cannot be affected.");
                }
            }

            delta.terrainAfter = cell.terrain;
            delta.hpAfter = cell.hp;
            delta.fogAfter = cell.fog;
            if (changed || invalidWhenNoEffect)
            {
                result.cellDeltas.Add(delta);
            }

            return result.isApplied;
        }

        private bool TryCreateSmallCoinDrop(FossickCellState cell, FossickTerrainType brokenTerrain, out FossickElementConfig reward)
        {
            reward = null;
            if (cell == null || smallCoinDrop == null || !smallCoinDrop.enabled || smallCoinDrop.chancePerMille <= 0)
            {
                return false;
            }

            if (brokenTerrain != FossickTerrainType.Dirt && brokenTerrain != FossickTerrainType.Stone)
            {
                return false;
            }

            var random = CreateCellRandom(cell, 0x41C64E6D);
            var chance = smallCoinDrop.chancePerMille > 1000 ? 1000 : smallCoinDrop.chancePerMille;
            if (random.RangeInclusive(1, 1000) > chance)
            {
                return false;
            }

            reward = new FossickElementConfig
            {
                type = FossickElementType.Coin,
                id = string.IsNullOrEmpty(smallCoinDrop.coinId) ? "coin_pile" : smallCoinDrop.coinId,
                amount = PickSmallCoinAmount(cell)
            };
            return true;
        }

        private int PickSmallCoinAmount(FossickCellState cell)
        {
            var amounts = smallCoinDrop == null ? null : smallCoinDrop.amounts;
            if (amounts == null || amounts.Count == 0)
            {
                return 1;
            }

            var totalWeight = 0;
            for (var i = 0; i < amounts.Count; i++)
            {
                var entry = amounts[i];
                if (entry != null && entry.amount > 0 && entry.weight > 0)
                {
                    totalWeight += entry.weight;
                }
            }

            if (totalWeight <= 0)
            {
                return 1;
            }

            var random = CreateCellRandom(cell, unchecked((int)0x9E3779B9));
            var roll = random.RangeInclusive(1, totalWeight);
            var cursor = 0;
            for (var i = 0; i < amounts.Count; i++)
            {
                var entry = amounts[i];
                if (entry == null || entry.amount <= 0 || entry.weight <= 0)
                {
                    continue;
                }

                cursor += entry.weight;
                if (roll <= cursor)
                {
                    return entry.amount;
                }
            }

            return 1;
        }

        private FossickSeededRandom CreateCellRandom(FossickCellState cell, int salt)
        {
            unchecked
            {
                var mixed = seed;
                mixed = mixed * 397 ^ (cell == null ? 0 : cell.x);
                mixed = mixed * 397 ^ (cell == null ? 0 : cell.y);
                mixed = mixed * 397 ^ salt;
                return new FossickSeededRandom(seed, mixed);
            }
        }

        private static void ApplyRadarReveal(FossickCellState cell, FossickActionResult result)
        {
            if (cell == null || cell.fog == FossickFogType.None)
            {
                return;
            }

            var delta = new FossickCellDelta
            {
                x = cell.x,
                y = cell.y,
                terrainBefore = cell.terrain,
                terrainAfter = cell.terrain,
                hpBefore = cell.hp,
                hpAfter = cell.hp,
                fogBefore = cell.fog,
                fogAfter = FossickFogType.None
            };
            cell.fog = FossickFogType.None;
            result.isApplied = true;
            result.cellDeltas.Add(delta);
            AddStep(result, FossickActionStepType.RadarScanned, cell.x, cell.y);
        }

        private static void TryScroll(FossickBoard board, FossickActionResult result, int x, int y)
        {
            while (board.CanScrollDown())
            {
                CollectOutgoingTopRowBeforeScroll(board, result);
                if (!board.TryScrollDown())
                {
                    break;
                }

                AddFogRevealDeltas(board.RefreshFogFromOpenSpace(), result);
                result.scrolled = true;
                result.scrollCount++;
                AddBoardScrolledStep(result, x, y);
            }
        }

        public static void CollectOutgoingTopRowBeforeScroll(FossickBoard board, FossickActionResult result)
        {
            if (board == null || result == null)
            {
                return;
            }

            var rowIndex = board.TopVisibleRow;
            for (var x = 0; x < board.Spec.width; x++)
            {
                var cell = board.GetCellAtAbsoluteRow(x, rowIndex);
                if (cell == null || !cell.HasCollectableReward)
                {
                    continue;
                }

                if (IsMissedWhenScrolledOut(cell.reward))
                {
                    MissReward(cell, result);
                }
                else
                {
                    CollectReward(cell, result, FossickActionStepType.RewardAutoCollected);
                }
            }
        }

        public static void AddFogRevealDeltas(List<FossickFogReveal> reveals, FossickActionResult result)
        {
            if (reveals == null || result == null)
            {
                return;
            }

            for (var i = 0; i < reveals.Count; i++)
            {
                var reveal = reveals[i];
                if (reveal == null)
                {
                    continue;
                }

                result.cellDeltas.Add(new FossickCellDelta
                {
                    x = reveal.x,
                    y = reveal.y,
                    fogBefore = reveal.fogBefore,
                    fogAfter = reveal.fogAfter
                });
                AddStep(result, FossickActionStepType.FogRevealed, reveal.x, reveal.y);
            }
        }

        public static void AddBoardScrolledStep(FossickActionResult result, int x, int y)
        {
            AddStep(result, FossickActionStepType.BoardScrolled, x, y);
        }

        private void AddToolTargets(FossickBoard board, FossickToolType toolType, int x, int y, List<FossickToolTarget> targets)
        {
            if (toolType == FossickToolType.Pickaxe)
            {
                if (IsVisiblePickaxeTarget(board.GetCell(x, y)))
                {
                    AddTargetIfValid(board, x, y, targets);
                }

                return;
            }

            if (toolType == FossickToolType.Dynamite)
            {
                if (IsVisibleEmptyCell(board.GetCell(x, y)))
                {
                    AddDynamiteRowTargets(board, x, y, targets);
                }

                return;
            }

            if (toolType == FossickToolType.Tnt)
            {
                if (IsVisibleEmptyCell(board.GetCell(x, y)))
                {
                    AddConfiguredTargets(board, toolRules.tnt, x, y, targets);
                }

                return;
            }
        }

        private static bool IsVisiblePickaxeTarget(FossickCellState cell)
        {
            return cell != null && cell.fog == FossickFogType.None && (cell.IsBreakable || cell.HasSpawnedReward);
        }

        private static bool IsVisibleEmptyCell(FossickCellState cell)
        {
            return cell != null && cell.fog == FossickFogType.None && !cell.HasObstacle && !cell.HasSpawnedReward;
        }

        private static void AddConfiguredTargets(FossickBoard board, FossickToolShapeConfig shape, int x, int y, List<FossickToolTarget> targets)
        {
            if (shape == null || shape.offsets == null || shape.offsets.Count == 0)
            {
                AddTargetIfValid(board, x, y, targets);
                return;
            }

            for (var i = 0; i < shape.offsets.Count; i++)
            {
                var offset = shape.offsets[i];
                if (offset == null)
                {
                    continue;
                }

                AddTargetIfValid(board, x + offset.x, y + offset.y, targets);
            }
        }

        private static void AddDynamiteRowTargets(FossickBoard board, int x, int y, List<FossickToolTarget> targets)
        {
            AddTargetIfValid(board, x, y, targets);
            AddDynamiteDirectionTargets(board, x - 1, y, -1, targets);
            AddDynamiteDirectionTargets(board, x + 1, y, 1, targets);
        }

        private static void AddDynamiteDirectionTargets(FossickBoard board, int startX, int y, int stepX, List<FossickToolTarget> targets)
        {
            for (var currentX = startX; currentX >= 0 && currentX < board.Spec.width; currentX += stepX)
            {
                var cell = board.GetCell(currentX, y);
                if (cell == null)
                {
                    return;
                }

                if (cell.HasObstacle && !cell.IsBreakable)
                {
                    return;
                }

                AddTargetIfValid(board, currentX, y, targets);
                if (cell.IsBreakable && cell.hp > 1)
                {
                    return;
                }
            }
        }

        private static void AddVisibleWindowTargets(FossickBoard board, List<FossickToolTarget> targets)
        {
            for (var y = 0; y < board.Spec.visibleHeight; y++)
            {
                for (var x = 0; x < board.Spec.width; x++)
                {
                    AddTargetIfValid(board, x, y, targets);
                }
            }
        }

        private static void AddTargetIfValid(FossickBoard board, int x, int y, List<FossickToolTarget> targets)
        {
            if (board.GetCell(x, y) == null)
            {
                return;
            }

            targets.Add(new FossickToolTarget
            {
                x = x,
                y = y
            });
        }

        private static bool CollectReward(FossickCellState cell, FossickActionResult result, FossickActionStepType stepType = FossickActionStepType.RewardCollected)
        {
            if (cell == null || !cell.HasCollectableReward)
            {
                return false;
            }

            cell.collected = true;
            result.rewards.Add(new FossickRewardEvent
            {
                elementType = cell.reward.type,
                id = cell.reward.id,
                amount = cell.reward.amount,
                x = cell.x,
                y = cell.y
            });
            result.steps.Add(new FossickActionStep
            {
                type = stepType,
                x = cell.x,
                y = cell.y,
                elementType = cell.reward.type,
                id = cell.reward.id,
                amount = cell.reward.amount
            });
            return true;
        }

        private static bool IsMissedWhenScrolledOut(FossickElementConfig reward)
        {
            if (reward == null || reward.type != FossickElementType.Chest)
            {
                return false;
            }

            return reward.id == "locked_chest" || reward.id == "lockedChest";
        }

        private static void MissReward(FossickCellState cell, FossickActionResult result)
        {
            cell.collected = true;
            result.steps.Add(new FossickActionStep
            {
                type = FossickActionStepType.RewardMissed,
                x = cell.x,
                y = cell.y,
                elementType = cell.reward.type,
                id = cell.reward.id,
                amount = cell.reward.amount
            });
        }

        private static void AddRewardRevealedStep(FossickActionResult result, FossickCellState cell)
        {
            if (result == null || cell == null || cell.reward == null)
            {
                return;
            }

            result.steps.Add(new FossickActionStep
            {
                type = FossickActionStepType.RewardRevealed,
                x = cell.x,
                y = cell.y,
                elementType = cell.reward.type,
                id = cell.reward.id,
                amount = cell.reward.amount
            });
        }

        private static void MarkInvalid(FossickActionResult result, int x, int y, string reason)
        {
            if (result == null)
            {
                return;
            }

            result.invalidReason = reason;
            AddStep(result, FossickActionStepType.InvalidTarget, x, y);
        }

        private static void AddStep(FossickActionResult result, FossickActionStepType type, int x, int y)
        {
            if (type == FossickActionStepType.ToolConsumed)
            {
                result.isApplied = true;
                result.toolConsumed = true;
            }

            result.steps.Add(new FossickActionStep
            {
                type = type,
                x = x,
                y = y
            });
        }
    }
}
