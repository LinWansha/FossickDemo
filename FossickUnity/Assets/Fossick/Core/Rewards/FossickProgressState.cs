namespace Fossick.Core.Rewards
{
    using Fossick.Core.Actions;
    using Fossick.Core.Config;

    public sealed class FossickProgressState
    {
        public int depth;
        public int oreFound;
        public int collectionFound;
        public int toolUsed;

        public void Apply(FossickActionResult result)
        {
            if (result == null)
            {
                return;
            }

            if (result.toolConsumed)
            {
                toolUsed++;
            }

            depth = result.depthAfterAction;
            for (var i = 0; i < result.rewards.Count; i++)
            {
                var reward = result.rewards[i];
                if (reward == null)
                {
                    continue;
                }

                if (reward.elementType == FossickElementType.Ore)
                {
                    oreFound += reward.amount <= 0 ? 1 : reward.amount;
                }
                else if (reward.elementType == FossickElementType.Collection)
                {
                    collectionFound += reward.amount <= 0 ? 1 : reward.amount;
                }
            }
        }

    }
}
