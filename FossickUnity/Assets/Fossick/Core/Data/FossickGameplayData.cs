using System;
using Fossick.Core.Definition.Config;
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
            this.inventory = inventory;
            this.rewards = rewards;
            this.progress = progress;
            this.generation = generation;
            boardWidth = mine.Spec.width;
            visibleHeight = mine.Spec.visibleHeight;
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

        public void Validate()
        {
            if (schemaVersion <= 0 || seed <= 0 || boardWidth <= 0 || visibleHeight <= 0 ||
                mineData == null || inventory == null || rewards == null || progress == null || generation == null ||
                mineData.loadedRows == null || mineData.loadedRows.Count == 0 || rewards.collectionItems == null ||
                rewards.collectionDiscoveredItems == null ||
                generation.seed != seed || generation.pendingRegularFragmentIds == null ||
                generation.generatedFragmentIds == null || generation.rewardInsertedAfterRegularCounts == null ||
                mineData.loadedStartRow < 0 || mineData.topVisibleRow < mineData.loadedStartRow ||
                mineData.topVisibleRow + visibleHeight > mineData.loadedStartRow + mineData.loadedRows.Count ||
                rewards.collectionDrawCount < 0 || !HasValidRows() || !HasValidCollectionItems())
            {
                throw new InvalidOperationException("Fossick gameplay data is incomplete or does not match the current map.");
            }
        }

        private bool HasValidRows()
        {
            for (var rowIndex = 0; rowIndex < mineData.loadedRows.Count; rowIndex++)
            {
                var row = mineData.loadedRows[rowIndex];
                if (row?.cells == null)
                {
                    return false;
                }

                for (var cellIndex = 0; cellIndex < row.cells.Count; cellIndex++)
                {
                    var cell = row.cells[cellIndex];
                    if (cell == null || cell.decorations == null || cell.x < 0 || cell.x >= boardWidth ||
                        !IsValidReward(cell.reward))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        private static bool IsValidReward(FossickElementConfig reward)
        {
            if (reward == null || reward.type == FossickElementType.None)
            {
                return true;
            }

            if (string.IsNullOrEmpty(reward.id) ||
                reward.type != FossickElementType.Ore && reward.type != FossickElementType.Coin &&
                reward.type != FossickElementType.Item && reward.type != FossickElementType.Chest &&
                reward.type != FossickElementType.Collection)
            {
                return false;
            }

            return reward.type != FossickElementType.Item ||
                   FossickContentIds.Tool.TryGetType(reward.id, out _);
        }

        private bool HasValidCollectionItems()
        {
            for (var i = 0; i < rewards.collectionItems.Count; i++)
            {
                var value = rewards.collectionItems[i];
                var splitIndex = string.IsNullOrEmpty(value) ? -1 : value.LastIndexOf(':');
                if (splitIndex <= 0 || splitIndex >= value.Length - 1 ||
                    !int.TryParse(value.Substring(splitIndex + 1), out var amount) || amount < 0)
                {
                    return false;
                }
            }

            return true;
        }
    }
}
