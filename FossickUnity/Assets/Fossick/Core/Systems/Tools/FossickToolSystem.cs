using System.Collections.Generic;
using Fossick.Core.Application;
using Fossick.Core.Application.Results;
using Fossick.Core.Definition.Config;
using Fossick.Core.Mine;

namespace Fossick.Core.Systems
{
    public sealed class FossickToolSystem : FossickSystem
    {
        private readonly FossickToolRulesConfig toolRules;

        public FossickToolSystem(FossickToolRulesConfig toolRules)
            : base("Tool")
        {
            this.toolRules = toolRules ?? new FossickToolRulesConfig();
        }

        public IReadOnlyList<FossickToolTarget> GetTargets(FossickMine mine, FossickToolType toolType, FossickPosition target)
        {
            var targets = new List<FossickToolTarget>();
            if (mine == null)
            {
                return targets;
            }

            if (toolType == FossickToolType.Radar)
            {
                AddVisibleWindowTargets(mine, targets);
                return targets;
            }

            if (toolType == FossickToolType.Pickaxe)
            {
                var cell = mine.GetCellAtAbsoluteRow(target.x, target.y);
                if (IsVisiblePickaxeTarget(cell))
                {
                    AddTargetIfValid(mine, target.x, target.y, targets);
                }

                return targets;
            }

            if (toolType == FossickToolType.Dynamite)
            {
                if (IsVisibleEmptyCell(mine.GetCellAtAbsoluteRow(target.x, target.y)))
                {
                    AddDynamiteRowTargets(mine, target.x, target.y, targets);
                }

                return targets;
            }

            if (toolType == FossickToolType.Tnt)
            {
                if (IsVisibleEmptyCell(mine.GetCellAtAbsoluteRow(target.x, target.y)))
                {
                    AddConfiguredTargets(mine, toolRules.tnt, target.x, target.y, targets);
                }
            }

            return targets;
        }

        public static int GetToolDamage(FossickToolType toolType)
        {
            return toolType == FossickToolType.Tnt ? 2 : 1;
        }

        private static bool IsVisiblePickaxeTarget(FossickCell cell)
        {
            return cell != null && cell.IsVisible && (cell.HasDiggableTerrain || cell.HasTriggerableTerrain);
        }

        private static bool IsVisibleEmptyCell(FossickCell cell)
        {
            return cell != null && cell.IsVisible && cell.IsPassable && !cell.HasCollectablePickup;
        }

        private static void AddConfiguredTargets(FossickMine mine, FossickToolShapeConfig shape, int x, int y, List<FossickToolTarget> targets)
        {
            if (shape == null || shape.offsets == null || shape.offsets.Count == 0)
            {
                AddTargetIfValid(mine, x, y, targets);
                return;
            }

            for (var i = 0; i < shape.offsets.Count; i++)
            {
                var offset = shape.offsets[i];
                if (offset == null)
                {
                    continue;
                }

                AddTargetIfValid(mine, x + offset.x, y + offset.y, targets);
            }
        }

        private static void AddDynamiteRowTargets(FossickMine mine, int x, int y, List<FossickToolTarget> targets)
        {
            AddTargetIfValid(mine, x, y, targets);
            AddDynamiteDirectionTargets(mine, x - 1, y, -1, targets);
            AddDynamiteDirectionTargets(mine, x + 1, y, 1, targets);
        }

        private static void AddDynamiteDirectionTargets(FossickMine mine, int startX, int y, int stepX, List<FossickToolTarget> targets)
        {
            for (var currentX = startX; currentX >= 0 && currentX < mine.Spec.width; currentX += stepX)
            {
                var cell = mine.GetCellAtAbsoluteRow(currentX, y);
                if (cell == null)
                {
                    return;
                }

                if (cell.HasObstacle && !cell.HasDiggableTerrain && !cell.HasTriggerableTerrain)
                {
                    return;
                }

                AddTargetIfValid(mine, currentX, y, targets);
                if (cell.HasTriggerableTerrain || (cell.HasDiggableTerrain && cell.Terrain.Hp > 1))
                {
                    return;
                }
            }
        }

        private static void AddVisibleWindowTargets(FossickMine mine, List<FossickToolTarget> targets)
        {
            for (var y = 0; y < mine.Spec.visibleHeight; y++)
            {
                for (var x = 0; x < mine.Spec.width; x++)
                {
                    AddTargetIfValid(mine, x, mine.TopVisibleRow + y, targets);
                }
            }
        }

        private static void AddTargetIfValid(FossickMine mine, int x, int y, List<FossickToolTarget> targets)
        {
            if (mine.GetCellAtAbsoluteRow(x, y) == null)
            {
                return;
            }

            targets.Add(new FossickToolTarget
            {
                x = x,
                y = y
            });
        }
    }
}
