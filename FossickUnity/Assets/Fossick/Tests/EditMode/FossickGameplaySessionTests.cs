using Fossick.Core.Config;
using Fossick.Core.Gameplay;
using NUnit.Framework;

namespace Fossick.Core.Tests
{
    public sealed class FossickGameplaySessionTests
    {
        [Test]
        public void UseTool_WhenPickaxeBreaksOre_AddsScoreAndConsumesOnePickaxe()
        {
            var config = CreateConfigWithSingleReward(FossickElementType.Ore, "copper", 15);
            config.gameplay.startingPickaxes = 2;
            var session = new FossickGameplaySession(config, 12345, 8);

            var result = session.UseTool(FossickToolType.Pickaxe, 0, 0);

            Assert.That(result.notEnoughTool, Is.False);
            Assert.That(result.action.rewards.Count, Is.EqualTo(1));
            Assert.That(session.Inventory.pickaxes, Is.EqualTo(1));
            Assert.That(session.Rewards.score, Is.EqualTo(15));
        }

        [Test]
        public void UseTool_WhenTargetIsInvalid_DoesNotConsumePickaxe()
        {
            var config = CreateConfigWithSingleReward(FossickElementType.None, null, 0);
            config.gameplay.startingPickaxes = 1;
            config.fragments[0].cells[0].terrain = FossickTerrainType.Empty;
            config.fragments[0].cells[0].hp = 0;
            var session = new FossickGameplaySession(config, 12345, 8);

            var result = session.UseTool(FossickToolType.Pickaxe, 0, 0);

            Assert.That(result.actionWasApplied, Is.False);
            Assert.That(session.Inventory.pickaxes, Is.EqualTo(1));
        }

        [Test]
        public void UseTool_WhenUnlimitedToolsEnabled_DoesNotBlockOrConsumeInventory()
        {
            var config = CreateConfigWithSingleReward(FossickElementType.Ore, "copper", 15);
            config.gameplay.startingPickaxes = 0;
            var session = new FossickGameplaySession(config, 12345, 8, true);

            var result = session.UseTool(FossickToolType.Pickaxe, 0, 0);

            Assert.That(result.notEnoughTool, Is.False);
            Assert.That(result.actionWasApplied, Is.True);
            Assert.That(session.Inventory.pickaxes, Is.EqualTo(0));
            Assert.That(session.Rewards.score, Is.EqualTo(15));
        }

        [Test]
        public void UseTool_WhenBuriedItemIsCollected_AddsMatchingToolInventory()
        {
            var config = CreateConfigWithSingleReward(FossickElementType.Item, "dynamite", 3);
            config.gameplay.startingPickaxes = 1;
            config.gameplay.startingDynamite = 0;
            var session = new FossickGameplaySession(config, 12345, 8);

            session.UseTool(FossickToolType.Pickaxe, 0, 0);

            Assert.That(session.Inventory.dynamite, Is.EqualTo(3));
        }

        [Test]
        public void UseTool_WhenAttachedPickaxeItemTerrainBreaks_ReleasesItemAfterTerrainIsDestroyed()
        {
            var config = CreateConfigWithSingleReward(FossickElementType.Item, "pickaxe", 3);
            config.gameplay.startingPickaxes = 2;
            config.fragments[0].cells[0].terrain = FossickTerrainType.Stone;
            config.fragments[0].cells[0].hp = 2;
            var session = new FossickGameplaySession(config, 12345, 8);

            Assert.That(session.Board.GetCell(0, 0).HasTerrainAttachedReward, Is.True);

            var firstHit = session.UseTool(FossickToolType.Pickaxe, 0, 0);

            Assert.That(firstHit.action.rewards, Is.Empty);
            Assert.That(session.Inventory.pickaxes, Is.EqualTo(1));
            Assert.That(session.Board.GetCell(0, 0).terrain, Is.EqualTo(FossickTerrainType.Stone));
            Assert.That(session.Board.GetCell(0, 0).hp, Is.EqualTo(1));
            Assert.That(session.Board.GetCell(0, 0).HasTerrainAttachedReward, Is.True);
            Assert.That(session.Board.GetCell(0, 0).HasRewardOverlay, Is.True);
            Assert.That(session.Board.GetCell(0, 0).collected, Is.False);

            var secondHit = session.UseTool(FossickToolType.Pickaxe, 0, 0);

            Assert.That(secondHit.action.rewards.Count, Is.EqualTo(1));
            Assert.That(secondHit.action.rewards[0].elementType, Is.EqualTo(FossickElementType.Item));
            Assert.That(secondHit.action.rewards[0].id, Is.EqualTo("pickaxe"));
            Assert.That(session.Board.GetCell(0, 0).terrain, Is.EqualTo(FossickTerrainType.Empty));
            Assert.That(session.Board.GetCell(0, 0).HasBuriedReward, Is.False);
            Assert.That(session.Board.GetCell(0, 0).HasTerrainAttachedReward, Is.False);
            Assert.That(session.Board.GetCell(0, 0).HasRewardOverlay, Is.False);
            Assert.That(session.Board.GetCell(0, 0).collected, Is.True);
            Assert.That(session.Inventory.pickaxes, Is.EqualTo(3));
        }

        [Test]
        public void SaveAndRestore_PreservesGameplayInventoryRewardsAndBoardState()
        {
            var config = CreateConfigWithSingleReward(FossickElementType.Ore, "copper", 20);
            config.gameplay.startingPickaxes = 3;
            var session = new FossickGameplaySession(config, 777, 8);
            session.UseTool(FossickToolType.Pickaxe, 0, 0);

            var save = session.CreateSaveState();
            var restored = FossickGameplaySession.Restore(config, save, 8);

            Assert.That(restored.Inventory.pickaxes, Is.EqualTo(2));
            Assert.That(restored.Rewards.score, Is.EqualTo(20));
            Assert.That(restored.Board.GetCellAtAbsoluteRow(0, 0).terrain, Is.EqualTo(FossickTerrainType.Empty));
            Assert.That(restored.Board.GetCellAtAbsoluteRow(0, 0).collected, Is.True);
        }

        [Test]
        public void NewSession_WhenInitialBottomRowHasVisibleEmptyCell_ScrollsUntilStable()
        {
            var config = CreateConfigWithInitialBottomGap();

            var session = new FossickGameplaySession(config, 12345, 12);

            Assert.That(session.Board.TopVisibleRow, Is.EqualTo(1));
            Assert.That(session.Board.Depth, Is.EqualTo(1));
            Assert.That(session.Progress.depth, Is.EqualTo(1));
            Assert.That(session.Board.CanScrollDown(), Is.False);
        }

        [Test]
        public void EndActivity_ReturnsCoreStatsAndRemainingCoins()
        {
            var config = CreateConfigWithSettlementRewards();
            config.gameplay.startingPickaxes = 4;
            var session = new FossickGameplaySession(config, 888, 8);
            session.Rewards.score = 12;
            session.Rewards.AddCollection("a", 1);
            session.Rewards.AddCollection("b", 1);
            session.Rewards.AddCollection("c", 1);
            session.Rewards.AddCollection("d", 1);
            session.Rewards.AddCollection("e", 1);

            var settlement = session.EndActivity();

            Assert.That(settlement.remainingCoinAmount, Is.EqualTo(30));
            Assert.That(settlement.collectionFound, Is.EqualTo(0));
            Assert.That(settlement.toolUsed, Is.EqualTo(0));
        }

        private static FossickMapConfig CreateConfigWithSingleReward(FossickElementType rewardType, string rewardId, int amount)
        {
            var config = new FossickMapConfig();
            config.fragments.Clear();
            config.fragments.Add(CreateFragment(1001, FossickFragmentType.Tutorial, FossickTerrainType.Dirt, rewardType, rewardId, amount));
            return config;
        }

        private static FossickMapConfig CreateConfigWithInitialBottomGap()
        {
            var config = new FossickMapConfig();
            config.fragments.Clear();
            var tutorial = CreateSolidFragment(1001, FossickFragmentType.Tutorial, 6, FossickTerrainType.Dirt);
            FillFragmentRow(tutorial, 0, FossickTerrainType.Empty, FossickFogType.None);
            SetFragmentCell(tutorial, 3, 5, FossickTerrainType.Empty, 0, FossickFogType.None);
            config.fragments.Add(tutorial);

            var regular = CreateSolidFragment(2001, FossickFragmentType.Regular, 6, FossickTerrainType.Dirt);
            regular.difficulty = 1;
            config.fragments.Add(regular);
            return config;
        }

        private static FossickMapConfig CreateConfigWithSettlementRewards()
        {
            var config = new FossickMapConfig();
            config.fragments.Clear();
            var fragment = CreateFragment(1001, FossickFragmentType.Tutorial, FossickTerrainType.Empty, FossickElementType.None, null, 0);
            fragment.cells[1].reward = new FossickElementConfig
            {
                type = FossickElementType.Coin,
                id = "coin_pile",
                amount = 30
            };
            config.fragments.Add(fragment);
            return config;
        }

        private static FossickFragmentConfig CreateFragment(int id, FossickFragmentType type, FossickTerrainType firstTerrain, FossickElementType rewardType, string rewardId, int amount)
        {
            var fragment = new FossickFragmentConfig
            {
                id = id,
                type = type,
                width = FossickBoardSpec.DefaultWidth,
                height = FossickBoardSpec.DefaultVisibleHeight
            };

            for (var y = 0; y < fragment.height; y++)
            {
                for (var x = 0; x < fragment.width; x++)
                {
                    var terrain = x == 0 && y == 0 ? firstTerrain : FossickTerrainType.Dirt;
                    fragment.cells.Add(new FossickCellConfig
                    {
                        x = x,
                        y = y,
                        terrain = terrain,
                        hp = terrain == FossickTerrainType.Empty ? 0 : 1,
                        fog = FossickFogType.None,
                        reward = x == 0 && y == 0 && rewardType != FossickElementType.None
                            ? new FossickElementConfig
                            {
                                type = rewardType,
                                id = rewardId,
                                amount = amount
                            }
                            : null
                    });
                }
            }

            return fragment;
        }

        private static FossickFragmentConfig CreateSolidFragment(int id, FossickFragmentType type, int height, FossickTerrainType terrain)
        {
            var fragment = new FossickFragmentConfig
            {
                id = id,
                type = type,
                width = FossickBoardSpec.DefaultWidth,
                height = height
            };

            for (var y = 0; y < fragment.height; y++)
            {
                for (var x = 0; x < fragment.width; x++)
                {
                    fragment.cells.Add(new FossickCellConfig
                    {
                        x = x,
                        y = y,
                        terrain = terrain,
                        hp = terrain == FossickTerrainType.Empty ? 0 : 1,
                        fog = FossickFogType.None
                    });
                }
            }

            return fragment;
        }

        private static void FillFragmentRow(FossickFragmentConfig fragment, int y, FossickTerrainType terrain, FossickFogType fog)
        {
            for (var x = 0; x < fragment.width; x++)
            {
                SetFragmentCell(fragment, x, y, terrain, terrain == FossickTerrainType.Empty ? 0 : 1, fog);
            }
        }

        private static void SetFragmentCell(FossickFragmentConfig fragment, int x, int y, FossickTerrainType terrain, int hp, FossickFogType fog)
        {
            for (var i = 0; i < fragment.cells.Count; i++)
            {
                var cell = fragment.cells[i];
                if (cell == null || cell.x != x || cell.y != y)
                {
                    continue;
                }

                cell.terrain = terrain;
                cell.hp = hp;
                cell.fog = fog;
                return;
            }
        }
    }
}
