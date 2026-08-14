using System.Collections.Generic;
using System;
using Fossick.Core.Definition.Config;
using Fossick.Core.Data;

namespace Fossick.Core.Generation
{
    public sealed class FossickFragmentGenerator
    {
        private readonly FossickMapConfig config;
        private readonly FossickSeededRandom random;
        private readonly FossickGenerationData data;
        private readonly List<FossickFragmentConfig> tutorialFragments = new List<FossickFragmentConfig>();
        private readonly List<FossickFragmentConfig> rewardFragments = new List<FossickFragmentConfig>();
        private readonly Dictionary<int, List<FossickFragmentConfig>> regularByDifficulty = new Dictionary<int, List<FossickFragmentConfig>>();
        private readonly Dictionary<int, FossickFragmentConfig> regularById = new Dictionary<int, FossickFragmentConfig>();

        public FossickFragmentGenerator(FossickMapConfig config, int seed)
            : this(config, new FossickGenerationData(seed))
        {
        }

        public FossickFragmentGenerator(FossickMapConfig config, FossickGenerationData state)
        {
            this.config = config;
            data = state;
            random = new FossickSeededRandom(data.seed, data.randomState);
            BuildPools();
            EnsureRewardInterval();
        }

        public FossickGenerationData Data => data;

        public List<FossickGeneratedFragment> GenerateInitialFragments()
        {
            var result = new List<FossickGeneratedFragment>();
            if (data.tutorialGenerated)
            {
                return result;
            }

            tutorialFragments.Sort((a, b) => a.id.CompareTo(b.id));
            for (var i = 0; i < tutorialFragments.Count; i++)
            {
                result.Add(CreateGenerated(tutorialFragments[i], false));
            }

            data.tutorialGenerated = true;
            CaptureRandomState();
            return result;
        }

        public FossickGeneratedFragment Next()
        {
            if (CanInsertRewardFragment())
            {
                data.regularSinceLastReward = 0;
                data.rewardInsertedAfterRegularCounts.Add(data.regularGeneratedCount);
                var reward = CreateGenerated(Pick(rewardFragments), true);
                ResetRewardInterval();
                CaptureRandomState();
                return reward;
            }

            var fragment = NextRegularFragment();
            data.regularSinceLastReward++;
            data.regularGeneratedCount++;
            var generated = CreateGenerated(fragment, false);
            CaptureRandomState();
            return generated;
        }

        private bool CanInsertRewardFragment()
        {
            return rewardFragments.Count > 0
                && data.regularSinceLastReward > 0
                && data.regularSinceLastReward >= data.nextRewardAfterRegularCount;
        }

        private void BuildPools()
        {
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
            if (data.pendingRegularFragmentIds.Count == 0)
            {
                RebuildRegularGroup();
            }

            var id = data.pendingRegularFragmentIds[0];
            data.pendingRegularFragmentIds.RemoveAt(0);
            if (regularById.TryGetValue(id, out var fragment))
            {
                return fragment;
            }

            throw new InvalidOperationException($"Fossick saved regular fragment {id} is not configured.");
        }

        private void RebuildRegularGroup()
        {
            var fragments = new List<FossickFragmentConfig>();
            var generation = config.generation;
            var groupSize = generation.regularGroupSize;
            var difficultyCounts = generation.difficultyCounts;
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
                data.pendingRegularFragmentIds.Add(fragment.id);
            }

            data.groupIndex++;
        }

        private FossickFragmentConfig Pick(List<FossickFragmentConfig> fragments)
        {
            var totalWeight = 0;
            for (var i = 0; i < fragments.Count; i++)
            {
                totalWeight += fragments[i].weight;
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

                cursor += fragment.weight;
                if (roll <= cursor)
                {
                    return fragment;
                }
            }

            return fragments[fragments.Count - 1];
        }

        private List<FossickFragmentConfig> ResolveRegularPool(int difficulty)
        {
            return regularByDifficulty[difficulty];
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

            return fallbackRegulars;
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
            var generation = config.generation;
            data.nextRewardAfterRegularCount = random.RangeInclusive(generation.rewardInsertMin, generation.rewardInsertMax);
        }

        private void EnsureRewardInterval()
        {
            if (data.nextRewardAfterRegularCount > 0)
            {
                return;
            }

            ResetRewardInterval();
            CaptureRandomState();
        }

        private FossickGeneratedFragment CreateGenerated(FossickFragmentConfig fragment, bool insertedAsReward)
        {
            data.generatedFragmentIds.Add(fragment.id);

            return new FossickGeneratedFragment(fragment, data.sequenceIndex++, insertedAsReward);
        }

        private void CaptureRandomState()
        {
            data.randomState = random.State;
        }
    }
}
