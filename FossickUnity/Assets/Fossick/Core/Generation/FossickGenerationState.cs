using System;
using System.Collections.Generic;

namespace Fossick.Core.Generation
{
    [Serializable]
    public sealed class FossickGenerationState
    {
        public int seed;
        public int randomState;
        public int sequenceIndex;
        public bool tutorialGenerated;
        public int regularGeneratedCount;
        public int regularSinceLastReward;
        public int nextRewardAfterRegularCount;
        public int groupIndex;
        public List<int> pendingRegularFragmentIds = new List<int>();
        public List<int> generatedFragmentIds = new List<int>();
        public List<int> rewardInsertedAfterRegularCounts = new List<int>();

        public FossickGenerationState()
        {
        }

        public FossickGenerationState(int seed)
        {
            this.seed = seed;
            randomState = seed;
        }

        public FossickGenerationState Clone()
        {
            return new FossickGenerationState
            {
                seed = seed,
                randomState = randomState,
                sequenceIndex = sequenceIndex,
                tutorialGenerated = tutorialGenerated,
                regularGeneratedCount = regularGeneratedCount,
                regularSinceLastReward = regularSinceLastReward,
                nextRewardAfterRegularCount = nextRewardAfterRegularCount,
                groupIndex = groupIndex,
                pendingRegularFragmentIds = pendingRegularFragmentIds == null ? new List<int>() : new List<int>(pendingRegularFragmentIds),
                generatedFragmentIds = generatedFragmentIds == null ? new List<int>() : new List<int>(generatedFragmentIds),
                rewardInsertedAfterRegularCounts = rewardInsertedAfterRegularCounts == null ? new List<int>() : new List<int>(rewardInsertedAfterRegularCounts)
            };
        }
    }
}
