using Fossick.Core.Application;
using Fossick.Core.Application.Results;
using Fossick.Core.Definition.Config;
using Fossick.Core.Generation;
using Fossick.Core.Mine;
using Fossick.Core.Mine.Objects;
using Fossick.Core.Data;

namespace Fossick.Core.Systems
{
    public sealed class FossickRewardSystem : FossickSystem
    {
        private readonly FossickSmallCoinDropConfig smallCoinDrop;
        private readonly int seed;

        public FossickRewardSystem(FossickSmallCoinDropConfig smallCoinDrop, int seed)
            : base("Reward")
        {
            this.smallCoinDrop = smallCoinDrop ?? new FossickSmallCoinDropConfig();
            this.seed = seed;
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

        public bool TrySpawnSmallCoinDrop(FossickCell cell, FossickTerrainType brokenTerrain, FossickActionResult result)
        {
            if (cell == null || result == null || cell.HasCollectablePickup)
            {
                return false;
            }

            if (!TryCreateSmallCoinDrop(cell.Position, brokenTerrain, out var smallCoin))
            {
                return false;
            }

            var rewardEntity = FossickPickupEntity.FromPayload(FossickRewardPayload.FromConfig(smallCoin), cell.Position);
            if (rewardEntity == null)
            {
                return false;
            }

            cell.SetPickup(rewardEntity);
            result.steps.Add(new FossickActionStep
            {
                type = FossickActionStepType.RewardRevealed,
                x = cell.Position.x,
                y = cell.Position.y,
                elementType = rewardEntity.Payload.ElementType,
                id = rewardEntity.Payload.Id,
                amount = rewardEntity.Payload.Amount
            });

            MarkRewardRevealed(result, cell.Position);
            return true;
        }

        private bool TryCreateSmallCoinDrop(FossickPosition position, FossickTerrainType brokenTerrain, out FossickElementConfig reward)
        {
            reward = null;
            if (smallCoinDrop == null || !smallCoinDrop.enabled || smallCoinDrop.chancePerMille <= 0)
            {
                return false;
            }

            if (brokenTerrain != FossickTerrainType.Dirt && brokenTerrain != FossickTerrainType.Stone)
            {
                return false;
            }

            var random = CreateCellRandom(position, 0x41C64E6D);
            var chance = smallCoinDrop.chancePerMille > 1000 ? 1000 : smallCoinDrop.chancePerMille;
            if (random.RangeInclusive(1, 1000) > chance)
            {
                return false;
            }

            reward = new FossickElementConfig
            {
                type = FossickElementType.Coin,
                id = string.IsNullOrEmpty(smallCoinDrop.coinId) ? "coin_pile" : smallCoinDrop.coinId,
                amount = PickSmallCoinAmount(position)
            };
            return true;
        }

        private int PickSmallCoinAmount(FossickPosition position)
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

            var random = CreateCellRandom(position, unchecked((int)0x9E3779B9));
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

        private FossickSeededRandom CreateCellRandom(FossickPosition position, int salt)
        {
            unchecked
            {
                var mixed = seed;
                mixed = mixed * 397 ^ position.x;
                mixed = mixed * 397 ^ position.y;
                mixed = mixed * 397 ^ salt;
                return new FossickSeededRandom(seed, mixed);
            }
        }

        private static void MarkRewardRevealed(FossickActionResult result, FossickPosition position)
        {
            for (var i = result.cellDeltas.Count - 1; i >= 0; i--)
            {
                var delta = result.cellDeltas[i];
                if (delta.x != position.x || delta.y != position.y)
                {
                    continue;
                }

                delta.rewardRevealed = true;
                delta.elementRevealed = true;
                return;
            }
        }
    }
}
