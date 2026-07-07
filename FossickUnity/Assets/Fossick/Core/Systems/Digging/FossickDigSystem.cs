using Fossick.Core.Application;
using Fossick.Core.Application.Results;
using Fossick.Core.Definition.Config;
using Fossick.Core.Generation;
using Fossick.Core.State;
using Fossick.Core.Application.Events;
using Fossick.Core.Mine;
using Fossick.Core.Mine.Objects;

namespace Fossick.Core.Systems
{
    public sealed class FossickDigSystem : FossickSystem
    {
        private readonly FossickSmallCoinDropConfig smallCoinDrop;
        private readonly int seed;

        public FossickDigSystem(FossickSmallCoinDropConfig smallCoinDrop, int seed)
            : base("Dig")
        {
            this.smallCoinDrop = smallCoinDrop ?? new FossickSmallCoinDropConfig();
            this.seed = seed;
        }

        public bool ApplyCellEffect(FossickCell cell, FossickActionResult result, bool invalidWhenNoEffect, int damage)
        {
            if (cell == null)
            {
                if (invalidWhenNoEffect)
                {
                    MarkInvalid(result, result.targetX, result.targetY, "Target cell is outside the mine.");
                }

                return false;
            }

            var terrainBefore = GetTerrainType(cell);
            var hpBefore = GetHp(cell);
            var visibleBefore = cell.IsVisible;
            var changed = false;
            var applied = false;

            if (cell.HasDiggableTerrain)
            {
                AddStep(result, FossickActionStepType.ObstacleHit, cell.Position.x, cell.Position.y);
                result.domainEvents.Add(new FossickDomainEvent
                {
                    type = FossickDomainEventType.TerrainDamaged,
                    x = cell.Position.x,
                    y = cell.Position.y
                });

                cell.Terrain.Damage(damage <= 0 ? 1 : damage);
                changed = true;
                applied = true;
                result.isApplied = true;

                if (cell.Terrain.IsDestroyed)
                {
                    var brokenTerrain = cell.Terrain.Terrain;
                    cell.ClearTerrain();
                    AddStep(result, FossickActionStepType.ObstacleBroken, cell.Position.x, cell.Position.y);
                    result.domainEvents.Add(new FossickDomainEvent
                    {
                        type = FossickDomainEventType.TerrainDestroyed,
                        x = cell.Position.x,
                        y = cell.Position.y
                    });

                    if (cell.FossickEmbeddedContent != null)
                    {
                        var pickup = cell.FossickEmbeddedContent.SpawnPickup(cell.Position);
                        cell.SetPickup(pickup);
                        AddRewardRevealedStep(result, pickup);
                        result.domainEvents.Add(new FossickDomainEvent
                        {
                            type = FossickDomainEventType.EmbeddedContentRevealed,
                            x = cell.Position.x,
                            y = cell.Position.y,
                            elementType = pickup.Payload.ElementType,
                            id = pickup.Payload.Id,
                            amount = pickup.Payload.Amount
                        });
                        cell.ClearEmbeddedContent();
                    }
                    else if (TryCreateSmallCoinDrop(cell.Position, brokenTerrain, out var smallCoin))
                    {
                        var pickup = FossickPickupEntity.FromPayload(FossickRewardPayload.FromConfig(smallCoin), cell.Position);
                        cell.SetPickup(pickup);
                        AddRewardRevealedStep(result, pickup);
                    }
                }
            }
            else if (invalidWhenNoEffect)
            {
                MarkInvalid(result, cell.Position.x, cell.Position.y, "Target cell has no diggable terrain.");
            }

            if (changed || invalidWhenNoEffect)
            {
                result.cellDeltas.Add(new FossickCellDelta
                {
                    x = cell.Position.x,
                    y = cell.Position.y,
                    terrainBefore = terrainBefore,
                    terrainAfter = GetTerrainType(cell),
                    hpBefore = hpBefore,
                    hpAfter = GetHp(cell),
                    fogBefore = visibleBefore ? FossickFogType.None : FossickFogType.Covered,
                    fogAfter = cell.IsVisible ? FossickFogType.None : FossickFogType.Covered,
                    rewardRevealed = cell.HasCollectablePickup,
                    elementRevealed = cell.HasCollectablePickup
                });
            }

            return applied;
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

        private static void AddRewardRevealedStep(FossickActionResult result, FossickPickupEntity pickup)
        {
            if (result == null || pickup == null || pickup.Payload == null)
            {
                return;
            }

            result.steps.Add(new FossickActionStep
            {
                type = FossickActionStepType.RewardRevealed,
                x = pickup.Position.x,
                y = pickup.Position.y,
                elementType = pickup.Payload.ElementType,
                id = pickup.Payload.Id,
                amount = pickup.Payload.Amount
            });
        }

        private static FossickTerrainType GetTerrainType(FossickCell cell)
        {
            return cell == null || cell.Terrain == null ? FossickTerrainType.Empty : cell.Terrain.Terrain;
        }

        private static int GetHp(FossickCell cell)
        {
            return cell == null || cell.Terrain == null ? 0 : cell.Terrain.Hp;
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
