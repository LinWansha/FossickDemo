using Fossick.Core.Definition.Config;
using Fossick.Core.Generation;
using Fossick.Core.Data;
using NUnit.Framework;

namespace Fossick.Core.Tests
{
    public sealed class FossickMineLayoutTests
    {
        [Test]
        public void Build_AppliesRowOverrideAsMapContent()
        {
            var config = FossickSampleMapFactory.CreateDefaultConfig();
            var replacement = CreateFlatFragment(9001, FossickTerrainType.Empty, 2);
            config.generation.rowOverrides.Add(new FossickRowOverrideConfig
            {
                startRow = 7,
                fragment = replacement
            });

            var mine = FossickMineLayoutBuilder.Build(config, 12345, 14);

            Assert.That(mine.rows[7].cells[0].terrain, Is.EqualTo(FossickTerrainType.Empty));
            Assert.That(mine.rows[8].cells[0].terrain, Is.EqualTo(FossickTerrainType.Empty));
        }

        [Test]
        public void Build_WhenRowOverridesOverlap_LaterOverrideWins()
        {
            var config = FossickSampleMapFactory.CreateDefaultConfig();
            config.generation.rowOverrides.Add(new FossickRowOverrideConfig
            {
                startRow = 6,
                fragment = CreateFlatFragment(9001, FossickTerrainType.Dirt, 3)
            });
            config.generation.rowOverrides.Add(new FossickRowOverrideConfig
            {
                startRow = 7,
                fragment = CreateFlatFragment(9002, FossickTerrainType.Stone, 1)
            });

            var mine = FossickMineLayoutBuilder.Build(config, 12345, 14);

            Assert.That(mine.rows[6].cells[0].terrain, Is.EqualTo(FossickTerrainType.Dirt));
            Assert.That(mine.rows[7].cells[0].terrain, Is.EqualTo(FossickTerrainType.Stone));
            Assert.That(mine.rows[8].cells[0].terrain, Is.EqualTo(FossickTerrainType.Dirt));
        }

        [Test]
        public void Generator_UsesConfiguredDifficultyCountsWithinEachRegularGroup()
        {
            var config = FossickSampleMapFactory.CreateDefaultConfig();
            config.generation.regularGroupSize = 5;
            config.generation.difficultyCounts.Clear();
            config.generation.difficultyCounts.Add(new FossickDifficultyCount { difficulty = 1, count = 3 });
            config.generation.difficultyCounts.Add(new FossickDifficultyCount { difficulty = 2, count = 1 });
            config.generation.difficultyCounts.Add(new FossickDifficultyCount { difficulty = 3, count = 1 });
            config.generation.rewardInsertMin = 99;
            config.generation.rewardInsertMax = 99;

            var generator = new FossickFragmentGenerator(config, 12345);
            generator.GenerateInitialFragments();

            var difficulty1 = 0;
            var difficulty2 = 0;
            var difficulty3 = 0;
            for (var i = 0; i < config.generation.regularGroupSize; i++)
            {
                var generated = generator.Next();
                Assert.That(generated.insertedAsReward, Is.False);
                if (generated.config.difficulty == 1)
                {
                    difficulty1++;
                }
                else if (generated.config.difficulty == 2)
                {
                    difficulty2++;
                }
                else if (generated.config.difficulty == 3)
                {
                    difficulty3++;
                }
            }

            Assert.That(difficulty1, Is.EqualTo(3));
            Assert.That(difficulty2, Is.EqualTo(1));
            Assert.That(difficulty3, Is.EqualTo(1));
        }

        [Test]
        public void Generator_InsertsRewardFragmentsAfterConfiguredRegularCount()
        {
            var config = FossickSampleMapFactory.CreateDefaultConfig();
            config.generation.rewardInsertMin = 2;
            config.generation.rewardInsertMax = 2;

            var generator = new FossickFragmentGenerator(config, 12345);
            generator.GenerateInitialFragments();

            Assert.That(generator.Next().insertedAsReward, Is.False);
            Assert.That(generator.Next().insertedAsReward, Is.False);

            var reward = generator.Next();
            Assert.That(reward.insertedAsReward, Is.True);
            Assert.That(reward.config.type, Is.EqualTo(FossickFragmentType.Reward));
        }

        [Test]
        public void Generator_WhenDifficultyCountsDoNotFillGroup_RepeatsAvailableDifficultiesWithoutCrashing()
        {
            var config = FossickSampleMapFactory.CreateDefaultConfig();
            config.generation.regularGroupSize = 4;
            config.generation.difficultyCounts.Clear();
            config.generation.difficultyCounts.Add(new FossickDifficultyCount { difficulty = 1, count = 1 });
            config.generation.rewardInsertMin = 99;
            config.generation.rewardInsertMax = 99;

            var generator = new FossickFragmentGenerator(config, 12345);
            generator.GenerateInitialFragments();

            for (var i = 0; i < config.generation.regularGroupSize; i++)
            {
                var generated = generator.Next();
                Assert.That(generated.insertedAsReward, Is.False);
                Assert.That(generated.config.type, Is.EqualTo(FossickFragmentType.Regular));
                Assert.That(generated.config.difficulty, Is.EqualTo(1));
            }
        }

        [Test]
        public void BuildAdditional_MatchesSinglePassGenerationWhenAppendedInChunks()
        {
            var config = FossickSampleMapFactory.CreateDefaultConfig();
            var full = FossickMineLayoutBuilder.Build(config, 2468, 36);

            var state = new FossickGenerationData(2468);
            var first = FossickMineLayoutBuilder.BuildAdditional(config, state, 12, 0, null);
            var second = FossickMineLayoutBuilder.BuildAdditional(config, state, 36 - first.rows.Count, first.rows.Count, null);

            var appendedRows = first.rows.Count + second.rows.Count;
            Assert.That(appendedRows, Is.GreaterThanOrEqualTo(36));
            for (var y = 0; y < 36; y++)
            {
                var row = y < first.rows.Count ? first.rows[y] : second.rows[y - first.rows.Count];
                Assert.That(row.fragment.fragmentId, Is.EqualTo(full.rows[y].fragment.fragmentId));
                Assert.That(row.cells[0].terrain, Is.EqualTo(full.rows[y].cells[0].terrain));
            }
        }

        [Test]
        public void Generator_DataSnapshotContinuesWithTheSameFragmentSequence()
        {
            var config = FossickSampleMapFactory.CreateDefaultConfig();
            config.generation.rewardInsertMin = 99;
            config.generation.rewardInsertMax = 99;

            var generator = new FossickFragmentGenerator(config, 13579);
            generator.GenerateInitialFragments();
            generator.Next();
            generator.Next();

            var snapshot = generator.Data.Clone();
            var expected = generator.Next();
            var restoredGenerator = new FossickFragmentGenerator(config, snapshot);
            var actual = restoredGenerator.Next();

            Assert.That(actual.config.id, Is.EqualTo(expected.config.id));
            Assert.That(actual.sequenceIndex, Is.EqualTo(expected.sequenceIndex));
            Assert.That(actual.insertedAsReward, Is.EqualTo(expected.insertedAsReward));
        }

        private static FossickFragmentConfig CreateFlatFragment(int id, FossickTerrainType terrain, int height)
        {
            var fragment = new FossickFragmentConfig
            {
                id = id,
                type = FossickFragmentType.Regular,
                difficulty = 1,
                width = FossickBoardSpec.DefaultWidth,
                height = height
            };

            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < fragment.width; x++)
                {
                    fragment.cells.Add(new FossickCellConfig
                    {
                        x = x,
                        y = y,
                        terrain = terrain,
                        hp = terrain == FossickTerrainType.Stone ? 2 : terrain == FossickTerrainType.Dirt ? 1 : 0,
                        fog = terrain == FossickTerrainType.Empty ? FossickFogType.None : FossickFogType.Covered
                    });
                }
            }

            return fragment;
        }
    }
}
