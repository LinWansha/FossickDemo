using Fossick.Core.Actions;
using Fossick.Core.Board;
using Fossick.Core.Config;
using Fossick.Core.Generation;
using Fossick.Core.Rewards;
using Fossick.Core.Save;

namespace Fossick.Core.Gameplay
{
    public sealed class FossickGameplaySession
    {
        private readonly FossickMapConfig config;
        private readonly int seed;
        private readonly FossickActionResolver actionResolver;
        private readonly bool unlimitedTools;

        public FossickBoard Board { get; }
        public FossickInventoryState Inventory { get; }
        public FossickRewardState Rewards { get; }
        public FossickProgressState Progress { get; }

        public FossickGameplaySession(FossickMapConfig config, int seed, int initialRows)
            : this(config, seed, initialRows, false)
        {
        }

        public FossickGameplaySession(FossickMapConfig config, int seed, int initialRows, bool unlimitedTools)
        {
            this.config = config ?? FossickSampleMapFactory.CreateDefaultConfig();
            this.seed = seed;
            this.unlimitedTools = unlimitedTools;
            actionResolver = new FossickActionResolver(this.config.tools);
            Inventory = FossickInventoryState.FromConfig(this.config.gameplay);
            Rewards = new FossickRewardState();
            Progress = new FossickProgressState();
            Board = new FossickBoard(this.config.BoardSpec);
            EnsureRows(initialRows);
            StabilizeInitialBoard();
        }

        private FossickGameplaySession(FossickMapConfig config, FossickSaveState save, int initialRows, bool unlimitedTools)
            : this(config, save == null ? 0 : save.seed, initialRows, unlimitedTools)
        {
            if (save == null)
            {
                return;
            }

            EnsureRows(save.topVisibleRow + this.config.visibleHeight * 3);
            Board.ApplySaveState(save);
            Inventory.pickaxes = save.pickaxes;
            Inventory.dynamite = save.dynamite;
            Inventory.tnt = save.tnt;
            Inventory.radar = save.radar;
            Rewards.score = save.score;
            Rewards.coins = save.coins;
            Rewards.LoadCollectionSaveList(save.collectionItems);
            Progress.depth = save.depth;
            Progress.oreFound = save.oreFound;
            Progress.collectionFound = save.collectionFound;
            Progress.toolUsed = save.toolUsed;
            EnsureRows(Board.TopVisibleRow + this.config.visibleHeight * 3);
            Board.RefreshFogFromOpenSpace();
        }

        public static FossickGameplaySession Restore(FossickMapConfig config, FossickSaveState save, int initialRows)
        {
            return new FossickGameplaySession(config, save, initialRows, false);
        }

        public static FossickGameplaySession Restore(FossickMapConfig config, FossickSaveState save, int initialRows, bool unlimitedTools)
        {
            return new FossickGameplaySession(config, save, initialRows, unlimitedTools);
        }

        public FossickGameplayActionResult UseTool(FossickToolType toolType, int x, int y)
        {
            var result = new FossickGameplayActionResult
            {
                toolType = toolType,
                scoreBefore = Rewards.score,
                scoreAfter = Rewards.score
            };

            if (!unlimitedTools && !Inventory.HasTool(toolType))
            {
                result.notEnoughTool = true;
                return result;
            }

            var action = actionResolver.ResolveTool(Board, toolType, x, y);
            result.action = action;
            result.actionWasApplied = action != null && action.toolConsumed;
            if (!result.actionWasApplied)
            {
                return result;
            }

            if (!unlimitedTools)
            {
                Inventory.ConsumeTool(toolType);
            }

            Progress.Apply(action);
            ApplyRewards(action);
            result.scoreAfter = Rewards.score;
            EnsureRows(Board.TopVisibleRow + config.visibleHeight * 3);
            return result;
        }

        public FossickSaveState CreateSaveState()
        {
            var save = Board.CreateSaveState(seed, Progress);
            save.pickaxes = Inventory.pickaxes;
            save.dynamite = Inventory.dynamite;
            save.tnt = Inventory.tnt;
            save.radar = Inventory.radar;
            save.score = Rewards.score;
            save.coins = Rewards.coins;
            save.collectionItems = Rewards.CreateCollectionSaveList();
            return save;
        }

        public FossickSettlementResult EndActivity()
        {
            var settlement = new FossickSettlementResult
            {
                depth = Progress.depth,
                oreFound = Progress.oreFound,
                collectionFound = Progress.collectionFound,
                toolUsed = Progress.toolUsed,
                remainingCoinAmount = CountUncollectedCoins()
            };

            return settlement;
        }

        private void ApplyRewards(FossickActionResult action)
        {
            if (action == null)
            {
                return;
            }

            for (var i = 0; i < action.rewards.Count; i++)
            {
                Rewards.Apply(action.rewards[i], Inventory);
            }
        }

        private int CountUncollectedCoins()
        {
            var amount = 0;
            foreach (var cell in Board.EnumerateCells())
            {
                if (cell == null || cell.collected || cell.reward == null || cell.reward.type != FossickElementType.Coin)
                {
                    continue;
                }

                amount += cell.reward.amount <= 0 ? 1 : cell.reward.amount;
            }

            return amount;
        }

        private void EnsureRows(int targetRows)
        {
            if (targetRows <= Board.RowCount)
            {
                return;
            }

            Board.AppendGeneratedMine(FossickMineLayoutBuilder.Build(config, seed, targetRows));
        }

        private void StabilizeInitialBoard()
        {
            Board.RefreshFogFromOpenSpace();
            while (Board.TryScrollDown())
            {
                Board.RefreshFogFromOpenSpace();
            }

            Progress.depth = Board.Depth;
            EnsureRows(Board.TopVisibleRow + config.visibleHeight * 3);
        }

    }
}
