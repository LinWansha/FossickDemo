using Fossick.Core.Application;
using Fossick.Core.Application.Results;
using Fossick.Core.Application.Commands;
using Fossick.Core.Definition.Config;
using Fossick.Core.Generation;
using Fossick.Core.State;
using Fossick.Core.Mine;
using Fossick.Core.Mine.Objects;
using NUnit.Framework;

namespace Fossick.Core.Tests
{
    public sealed class FossickCoreSessionTests
    {
        [Test]
        public void Session_WhenCreated_OwnsRuntimeStateAndSnapshot()
        {
            var config = FossickSampleMapFactory.CreateDefaultConfig();

            var session = new FossickGameplaySession(config, 12345);
            var snapshot = session.CreateSnapshot();

            Assert.That(session.State, Is.Not.Null);
            Assert.That(session.State.Mine, Is.TypeOf<FossickMine>());
            Assert.That(snapshot.Spec.width, Is.EqualTo(config.BoardSpec.width));
            Assert.That(snapshot.Spec.visibleHeight, Is.EqualTo(config.BoardSpec.visibleHeight));
            Assert.That(snapshot.TopVisibleRow, Is.EqualTo(0));
            Assert.That(snapshot.Depth, Is.EqualTo(0));
        }

        [Test]
        public void Session_WhenCommandIsUnsupported_ReturnsRejectedResultWithoutOldSession()
        {
            var session = new FossickGameplaySession(FossickSampleMapFactory.CreateDefaultConfig(), 12345);

            var result = session.Execute(new TestUnsupportedCommand());

            Assert.That(result.isApplied, Is.False);
            Assert.That(result.invalidReason, Is.Not.Empty);
            Assert.That(result.steps.Count, Is.EqualTo(1));
            Assert.That(result.steps[0].type, Is.EqualTo(FossickActionStepType.InvalidTarget));
        }

        [Test]
        public void Session_WhenExecutingPreviewCommand_MovesWindowToRequestedDepth()
        {
            var config = FossickSampleMapFactory.CreateDefaultConfig();
            var session = new FossickGameplaySession(config, 12345);

            var result = session.Execute(new FossickPreviewCommand(12345, 12));
            var snapshot = session.CreateSnapshot();

            Assert.That(result.isApplied, Is.True);
            Assert.That(result.toolConsumed, Is.False);
            Assert.That(snapshot.TopVisibleRow, Is.EqualTo(12));
            Assert.That(snapshot.Depth, Is.EqualTo(12));
            Assert.That(session.State.Mine.Window.TopDepth, Is.EqualTo(12));
            Assert.That(session.State.Mine.LoadedRowCount, Is.GreaterThanOrEqualTo(12 + config.visibleHeight));
        }

        [Test]
        public void RuntimeObjectFactory_WhenTerrainContainsReward_CreatesEmbeddedContentInsteadOfPickup()
        {
            var cell = FossickRuntimeObjectFactory.CreateCell(new FossickCellConfig
            {
                x = 2,
                y = 3,
                terrain = FossickTerrainType.Dirt,
                fog = FossickFogType.None,
                reward = new FossickElementConfig
                {
                    type = FossickElementType.Ore,
                    id = "ore_gold",
                    amount = 10
                }
            }, 2, 3);

            Assert.That(cell.Terrain, Is.Not.Null);
            Assert.That(cell.FossickEmbeddedContent, Is.TypeOf<OreEmbeddedContent>());
            Assert.That(cell.Pickup, Is.Null);
        }

        [Test]
        public void RuntimeObjectFactory_WhenOpenCellContainsReward_CreatesPickup()
        {
            var cell = FossickRuntimeObjectFactory.CreateCell(new FossickCellConfig
            {
                x = 2,
                y = 3,
                terrain = FossickTerrainType.Empty,
                fog = FossickFogType.None,
                reward = new FossickElementConfig
                {
                    type = FossickElementType.Coin,
                    id = "coin_small",
                    amount = 5
                }
            }, 2, 3);

            Assert.That(cell.Terrain, Is.Null);
            Assert.That(cell.FossickEmbeddedContent, Is.Null);
            Assert.That(cell.Pickup, Is.TypeOf<CoinPickupEntity>());
        }

        [Test]
        public void Mine_WhenBuiltFromGeneratedRows_ExposesVisibleWindowWithoutBoard()
        {
            var config = FossickSampleMapFactory.CreateDefaultConfig();
            var generated = FossickMineLayoutBuilder.Build(config, 12345, config.visibleHeight + 1);

            var mine = FossickRuntimeObjectFactory.CreateMine(config, generated);
            var visibleRows = mine.GetVisibleRows();

            Assert.That(visibleRows.Count, Is.EqualTo(config.visibleHeight));
            Assert.That(mine.LoadedRowCount, Is.EqualTo(config.visibleHeight + 1));
            Assert.That(mine.GetCell(0, 0), Is.Not.Null);
        }

        [Test]
        public void Mine_WhenOpenSpaceTouchesFog_RevealsConnectedCellsAndAdjacentObstacles()
        {
            var mine = new FossickMine(new FossickBoardSpec(3, 3));
            mine.AddRow(FossickRuntimeObjectFactory.CreateEmptyRow(mine.Spec, 0));
            mine.AddRow(FossickRuntimeObjectFactory.CreateEmptyRow(mine.Spec, 1));
            mine.AddRow(FossickRuntimeObjectFactory.CreateEmptyRow(mine.Spec, 2));

            mine.GetCellAtAbsoluteRow(1, 0).Fog.Cover();
            mine.GetCellAtAbsoluteRow(1, 1).Fog.Cover();
            mine.GetCellAtAbsoluteRow(2, 1).Fog.Cover();
            mine.GetCellAtAbsoluteRow(2, 1).Terrain = new FossickTerrainInstance(FossickTerrainType.Dirt, 1, new FossickPosition(2, 1));

            var reveals = mine.RefreshFogFromOpenSpace();

            Assert.That(reveals.Count, Is.GreaterThanOrEqualTo(3));
            Assert.That(mine.GetCellAtAbsoluteRow(1, 0).IsVisible, Is.True);
            Assert.That(mine.GetCellAtAbsoluteRow(1, 1).IsVisible, Is.True);
            Assert.That(mine.GetCellAtAbsoluteRow(2, 1).IsVisible, Is.True);
        }

        [Test]
        public void Mine_WhenTopRowsAreClearAndPathReachesBottom_CanScrollDown()
        {
            var mine = new FossickMine(new FossickBoardSpec(3, 3));
            for (var y = 0; y < 4; y++)
            {
                mine.AddRow(FossickRuntimeObjectFactory.CreateEmptyRow(mine.Spec, y));
            }

            Assert.That(mine.CanScrollDown(), Is.True);
            Assert.That(mine.TryScrollDown(), Is.True);
            Assert.That(mine.TopVisibleRow, Is.EqualTo(1));
            Assert.That(mine.Depth, Is.EqualTo(1));
        }

        [Test]
        public void Mine_WhenPruningRowsBehind_KeepsAbsoluteCoordinates()
        {
            var mine = new FossickMine(new FossickBoardSpec(3, 3));
            for (var y = 0; y < 6; y++)
            {
                mine.AddRow(FossickRuntimeObjectFactory.CreateEmptyRow(mine.Spec, y));
            }

            Assert.That(mine.TryScrollDown(), Is.True);
            mine.PruneRowsBefore(1);

            Assert.That(mine.FirstLoadedRow, Is.EqualTo(1));
            Assert.That(mine.GetCellAtAbsoluteRow(0, 0), Is.Null);
            Assert.That(mine.GetCellAtAbsoluteRow(0, 1).Position.y, Is.EqualTo(1));
        }

        [Test]
        public void Session_WhenPickaxeBreaksEmbeddedOre_SpawnsPickupWithoutCollecting()
        {
            var session = new FossickGameplaySession(FossickSampleMapFactory.CreateDefaultConfig(), 12345);
            var position = new FossickPosition(0, 0);
            var cell = session.State.Mine.GetCellAtAbsoluteRow(position.x, position.y);
            cell.Fog = new FossickFogState(true);
            cell.Terrain = new FossickTerrainInstance(FossickTerrainType.Dirt, 1, position);
            cell.FossickEmbeddedContent = FossickEmbeddedContent.FromPayload(new OrePayload("ore_gold", 10), "ore_gold_dirt", position);
            var pickaxesBefore = session.State.Inventory.pickaxes;

            var result = session.Execute(new FossickUseToolCommand(FossickToolType.Pickaxe, position));

            Assert.That(result.isApplied, Is.True);
            Assert.That(result.toolConsumed, Is.True);
            Assert.That(session.State.Inventory.pickaxes, Is.EqualTo(pickaxesBefore - 1));
            Assert.That(session.State.Rewards.score, Is.EqualTo(0));
            Assert.That(cell.Terrain, Is.Null);
            Assert.That(cell.FossickEmbeddedContent, Is.Null);
            Assert.That(cell.Pickup, Is.TypeOf<OrePickupEntity>());
        }

        [Test]
        public void Session_WhenClickingPickupWithToolSelected_CollectsWithoutConsumingTool()
        {
            var session = new FossickGameplaySession(FossickSampleMapFactory.CreateDefaultConfig(), 12345);
            var position = new FossickPosition(0, 0);
            var cell = session.State.Mine.GetCellAtAbsoluteRow(position.x, position.y);
            cell.Fog = new FossickFogState(true);
            cell.Terrain = null;
            cell.SetPickup(new CoinPickupEntity(new CoinPayload("coin_pile", 5), position));
            var pickaxesBefore = session.State.Inventory.pickaxes;

            var result = session.Execute(new FossickUseToolCommand(FossickToolType.Pickaxe, position));

            Assert.That(result.isApplied, Is.True);
            Assert.That(result.isCollectOnly, Is.True);
            Assert.That(result.toolConsumed, Is.False);
            Assert.That(session.State.Inventory.pickaxes, Is.EqualTo(pickaxesBefore));
            Assert.That(session.State.Rewards.coins, Is.EqualTo(5));
            Assert.That(cell.Pickup, Is.Null);
        }

        [Test]
        public void Session_WhenCollectingOre_UpdatesScoreAndFoundCountSeparately()
        {
            var session = new FossickGameplaySession(FossickSampleMapFactory.CreateDefaultConfig(), 12345);
            var position = new FossickPosition(0, 0);
            var cell = session.State.Mine.GetCellAtAbsoluteRow(position.x, position.y);
            cell.Fog = new FossickFogState(true);
            cell.Terrain = null;
            cell.SetPickup(new OrePickupEntity(new OrePayload("ore_gold", 10), position));

            var result = session.Execute(new FossickUseToolCommand(FossickToolType.Pickaxe, position));

            Assert.That(result.isApplied, Is.True);
            Assert.That(result.isCollectOnly, Is.True);
            Assert.That(result.toolConsumed, Is.False);
            Assert.That(session.State.Rewards.score, Is.EqualTo(10));
            Assert.That(session.State.Progress.oreFound, Is.EqualTo(1));
            Assert.That(session.State.Progress.toolUsed, Is.EqualTo(0));
        }

        [Test]
        public void Session_WhenToolConsumesItem_UpdatesToolUsedCount()
        {
            var session = new FossickGameplaySession(FossickSampleMapFactory.CreateDefaultConfig(), 12345);
            var position = new FossickPosition(0, 0);
            var cell = session.State.Mine.GetCellAtAbsoluteRow(position.x, position.y);
            cell.Fog = new FossickFogState(true);
            cell.Terrain = new FossickTerrainInstance(FossickTerrainType.Dirt, 1, position);

            var result = session.Execute(new FossickUseToolCommand(FossickToolType.Pickaxe, position));

            Assert.That(result.isApplied, Is.True);
            Assert.That(result.toolConsumed, Is.True);
            Assert.That(session.State.Progress.toolUsed, Is.EqualTo(1));
        }

        [Test]
        public void Session_WhenCreatingSettlementResult_UsesCurrentProgressAndRewards()
        {
            var session = new FossickGameplaySession(FossickSampleMapFactory.CreateDefaultConfig(), 12345);
            var position = new FossickPosition(0, 0);
            var cell = session.State.Mine.GetCellAtAbsoluteRow(position.x, position.y);
            cell.Fog = new FossickFogState(true);
            cell.Terrain = null;
            cell.SetPickup(new CoinPickupEntity(new CoinPayload("coin_pile", 5), position));

            session.Execute(new FossickUseToolCommand(FossickToolType.Pickaxe, position));
            var settlement = session.CreateSettlementResult();

            Assert.That(settlement.depth, Is.EqualTo(session.State.Mine.Depth));
            Assert.That(settlement.remainingCoinAmount, Is.EqualTo(5));
            Assert.That(settlement.toolUsed, Is.EqualTo(0));
        }

        [Test]
        public void Session_WhenRadarIsUsed_RevealsVisibleWindowFog()
        {
            var session = new FossickGameplaySession(FossickSampleMapFactory.CreateDefaultConfig(), 12345);
            var coveredCell = session.State.Mine.GetCellAtAbsoluteRow(0, 1);
            coveredCell.Fog = new FossickFogState(false);
            var radarBefore = session.State.Inventory.radar;

            var result = session.Execute(new FossickUseToolCommand(FossickToolType.Radar, new FossickPosition(0, 0)));

            Assert.That(result.isApplied, Is.True);
            Assert.That(result.toolConsumed, Is.True);
            Assert.That(session.State.Inventory.radar, Is.EqualTo(radarBefore - 1));
            Assert.That(coveredCell.IsVisible, Is.True);
        }

        [Test]
        public void Session_WhenToolTargetIsCovered_DoesNotConsumeOrScroll()
        {
            var session = new FossickGameplaySession(FossickSampleMapFactory.CreateDefaultConfig(), 12345);
            var position = new FossickPosition(0, 1);
            var cell = session.State.Mine.GetCellAtAbsoluteRow(position.x, position.y);
            cell.Fog = new FossickFogState(false);
            cell.Terrain = new FossickTerrainInstance(FossickTerrainType.Dirt, 1, position);
            var pickaxesBefore = session.State.Inventory.pickaxes;
            var depthBefore = session.State.Mine.Depth;

            var result = session.Execute(new FossickUseToolCommand(FossickToolType.Pickaxe, position));

            Assert.That(result.isApplied, Is.False);
            Assert.That(result.toolConsumed, Is.False);
            Assert.That(session.State.Inventory.pickaxes, Is.EqualTo(pickaxesBefore));
            Assert.That(session.State.Mine.Depth, Is.EqualTo(depthBefore));
        }

        [Test]
        public void GenerationSystem_WhenEnsuringRuntimeRows_AppendsRowsToMine()
        {
            var config = FossickSampleMapFactory.CreateDefaultConfig();
            var system = new Systems.FossickGenerationSystem(config);
            var mine = new FossickMine(config.BoardSpec);
            var state = new FossickGenerationState(12345);

            system.EnsureRows(mine, state, 12);

            Assert.That(mine.RowCount, Is.EqualTo(12));
            Assert.That(mine.LoadedRowCount, Is.EqualTo(12));
            Assert.That(mine.GetCellAtAbsoluteRow(0, 11), Is.Not.Null);
        }

        [Test]
        public void GenerationSystem_WhenSeedIsSame_GeneratesSameRuntimeMine()
        {
            var config = FossickSampleMapFactory.CreateDefaultConfig();
            var system = new Systems.FossickGenerationSystem(config);
            var leftMine = new FossickMine(config.BoardSpec);
            var rightMine = new FossickMine(config.BoardSpec);

            system.EnsureRows(leftMine, new FossickGenerationState(23456), 18);
            system.EnsureRows(rightMine, new FossickGenerationState(23456), 18);

            for (var y = 0; y < 18; y++)
            {
                for (var x = 0; x < config.boardWidth; x++)
                {
                    Assert.That(GetTerrain(leftMine, x, y), Is.EqualTo(GetTerrain(rightMine, x, y)));
                    Assert.That(GetPickupId(leftMine, x, y), Is.EqualTo(GetPickupId(rightMine, x, y)));
                    Assert.That(GetEmbeddedId(leftMine, x, y), Is.EqualTo(GetEmbeddedId(rightMine, x, y)));
                }
            }
        }

        [Test]
        public void Session_WhenCreated_PrefetchesRowsForInfiniteMine()
        {
            var config = FossickSampleMapFactory.CreateDefaultConfig();

            var session = new FossickGameplaySession(config, 12345);

            Assert.That(session.State.Mine.RowCount, Is.GreaterThan(config.visibleHeight));
            Assert.That(session.State.Generation.sequenceIndex, Is.GreaterThan(0));
        }

        private static FossickTerrainType GetTerrain(FossickMine mine, int x, int y)
        {
            var cell = mine.GetCellAtAbsoluteRow(x, y);
            return cell == null || cell.Terrain == null ? FossickTerrainType.Empty : cell.Terrain.Terrain;
        }

        private static string GetPickupId(FossickMine mine, int x, int y)
        {
            var cell = mine.GetCellAtAbsoluteRow(x, y);
            return cell == null || cell.Pickup == null || cell.Pickup.Payload == null ? null : cell.Pickup.Payload.Id;
        }

        private static string GetEmbeddedId(FossickMine mine, int x, int y)
        {
            var cell = mine.GetCellAtAbsoluteRow(x, y);
            return cell == null || cell.FossickEmbeddedContent == null || cell.FossickEmbeddedContent.Payload == null ? null : cell.FossickEmbeddedContent.Payload.Id;
        }

        private sealed class TestUnsupportedCommand : FossickCommand
        {
            public TestUnsupportedCommand()
                : base("unsupported_test")
            {
            }
        }
    }
}
