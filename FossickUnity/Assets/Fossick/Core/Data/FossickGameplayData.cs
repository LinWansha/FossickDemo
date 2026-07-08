using System;
using Fossick.Core.Mine;

namespace Fossick.Core.Data
{
    [Serializable]
    public sealed class FossickGameplayData
    {
        public const int CurrentSchemaVersion = 1;

        public int schemaVersion = CurrentSchemaVersion;
        public int seed;
        public int boardWidth;
        public int visibleHeight;
        public FossickMineData mineData = new FossickMineData();
        public FossickInventoryData inventory = new FossickInventoryData();
        public FossickRewardData rewards = new FossickRewardData();
        public FossickProgressData progress = new FossickProgressData();
        public FossickGenerationData generation = new FossickGenerationData();

        [NonSerialized]
        private FossickMine mine;

        public FossickGameplayData()
        {
            EnsureDefaults();
        }

        public FossickGameplayData(
            int seed,
            FossickMine mine,
            FossickInventoryData inventory,
            FossickRewardData rewards,
            FossickProgressData progress,
            FossickGenerationData generation)
        {
            this.seed = seed;
            this.mine = mine;
            this.inventory = inventory ?? new FossickInventoryData();
            this.rewards = rewards ?? new FossickRewardData();
            this.progress = progress ?? new FossickProgressData();
            this.generation = generation ?? new FossickGenerationData(seed);
            boardWidth = mine == null ? 0 : mine.Spec.width;
            visibleHeight = mine == null ? 0 : mine.Spec.visibleHeight;
            EnsureDefaults();
        }

        public FossickMine Mine => mine;
        public FossickInventoryData Inventory => inventory;
        public FossickRewardData Rewards => rewards;
        public FossickProgressData Progress => progress;
        public FossickGenerationData Generation => generation;

        public void BindMine(FossickMine mine)
        {
            this.mine = mine;
            if (mine == null)
            {
                return;
            }

            boardWidth = mine.Spec.width;
            visibleHeight = mine.Spec.visibleHeight;
        }

        public void EnsureDefaults()
        {
            if (schemaVersion <= 0)
            {
                schemaVersion = CurrentSchemaVersion;
            }

            mineData ??= new FossickMineData();
            inventory ??= new FossickInventoryData();
            rewards ??= new FossickRewardData();
            progress ??= new FossickProgressData();
            generation ??= new FossickGenerationData(seed);
        }
    }
}
