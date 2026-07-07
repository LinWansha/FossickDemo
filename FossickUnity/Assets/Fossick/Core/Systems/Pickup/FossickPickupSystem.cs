using Fossick.Core.Application;
using Fossick.Core.Application.Results;
using Fossick.Core.Application.Events;
using Fossick.Core.Mine;

namespace Fossick.Core.Systems
{
    public sealed class FossickPickupSystem : FossickSystem
    {
        public FossickPickupSystem()
            : base("Pickup")
        {
        }

        public bool Collect(FossickCell cell, FossickActionResult result)
        {
            if (cell == null || cell.Pickup == null || cell.Pickup.Payload == null || cell.Pickup.Collected)
            {
                return false;
            }

            var pickup = cell.Pickup;
            if (!pickup.Collect())
            {
                return false;
            }

            if (result != null)
            {
                var reward = pickup.Payload.ToRewardEvent(pickup.Position);
                result.isApplied = true;
                result.isCollectOnly = true;
                result.rewards.Add(reward);
                result.steps.Add(new FossickActionStep
                {
                    type = FossickActionStepType.RewardCollected,
                    x = reward.x,
                    y = reward.y,
                    elementType = reward.elementType,
                    id = reward.id,
                    amount = reward.amount
                });
                result.domainEvents.Add(FossickDomainEvent.PickupCollected(reward.x, reward.y, reward.elementType, reward.id, reward.amount));
            }

            cell.ClearPickup();
            return true;
        }
    }
}
