using Fossick.Core.Mine;

namespace Fossick.Core.State
{
    public sealed class FossickRuntimeState
    {
        public FossickRuntimeState(FossickMine mine, FossickInventoryState inventory, FossickRewardState rewards, FossickProgressState progress, FossickGenerationState generation)
        {
            Mine = mine;
            Inventory = inventory;
            Rewards = rewards;
            Progress = progress;
            Generation = generation;
        }

        public FossickMine Mine { get; }
        public FossickInventoryState Inventory { get; }
        public FossickRewardState Rewards { get; }
        public FossickProgressState Progress { get; }
        public FossickGenerationState Generation { get; }
    }
}
