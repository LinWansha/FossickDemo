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
        private FossickGenerationState generationState;

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
            generationState = new FossickGenerationState(seed);
            actionResolver = new FossickActionResolver(this.config.tools);
            Inventory = FossickInventoryState.FromConfig(this.config.gameplay);
            Rewards = new FossickRewardState();
            Progress = new FossickProgressState();
            Board = new FossickBoard(this.config.BoardSpec);
            EnsureRows(initialRows);
            EnsureGeneratedRowsAhead();
            StabilizeInitialBoard();
            PruneRowsBehind();
        }

        private FossickGameplaySession(FossickMapConfig config, FossickSaveState save, int initialRows, bool unlimitedTools)
        {
            this.config = config ?? FossickSampleMapFactory.CreateDefaultConfig();
            seed = save == null ? 0 : save.seed;
            this.unlimitedTools = unlimitedTools;
            generationState = save != null && save.generationState != null ? save.generationState.Clone() : new FossickGenerationState(seed);
            actionResolver = new FossickActionResolver(this.config.tools);
            Inventory = FossickInventoryState.FromConfig(this.config.gameplay);
            Rewards = new FossickRewardState();
            Progress = new FossickProgressState();
            Board = new FossickBoard(this.config.BoardSpec);
            if (save == null)
            {
                EnsureRows(initialRows);
                EnsureGeneratedRowsAhead();
                StabilizeInitialBoard();
                PruneRowsBehind();
                return;
            }

            if (save.loadedRows != null && save.loadedRows.Count > 0)
            {
                Board.LoadSavedRows(save.loadedRows, save.loadedStartRow);
            }
            else
            {
                generationState = new FossickGenerationState(seed);
                EnsureRows(save.topVisibleRow + GetGenerationBufferRows());
            }

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
            EnsureGeneratedRowsAhead();
            Board.RefreshFogFromOpenSpace();
            PruneRowsBehind();
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

            EnsureGeneratedRowsAhead();
            var canCollectSpawnedRewardWithoutTool = CanCollectSpawnedRewardWithoutTool(toolType, x, y);
            if (!unlimitedTools && !canCollectSpawnedRewardWithoutTool && !Inventory.HasTool(toolType))
            {
                result.notEnoughTool = true;
                return result;
            }

            var action = actionResolver.ResolveTool(Board, toolType, x, y);
            result.action = action;
            result.actionWasApplied = action != null && action.isApplied;
            if (!result.actionWasApplied)
            {
                return result;
            }

            if (!unlimitedTools && action.toolConsumed)
            {
                Inventory.ConsumeTool(toolType);
            }

            EnsureGeneratedRowsAhead();
            ContinueScrollAfterGeneration(action);
            Progress.Apply(action);
            ApplyRewards(action);
            result.scoreAfter = Rewards.score;
            EnsureGeneratedRowsAhead();
            PruneRowsBehind();
            return result;
        }

        private bool CanCollectSpawnedRewardWithoutTool(FossickToolType toolType, int x, int y)
        {
            if (toolType != FossickToolType.Pickaxe || Board == null)
            {
                return false;
            }

            var cell = Board.GetCell(x, y);
            return cell != null && cell.fog == FossickFogType.None && cell.HasSpawnedReward;
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
            save.generationState = generationState.Clone();
            save.generatedFragmentIds = generationState.generatedFragmentIds == null
                ? new System.Collections.Generic.List<int>()
                : new System.Collections.Generic.List<int>(generationState.generatedFragmentIds);
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

            var additionalRows = targetRows - Board.RowCount;
            var mine = FossickMineLayoutBuilder.BuildAdditional(config, generationState, additionalRows, Board.RowCount, null);
            Board.AppendAdditionalGeneratedMine(mine);
        }

        private void EnsureGeneratedRowsAhead()
        {
            EnsureRows(Board.TopVisibleRow + GetGenerationBufferRows());
        }

        private void PruneRowsBehind()
        {
            Board.PruneRowsBefore(Board.TopVisibleRow - GetRetentionRowsBehind());
        }

        private void ContinueScrollAfterGeneration(FossickActionResult action)
        {
            if (action == null)
            {
                return;
            }

            while (Board.TryScrollDown())
            {
                Board.RefreshFogFromOpenSpace();
                EnsureGeneratedRowsAhead();
                action.scrolled = true;
                action.scrollCount++;
                action.depthAfterAction = Board.Depth;
            }
        }

        private int GetGenerationBufferRows()
        {
            var generation = config == null ? null : config.generation;
            var visibleHeight = config == null ? FossickBoardSpec.DefaultVisibleHeight : config.visibleHeight;
            var screenCount = generation == null ? 4 : generation.prefetchVisibleScreens;
            var minimumRowsAhead = generation == null ? 24 : generation.minimumRowsAhead;
            if (screenCount < 1)
            {
                screenCount = 1;
            }

            if (minimumRowsAhead < visibleHeight)
            {
                minimumRowsAhead = visibleHeight;
            }

            var rowsAhead = visibleHeight * screenCount;
            if (rowsAhead < minimumRowsAhead)
            {
                rowsAhead = minimumRowsAhead;
            }

            return visibleHeight + rowsAhead;
        }

        private int GetRetentionRowsBehind()
        {
            var generation = config == null ? null : config.generation;
            var visibleHeight = config == null ? FossickBoardSpec.DefaultVisibleHeight : config.visibleHeight;
            var retainRowsBehind = generation == null ? visibleHeight * 2 : generation.retainRowsBehind;
            if (retainRowsBehind < 0)
            {
                return 0;
            }

            return retainRowsBehind;
        }

        private void StabilizeInitialBoard()
        {
            Board.RefreshFogFromOpenSpace();
            while (Board.TryScrollDown())
            {
                EnsureGeneratedRowsAhead();
                Board.RefreshFogFromOpenSpace();
            }

            Progress.depth = Board.Depth;
            EnsureGeneratedRowsAhead();
            PruneRowsBehind();
        }

    }
}
