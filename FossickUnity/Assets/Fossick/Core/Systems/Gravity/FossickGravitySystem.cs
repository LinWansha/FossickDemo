using System;
using Fossick.Core.Application.Results;
using Fossick.Core.Mine;
using Fossick.Core.Mine.Objects;

namespace Fossick.Core.Systems
{
    public sealed class FossickGravitySystem : FossickSystem
    {
        public FossickGravitySystem()
            : base("Gravity")
        {
        }

        public void Settle(FossickMine mine, FossickActionResult result)
        {
            Settle(mine, result, null);
        }

        public void Settle(FossickMine mine, FossickActionResult result, Action<int> ensureRows)
        {
            var lastSourceRow = mine.RowCount - 1;
            for (var x = 0; x < mine.Spec.width; x++)
            {
                SettleColumn(mine, result, x, lastSourceRow, ensureRows);
            }
        }

        private static void SettleColumn(
            FossickMine mine,
            FossickActionResult result,
            int x,
            int lastSourceRow,
            Action<int> ensureRows)
        {
            for (var sourceY = lastSourceRow; sourceY >= mine.FirstLoadedRow; sourceY--)
            {
                var sourceCell = mine.GetCellAtAbsoluteRow(x, sourceY);
                if (sourceCell == null || !sourceCell.HasCollectablePickup)
                {
                    continue;
                }

                var targetY = FindLandingRow(mine, x, sourceY, ensureRows);
                if (targetY == sourceY)
                {
                    continue;
                }

                MovePickup(sourceCell, mine.GetCellAtAbsoluteRow(x, targetY), result);
            }
        }

        private static int FindLandingRow(FossickMine mine, int x, int sourceY, Action<int> ensureRows)
        {
            var targetY = sourceY;
            for (var y = sourceY + 1;; y++)
            {
                if (y >= mine.RowCount)
                {
                    if (ensureRows == null)
                    {
                        break;
                    }

                    ensureRows(y + 1);
                    if (y >= mine.RowCount)
                    {
                        break;
                    }
                }

                var cell = mine.GetCellAtAbsoluteRow(x, y);
                if (cell == null || cell.Terrain != null || cell.HasCollectablePickup)
                {
                    break;
                }

                targetY = y;
            }

            return targetY;
        }

        private static void MovePickup(FossickCell source, FossickCell target, FossickActionResult result)
        {
            var pickup = source.Pickup;
            var from = pickup.Position;
            source.ClearPickup();
            pickup.MoveTo(target.Position);
            target.SetPickup(pickup);

            if (result == null)
            {
                return;
            }

            result.entityDrops.Add(new FossickEntityDrop
            {
                entity = pickup,
                fromX = from.x,
                fromY = from.y,
                toX = target.Position.x,
                toY = target.Position.y,
                elementType = pickup.Payload.ElementType,
                id = pickup.Payload.Id
            });
        }
    }
}
