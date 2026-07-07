using System.Collections.Generic;
using System;
using Fossick.Core.Definition.Config;
using Fossick.Core.State;

namespace Fossick.Core.Generation
{
    public sealed class FossickFragmentGenerator
    {
        private readonly FossickMapConfig config;
        private readonly FossickSeededRandom random;
        private readonly FossickGenerationState state;
        private readonly List<FossickFragmentConfig> tutorialFragments = new List<FossickFragmentConfig>();
        private readonly List<FossickFragmentConfig> rewardFragments = new List<FossickFragmentConfig>();
        private readonly Dictionary<int, List<FossickFragmentConfig>> regularByDifficulty = new Dictionary<int, List<FossickFragmentConfig>>();
        private readonly Dictionary<int, FossickFragmentConfig> regularById = new Dictionary<int, FossickFragmentConfig>();
        private readonly List<FossickFragmentConfig> emptyFallbackFragments = new List<FossickFragmentConfig>();

        public FossickFragmentGenerator(FossickMapConfig config, int seed)
            : this(config, new FossickGenerationState(seed))
        {
        }

        public FossickFragmentGenerator(FossickMapConfig config, FossickGenerationState state)
        {
            this.config = config;
            this.state = state ?? new FossickGenerationState(0);
            random = new FossickSeededRandom(this.state.seed, this.state.randomState);
            BuildPools();
            EnsureRewardInterval();
        }

        public FossickGenerationState State => state;

        public List<FossickGeneratedFragment> GenerateInitialFragments()
        {
            var result = new List<FossickGeneratedFragment>();
            if (state.tutorialGenerated)
            {
                return result;
            }

            tutorialFragments.Sort((a, b) => a.id.CompareTo(b.id));
            for (var i = 0; i < tutorialFragments.Count; i++)
            {
                result.Add(CreateGenerated(tutorialFragments[i], false));
            }

            state.tutorialGenerated = true;
            CaptureRandomState();
            return result;
        }

        public FossickGeneratedFragment Next()
        {
            if (state.regularSinceLastReward >= state.nextRewardAfterRegularCount && rewardFragments.Count > 0)
            {
                state.regularSinceLastReward = 0;
                state.rewardInsertedAfterRegularCounts.Add(state.regularGeneratedCount);
                var reward = CreateGenerated(Pick(rewardFragments), true);
                ResetRewardInterval();
                CaptureRandomState();
                return reward;
            }

            var fragment = NextRegularFragment();
            state.regularSinceLastReward++;
            state.regularGeneratedCount++;
            var generated = CreateGenerated(fragment, false);
            CaptureRandomState();
            return generated;
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
                    if (!regularById.ContainsKey(fragment.id))
                    {
                        regularById.Add(fragment.id, fragment);
                    }
                }
            }
        }

        private FossickFragmentConfig NextRegularFragment()
        {
            if (state.pendingRegularFragmentIds == null)
            {
                state.pendingRegularFragmentIds = new List<int>();
            }

            if (state.pendingRegularFragmentIds.Count == 0)
            {
                RebuildRegularGroup();
            }

            var id = state.pendingRegularFragmentIds[0];
            state.pendingRegularFragmentIds.RemoveAt(0);
            if (regularById.TryGetValue(id, out var fragment))
            {
                return fragment;
            }

            return Pick(ResolveAllRegularPool());
        }

        private void RebuildRegularGroup()
        {
            var fragments = new List<FossickFragmentConfig>();
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
                        fragments.Add(Pick(ResolveRegularPool(entry.difficulty)));
                    }
                }
            }

            if (fragments.Count == 0)
            {
                fragments.Add(Pick(ResolveAllRegularPool()));
            }

            Shuffle(fragments);
            for (var i = 0; i < groupSize; i++)
            {
                var fragment = fragments[i % fragments.Count];
                state.pendingRegularFragmentIds.Add(fragment == null ? 0 : fragment.id);
            }

            state.groupIndex++;
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

        private List<FossickFragmentConfig> ResolveAllRegularPool()
        {
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
                        fog = FossickFogType.None
                    });
                }
            }

            return fragment;
        }

        private void Shuffle<T>(List<T> values)
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
            var generation = config == null ? null : config.generation;
            if (generation == null)
            {
                state.nextRewardAfterRegularCount = 1;
                return;
            }

            state.nextRewardAfterRegularCount = random.RangeInclusive(generation.rewardInsertMin, generation.rewardInsertMax);
        }

        private void EnsureRewardInterval()
        {
            if (state.nextRewardAfterRegularCount > 0)
            {
                return;
            }

            ResetRewardInterval();
            CaptureRandomState();
        }

        private FossickGeneratedFragment CreateGenerated(FossickFragmentConfig fragment, bool insertedAsReward)
        {
            if (fragment != null)
            {
                state.generatedFragmentIds.Add(fragment.id);
            }

            return new FossickGeneratedFragment(fragment, state.sequenceIndex++, insertedAsReward);
        }

        private void CaptureRandomState()
        {
            state.randomState = random.State;
        }
    }
}
