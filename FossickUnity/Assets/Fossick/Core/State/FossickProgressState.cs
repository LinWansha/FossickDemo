namespace Fossick.Core.State
{
    using Fossick.Core.Application.Results;
    using Fossick.Core.Definition.Config;

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
                    oreFound++;
                }
                else if (reward.elementType == FossickElementType.Collection)
                {
                    collectionFound++;
                }
            }
        }

    }
}
