using Fossick.Core.Application;
using Fossick.Core.Application.Results;
using Fossick.Core.State;

namespace Fossick.Core.Systems
{
    public sealed class FossickRewardSystem : FossickSystem
    {
        public FossickRewardSystem()
            : base("Reward")
        {
        }

        public void ApplyRewards(FossickActionResult action, FossickRewardState rewards, FossickInventoryState inventory)
        {
            if (action == null || rewards == null)
            {
                return;
            }

            for (var i = 0; i < action.rewards.Count; i++)
            {
                rewards.Apply(action.rewards[i], inventory);
            }
        }
    }
}
