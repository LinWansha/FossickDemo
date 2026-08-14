using Fossick.Core.Application;
using Fossick.Core.Application.Results;
using Fossick.Core.Definition.Config;
using Fossick.Core.Mine;
using Fossick.Core.Mine.Objects;
using Fossick.Core.Data;

namespace Fossick.Core.Systems
{
    public sealed class FossickRewardSystem : FossickSystem
    {
        private readonly IFossickRewardProvider rewardProvider;

        public FossickRewardSystem(IFossickRewardProvider rewardProvider)
            : base("Reward")
        {
            this.rewardProvider = rewardProvider;
        }

        public void ApplyRewards(FossickActionResult action, FossickRewardData rewards, FossickInventoryData inventory)
        {
            if (action == null || rewards == null)
            {
                return;
            }

            for (var i = 0; i < action.rewards.Count; i++)
            {
                rewards.Apply(action.rewards[i], inventory);
            }
        }

        public bool TrySpawnCoinDrop(FossickCell cell, FossickTerrainType brokenTerrain, FossickActionResult result)
        {
            if (cell == null || result == null || cell.HasCollectablePickup)
            {
                return false;
            }

            if (!TryCreateCoinDrop(brokenTerrain, out var coinDrop))
            {
                return false;
            }

            var pickupEntity = FossickPickupEntity.FromPayload(
                FossickEntityPayload.FromConfig(coinDrop, rewardProvider),
                cell.Position);
            if (pickupEntity == null)
            {
                return false;
            }

            cell.SetPickup(pickupEntity);
            result.steps.Add(new FossickActionStep
            {
                type = FossickActionStepType.EntityRevealed,
                x = cell.Position.x,
                y = cell.Position.y,
                elementType = pickupEntity.Payload.ElementType,
                id = pickupEntity.Payload.Id,
                amount = pickupEntity.Payload.Amount
            });

            return true;
        }

        private bool TryCreateCoinDrop(FossickTerrainType brokenTerrain, out FossickElementConfig reward)
        {
            reward = null;
            if (brokenTerrain != FossickTerrainType.Dirt && brokenTerrain != FossickTerrainType.Stone)
            {
                return false;
            }

            if (!rewardProvider.TryPickTerrainCoinDropId(out var coinDropId))
            {
                return false;
            }

            reward = new FossickElementConfig
            {
                type = FossickElementType.Coin,
                id = coinDropId
            };
            return true;
        }

    }
}
