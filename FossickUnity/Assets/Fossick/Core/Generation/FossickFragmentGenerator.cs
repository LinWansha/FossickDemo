using System.Collections.Generic;
using System;
using Fossick.Core.Config;

namespace Fossick.Core.Generation
{
    public sealed class FossickFragmentGenerator
    {
        private readonly FossickMapConfig config;
        private readonly FossickSeededRandom random;
        private readonly List<FossickFragmentConfig> tutorialFragments = new List<FossickFragmentConfig>();
        private readonly List<FossickFragmentConfig> rewardFragments = new List<FossickFragmentConfig>();
        private readonly Dictionary<int, List<FossickFragmentConfig>> regularByDifficulty = new Dictionary<int, List<FossickFragmentConfig>>();
        private readonly Queue<int> difficultyQueue = new Queue<int>();
        private readonly List<FossickFragmentConfig> emptyFallbackFragments = new List<FossickFragmentConfig>();

        private int sequenceIndex;
        private int regularSinceLastReward;
        private int nextRewardAfterRegularCount;

        public FossickFragmentGenerator(FossickMapConfig config, int seed)
        {
            this.config = config;
            random = new FossickSeededRandom(seed);
            BuildPools();
            ResetRewardInterval();
        }

        public List<FossickGeneratedFragment> GenerateInitialFragments()
        {
            var result = new List<FossickGeneratedFragment>();
            tutorialFragments.Sort((a, b) => a.id.CompareTo(b.id));
            for (var i = 0; i < tutorialFragments.Count; i++)
            {
                result.Add(new FossickGeneratedFragment(tutorialFragments[i], sequenceIndex++, false));
            }

            return result;
        }

        public FossickGeneratedFragment Next()
        {
            if (regularSinceLastReward >= nextRewardAfterRegularCount && rewardFragments.Count > 0)
            {
                regularSinceLastReward = 0;
                ResetRewardInterval();
                return new FossickGeneratedFragment(Pick(rewardFragments), sequenceIndex++, true);
            }

            var difficulty = NextDifficulty();
            var fragment = Pick(ResolveRegularPool(difficulty));
            regularSinceLastReward++;
            return new FossickGeneratedFragment(fragment, sequenceIndex++, false);
        }

        private void BuildPools()
        {
            if (config == null || config.fragments == null)
            {
                return;
            }

            for (var i = 0; i < config.fragments.Count; i++)
            {
                var fragment = config.fragments[i];
                if (fragment == null)
                {
                    continue;
                }

                if (fragment.type == FossickFragmentType.Tutorial)
                {
                    tutorialFragments.Add(fragment);
                }
                else if (fragment.type == FossickFragmentType.Reward)
                {
                    rewardFragments.Add(fragment);
                }
                else if (fragment.type == FossickFragmentType.Regular)
                {
                    if (!regularByDifficulty.TryGetValue(fragment.difficulty, out var fragments))
                    {
                        fragments = new List<FossickFragmentConfig>();
                        regularByDifficulty.Add(fragment.difficulty, fragments);
                    }

                    fragments.Add(fragment);
                }
            }
        }

        private int NextDifficulty()
        {
            if (difficultyQueue.Count == 0)
            {
                RebuildDifficultyQueue();
            }

            return difficultyQueue.Dequeue();
        }

        private void RebuildDifficultyQueue()
        {
            var difficulties = new List<int>();
            var generation = config == null ? null : config.generation;
            var groupSize = generation == null ? 0 : Math.Max(1, generation.regularGroupSize);
            var difficultyCounts = generation == null ? null : generation.difficultyCounts;
            if (difficultyCounts != null)
            {
                for (var i = 0; i < difficultyCounts.Count; i++)
                {
                    var entry = difficultyCounts[i];
                    if (entry == null || entry.difficulty <= 0 || entry.count <= 0)
                    {
                        continue;
                    }

                    for (var count = 0; count < entry.count; count++)
                    {
                        difficulties.Add(entry.difficulty);
                    }
                }
            }

            if (difficulties.Count == 0)
            {
                foreach (var pair in regularByDifficulty)
                {
                    if (pair.Value != null && pair.Value.Count > 0)
                    {
                        difficulties.Add(pair.Key);
                    }
                }
            }

            if (difficulties.Count == 0)
            {
                difficulties.Add(0);
            }

            Shuffle(difficulties);
            for (var i = 0; i < groupSize; i++)
            {
                difficultyQueue.Enqueue(difficulties[i % difficulties.Count]);
            }
        }

        private FossickFragmentConfig Pick(List<FossickFragmentConfig> fragments)
        {
            if (fragments == null || fragments.Count == 0)
            {
                throw new InvalidOperationException("Cannot pick from an empty Fossick fragment pool.");
            }

            var totalWeight = 0;
            for (var i = 0; i < fragments.Count; i++)
            {
                totalWeight += fragments[i] == null ? 0 : Math.Max(1, fragments[i].weight);
            }

            var roll = random.RangeInclusive(1, totalWeight);
            var cursor = 0;
            for (var i = 0; i < fragments.Count; i++)
            {
                var fragment = fragments[i];
                if (fragment == null)
                {
                    continue;
                }

                cursor += Math.Max(1, fragment.weight);
                if (roll <= cursor)
                {
                    return fragment;
                }
            }

            return fragments[0];
        }

        private List<FossickFragmentConfig> ResolveRegularPool(int difficulty)
        {
            if (regularByDifficulty.TryGetValue(difficulty, out var exactPool) && exactPool.Count > 0)
            {
                return exactPool;
            }

            var fallbackRegulars = new List<FossickFragmentConfig>();
            foreach (var pair in regularByDifficulty)
            {
                if (pair.Value == null)
                {
                    continue;
                }

                fallbackRegulars.AddRange(pair.Value);
            }

            if (fallbackRegulars.Count > 0)
            {
                return fallbackRegulars;
            }

            if (emptyFallbackFragments.Count == 0)
            {
                emptyFallbackFragments.Add(CreateEmptyFallbackFragment());
            }

            return emptyFallbackFragments;
        }

        private FossickFragmentConfig CreateEmptyFallbackFragment()
        {
            var spec = config == null ? FossickBoardSpec.Default : config.BoardSpec;
            var fragment = new FossickFragmentConfig
            {
                id = 0,
                type = FossickFragmentType.Regular,
                width = spec.width,
                height = spec.visibleHeight,
                difficulty = 0
            };

            for (var y = 0; y < fragment.height; y++)
            {
                for (var x = 0; x < fragment.width; x++)
                {
                    fragment.cells.Add(new FossickCellConfig
                    {
                        x = x,
                        y = y,
                        terrain = FossickTerrainType.Empty,
                        hp = 0,
                        fog = FossickFogType.None,
                        mask = false
                    });
                }
            }

            return fragment;
        }

        private void Shuffle(List<int> values)
        {
            for (var i = values.Count - 1; i > 0; i--)
            {
                var swapIndex = random.RangeInclusive(0, i);
                var temp = values[i];
                values[i] = values[swapIndex];
                values[swapIndex] = temp;
            }
        }

        private void ResetRewardInterval()
        {
            var generation = config.generation;
            nextRewardAfterRegularCount = random.RangeInclusive(generation.rewardInsertMin, generation.rewardInsertMax);
        }
    }
}
