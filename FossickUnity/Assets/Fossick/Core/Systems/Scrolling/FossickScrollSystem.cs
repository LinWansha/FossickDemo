using System;
using Fossick.Core.Application;
using Fossick.Core.Application.Results;
using Fossick.Core.Definition.Config;
using Fossick.Core.Application.Events;
using Fossick.Core.Mine;
using Fossick.Core.Mine.Objects;

namespace Fossick.Core.Systems
{
    public sealed class FossickScrollSystem : FossickSystem
    {
        private readonly FossickVisibilitySystem visibilitySystem;

        public FossickScrollSystem(FossickVisibilitySystem visibilitySystem)
            : base("Scroll")
        {
            this.visibilitySystem = visibilitySystem;
        }

        public bool TryScrollUntilStable(FossickMine mine, FossickActionResult result, int x, int y, Action afterScroll)
        {
            if (mine == null || result == null)
            {
                return false;
            }

            var scrolled = false;
            while (mine.CanScrollDown())
            {
                CollectOutgoingTopRowBeforeScroll(mine, result);
                if (!mine.TryScrollDown())
                {
                    break;
                }

                visibilitySystem.RefreshFromOpenSpace(mine, result);
                if (afterScroll != null)
                {
                    afterScroll();
                }

                scrolled = true;
                result.scrolled = true;
                result.scrollCount++;
                result.depthAfterAction = mine.Depth;
                AddMineScrolledStep(result, x, y);
                result.domainEvents.Add(FossickDomainEvent.MineScrolled(mine.Depth));
            }

            return scrolled;
        }

        public void CollectOutgoingTopRowBeforeScroll(FossickMine mine, FossickActionResult result)
        {
            if (mine == null || result == null)
            {
                return;
            }

            var rowIndex = mine.TopVisibleRow;
            for (var x = 0; x < mine.Spec.width; x++)
            {
                var cell = mine.GetCellAtAbsoluteRow(x, rowIndex);
                if (cell == null || !cell.HasCollectablePickup)
                {
                    continue;
                }

                if (IsMissedWhenScrolledOut(cell.Pickup.Payload))
                {
                    MissEntity(cell, result);
                }
                else
                {
                    CollectEntity(cell, result, FossickActionStepType.EntityAutoCollected);
                }
            }
        }

        private static bool CollectEntity(FossickCell cell, FossickActionResult result, FossickActionStepType stepType)
        {
            if (cell == null || result == null || !cell.HasCollectablePickup || cell.Pickup.Payload == null)
            {
                return false;
            }

            var pickupEntity = cell.Pickup;
            var reward = pickupEntity.Payload.ToRewardEvent(pickupEntity.Position);
            pickupEntity.Collect();
            cell.ClearPickup();
            result.rewards.Add(reward);
            result.steps.Add(new FossickActionStep
            {
                type = stepType,
                x = reward.x,
                y = reward.y,
                elementType = reward.elementType,
                id = reward.id,
                amount = reward.amount
            });
            result.domainEvents.Add(FossickDomainEvent.PickupCollected(reward.x, reward.y, reward.elementType, reward.id, reward.amount));
            return true;
        }

        private static bool IsMissedWhenScrolledOut(FossickEntityPayload payload)
        {
            if (payload == null || payload.ElementType != FossickElementType.Chest)
            {
                return false;
            }

            return payload.Id == FossickContentIds.Reward.LockedChest ||
                   payload.Id == FossickContentIds.Reward.LockedChestLegacy;
        }

        private static void MissEntity(FossickCell cell, FossickActionResult result)
        {
            if (cell == null || result == null || !cell.HasCollectablePickup || cell.Pickup.Payload == null)
            {
                return;
            }

            var pickupEntity = cell.Pickup;
            result.steps.Add(new FossickActionStep
            {
                type = FossickActionStepType.EntityMissed,
                x = pickupEntity.Position.x,
                y = pickupEntity.Position.y,
                elementType = pickupEntity.Payload.ElementType,
                id = pickupEntity.Payload.Id,
                amount = pickupEntity.Payload.Amount
            });
            pickupEntity.Collect();
            cell.ClearPickup();
        }

        private static void AddMineScrolledStep(FossickActionResult result, int x, int y)
        {
            result.steps.Add(new FossickActionStep
            {
                type = FossickActionStepType.MineScrolled,
                x = x,
                y = y
            });
        }
    }
}
