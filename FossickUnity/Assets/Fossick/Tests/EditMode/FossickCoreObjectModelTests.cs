using Fossick.Core.Application;
using Fossick.Core.Application.Results;
using Fossick.Core.Definition.Config;
using Fossick.Core.Application.Events;
using Fossick.Core.Generation;
using Fossick.Core.Data;
using Fossick.Core.Mine;
using Fossick.Core.Mine.Objects;
using Fossick.Core.Systems;
using Fossick.Core.Visual.Tiling;
using NUnit.Framework;

namespace Fossick.Core.Tests
{
    public sealed class FossickCoreObjectModelTests
    {
        [Test]
        public void StoneTerrain_WhenDamaged_TracksHpAndDestroyedState()
        {
            var terrain = new FossickStoneTerrain(2, new FossickPosition(1, 2));

            Assert.That(terrain.Damage(1), Is.True);
            Assert.That(terrain.Hp, Is.EqualTo(1));
            Assert.That(terrain.IsDestroyed, Is.False);

            Assert.That(terrain.Damage(2), Is.True);
            Assert.That(terrain.Hp, Is.EqualTo(0));
            Assert.That(terrain.IsDestroyed, Is.True);
        }

        [Test]
        public void EmbeddedContent_WhenSpawned_CreatesMatchingPickupEntity()
        {
            var position = new FossickPosition(2, 3);
            var payload = new OrePayload("ore_gold", 30);
            var embedded = FossickEmbeddedContent.FromPayload(payload, "ore_gold_dirt", position);

            var pickup = embedded.SpawnPickup(position);

            Assert.That(embedded, Is.TypeOf<OreEmbeddedContent>());
            Assert.That(pickup, Is.TypeOf<OrePickupEntity>());
            Assert.That(pickup.Payload, Is.SameAs(payload));
            Assert.That(pickup.Position, Is.EqualTo(position));
        }

        [Test]
        public void FogState_WhenRevealed_OnlyChangesVisibility()
        {
            var fog = new FossickFogState(false);

            Assert.That(fog.IsVisible, Is.False);
            Assert.That(fog.Reveal(), Is.True);
            Assert.That(fog.IsVisible, Is.True);
            Assert.That(fog.Reveal(), Is.False);
        }

        [Test]
        public void RuntimeObjectFactory_WhenCellHasTerrainReward_CreatesTerrainAndEmbeddedContent()
        {
            var source = CreateCell(FossickTerrainType.Dirt, 1, FossickFogType.Covered, FossickElementType.Item, "pickaxe", 2);

            var cell = FossickRuntimeObjectFactory.CreateCell(source, source.x, source.y);

            Assert.That(cell.Terrain, Is.Not.Null);
            Assert.That(cell.FossickEmbeddedContent, Is.TypeOf<ToolEmbeddedContent>());
            Assert.That(cell.Pickup, Is.Null);
            Assert.That(cell.Fog.IsVisible, Is.False);
        }

        [Test]
        public void RuntimeObjectFactory_WhenEmptyCellHasReward_CreatesPickupEntity()
        {
            var source = CreateCell(FossickTerrainType.Empty, 0, FossickFogType.None, FossickElementType.Coin, "coin_small", 5);

            var cell = FossickRuntimeObjectFactory.CreateCell(source, source.x, source.y);

            Assert.That(cell.Terrain, Is.Null);
            Assert.That(cell.FossickEmbeddedContent, Is.Null);
            Assert.That(cell.Pickup, Is.TypeOf<CoinPickupEntity>());
            Assert.That(cell.Pickup.Payload.Amount, Is.EqualTo(5));
            Assert.That(cell.Fog.IsVisible, Is.True);
        }

        [Test]
        public void RuntimeObjectFactory_WhenEmptyCellHasNoReward_DoesNotCreatePickup()
        {
            var source = CreateCell(FossickTerrainType.Empty, 0, FossickFogType.None, FossickElementType.None, null, 0);

            var cell = FossickRuntimeObjectFactory.CreateCell(source, source.x, source.y);

            Assert.That(cell.Pickup, Is.Null);
            Assert.That(cell.FossickEmbeddedContent, Is.Null);
        }

        [Test]
        public void RuntimeObjectFactory_WhenCellHasBackgroundIds_PreservesRuntimeVisualIds()
        {
            var source = CreateCell(FossickTerrainType.Empty, 0, FossickFogType.None, FossickElementType.None, null, 0);
            source.backgroundId = "mine_variant";
            source.rewardBackgroundId = "treasure_room_7x2";

            var cell = FossickRuntimeObjectFactory.CreateCell(source, source.x, source.y);

            Assert.That(cell.BackgroundId, Is.EqualTo("mine_variant"));
            Assert.That(cell.RewardBackgroundId, Is.EqualTo("treasure_room_7x2"));
        }

        [Test]
        public void RuntimeObjectFactory_WhenGeneratedMineHasBackgroundIds_CreatesRegionLayer()
        {
            var config = FossickSampleMapFactory.CreateDefaultConfig();
            var generated = new FossickGeneratedMine();
            generated.rows.Add(new FossickGeneratedMineRow
            {
                rowIndex = 0,
                cells = new[]
                {
                    new FossickCellConfig { x = 0, y = 0, backgroundId = "mine_default", terrain = FossickTerrainType.Empty },
                    new FossickCellConfig { x = 1, y = 0, backgroundId = "mine_default", terrain = FossickTerrainType.Empty },
                    new FossickCellConfig { x = 2, y = 0, rewardBackgroundId = "treasure_room_3x2", terrain = FossickTerrainType.Empty },
                    new FossickCellConfig { x = 3, y = 0, rewardBackgroundId = "treasure_room_3x2", terrain = FossickTerrainType.Empty },
                    new FossickCellConfig { x = 4, y = 0, rewardBackgroundId = "treasure_room_3x2", terrain = FossickTerrainType.Empty },
                    new FossickCellConfig { x = 5, y = 0, terrain = FossickTerrainType.Empty },
                    new FossickCellConfig { x = 6, y = 0, terrain = FossickTerrainType.Empty }
                }
            });

            var mine = FossickRuntimeObjectFactory.CreateMine(config, generated);
            var background = mine.RegionLayer.FindAt(new FossickPosition(1, 0), FossickVisualLayer.Background);
            var rewardBackground = mine.RegionLayer.FindAt(new FossickPosition(3, 0), FossickVisualLayer.RewardBackground);

            Assert.That(mine.Rows.Count, Is.EqualTo(1));
            Assert.That(mine.Rows[0].Depth, Is.EqualTo(0));
            Assert.That(mine.Window.VisibleWidth, Is.EqualTo(config.boardWidth));
            Assert.That(mine.Window.VisibleHeight, Is.EqualTo(config.visibleHeight));
            Assert.That(background, Is.TypeOf<BackgroundRegion>());
            Assert.That(background.Bounds.width, Is.EqualTo(2));
            Assert.That(rewardBackground, Is.TypeOf<RewardBackdropRegion>());
            Assert.That(rewardBackground.Bounds.width, Is.EqualTo(3));
        }

        [Test]
        public void Mine_WhenRowsArePruned_PrunesOutOfRangeRegions()
        {
            var mine = new FossickMine(new FossickBoardSpec(3, 2));
            mine.AddRow(FossickRuntimeObjectFactory.CreateEmptyRow(mine.Spec, 0));
            mine.AddRow(FossickRuntimeObjectFactory.CreateEmptyRow(mine.Spec, 1));
            mine.AddRow(FossickRuntimeObjectFactory.CreateEmptyRow(mine.Spec, 2));
            mine.AddRegion(new BackgroundRegion("old", new FossickRect(0, 0, 3, 1), "mine_default"));
            mine.AddRegion(new BackgroundRegion("kept", new FossickRect(0, 2, 3, 1), "mine_variant"));
            mine.MoveWindowTo(1);

            mine.PruneRowsBefore(1);

            Assert.That(mine.RegionLayer.FindAt(new FossickPosition(0, 0), FossickVisualLayer.Background), Is.Null);
            Assert.That(mine.RegionLayer.FindAt(new FossickPosition(0, 2), FossickVisualLayer.Background).AssetId, Is.EqualTo("mine_variant"));
        }

        [Test]
        public void PickupSystem_WhenCollectingPickup_EmitsRewardAndDomainEventWithoutToolConsumption()
        {
            var position = new FossickPosition(3, 4);
            var cell = new FossickCell(position);
            cell.SetPickup(new CoinPickupEntity(new CoinPayload("coin_small", 7), position));
            var result = new FossickActionResult
            {
                toolType = FossickToolType.Tnt,
                targetX = position.x,
                targetY = position.y
            };

            var applied = new FossickPickupSystem().Collect(cell, result);

            Assert.That(applied, Is.True);
            Assert.That(result.isApplied, Is.True);
            Assert.That(result.isCollectOnly, Is.True);
            Assert.That(result.toolConsumed, Is.False);
            Assert.That(result.rewards.Count, Is.EqualTo(1));
            Assert.That(result.rewards[0].elementType, Is.EqualTo(FossickElementType.Coin));
            Assert.That(result.rewards[0].amount, Is.EqualTo(7));
            Assert.That(result.domainEvents.Count, Is.EqualTo(1));
            Assert.That(result.domainEvents[0].type, Is.EqualTo(FossickDomainEventType.PickupCollected));
            Assert.That(cell.HasPickup, Is.False);
        }

        [Test]
        public void PickupSystem_WhenCellHasNoPickup_ReturnsFalse()
        {
            var result = new FossickActionResult();

            var applied = new FossickPickupSystem().Collect(new FossickCell(new FossickPosition(0, 0)), result);

            Assert.That(applied, Is.False);
            Assert.That(result.isApplied, Is.False);
            Assert.That(result.rewards, Is.Empty);
            Assert.That(result.domainEvents, Is.Empty);
        }

        [Test]
        public void AutoTileResolver_WhenResolvingCorner_UsesSharedFossickMaskMapping()
        {
            var topRightBottomLeft = FossickAutoTileResolver.ResolveMask(false, true, true, false);
            var topLeftBottomRight = FossickAutoTileResolver.ResolveMask(true, false, false, true);
            var full = FossickAutoTileResolver.ResolveMask(true, true, true, true);

            Assert.That(topRightBottomLeft.mask, Is.EqualTo(6));
            Assert.That(topRightBottomLeft.spriteIndex, Is.EqualTo(9));
            Assert.That(topLeftBottomRight.mask, Is.EqualTo(9));
            Assert.That(topLeftBottomRight.spriteIndex, Is.EqualTo(6));
            Assert.That(full.spriteIndex, Is.EqualTo(15));
        }

        [Test]
        public void AutoTileResolver_WhenResolvingConfigRows_MatchesTerrainOnly()
        {
            var rows = new[]
            {
                new[] { new FossickCellConfig { x = 0, y = 0, terrain = FossickTerrainType.Dirt }, new FossickCellConfig { x = 1, y = 0, terrain = FossickTerrainType.Stone } },
                new[] { new FossickCellConfig { x = 0, y = 1, terrain = FossickTerrainType.Empty }, new FossickCellConfig { x = 1, y = 1, terrain = FossickTerrainType.Dirt } }
            };

            var result = FossickAutoTileResolver.ResolveConfigCornerAssetIndex(rows, 1, 1, FossickTerrainType.Dirt);

            Assert.That(result, Is.EqualTo(6));
        }

        private static FossickCellConfig CreateCell(FossickTerrainType terrain, int hp, FossickFogType fog, FossickElementType rewardType, string rewardId, int amount)
        {
            return new FossickCellConfig
            {
                x = 1,
                y = 2,
                terrain = terrain,
                hp = hp,
                fog = fog,
                reward = rewardType == FossickElementType.None
                    ? null
                    : new FossickElementConfig
                    {
                        type = rewardType,
                        id = rewardId,
                        amount = amount
                    }
            };
        }
    }
}
