using Fossick.Core.Application;
using Fossick.Core.Application.Results;
using Fossick.Core.Application.Events;
using Fossick.Core.Definition.Config;
using Fossick.Core.Mine;
using Fossick.Core.Mine.Objects;

namespace Fossick.Core.Systems
{
    public sealed class FossickDigSystem : FossickSystem
    {
        public FossickDigSystem()
            : base("Dig")
        {
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
                        var rewardEntity = cell.FossickEmbeddedContent.SpawnPickup(cell.Position);
                        cell.SetPickup(rewardEntity);
                        AddRewardRevealedStep(result, rewardEntity);
                        result.domainEvents.Add(new FossickDomainEvent
                        {
                            type = FossickDomainEventType.EmbeddedContentRevealed,
                            x = cell.Position.x,
                            y = cell.Position.y,
                            elementType = rewardEntity.Payload.ElementType,
                            id = rewardEntity.Payload.Id,
                            amount = rewardEntity.Payload.Amount
                        });
                        cell.ClearEmbeddedContent();
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

        public bool TriggerExplosivesTerrain(FossickCell cell, FossickActionResult result)
        {
            if (cell == null || result == null || !(cell.Terrain is FossickExplosivesTerrain explosivesTerrain))
            {
                return false;
            }

            var terrainBefore = GetTerrainType(cell);
            var hpBefore = GetHp(cell);
            var visibleBefore = cell.IsVisible;

            explosivesTerrain.Damage(explosivesTerrain.Hp);
            cell.ClearTerrain();
            result.isApplied = true;
            AddStep(result, FossickActionStepType.ExplosiveCrateTriggered, cell.Position.x, cell.Position.y);
            result.domainEvents.Add(new FossickDomainEvent
            {
                type = FossickDomainEventType.ExplosiveCrateTriggered,
                x = cell.Position.x,
                y = cell.Position.y,
                id = FossickExplosivesTerrain.Id
            });
            result.cellDeltas.Add(new FossickCellDelta
            {
                x = cell.Position.x,
                y = cell.Position.y,
                terrainBefore = terrainBefore,
                terrainAfter = GetTerrainType(cell),
                hpBefore = hpBefore,
                hpAfter = GetHp(cell),
                fogBefore = visibleBefore ? FossickFogType.None : FossickFogType.Covered,
                fogAfter = cell.IsVisible ? FossickFogType.None : FossickFogType.Covered
            });

            return true;
        }

        private static void AddRewardRevealedStep(FossickActionResult result, FossickPickupEntity rewardEntity)
        {
            if (result == null || rewardEntity == null || rewardEntity.Payload == null)
            {
                return;
            }

            result.steps.Add(new FossickActionStep
            {
                type = FossickActionStepType.RewardRevealed,
                x = rewardEntity.Position.x,
                y = rewardEntity.Position.y,
                elementType = rewardEntity.Payload.ElementType,
                id = rewardEntity.Payload.Id,
                amount = rewardEntity.Payload.Amount
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
