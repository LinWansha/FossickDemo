using Fossick.Core.Definition.Config;
using Fossick.Core.Generation;
using Fossick.Core.State;
using Fossick.Core.Mine;

namespace Fossick.Core.Systems
{
    public sealed class FossickGenerationSystem : FossickSystem
    {
        private readonly FossickMapConfig config;

        public FossickGenerationSystem(FossickMapConfig config)
            : base("Generation")
        {
            this.config = config ?? FossickSampleMapFactory.CreateDefaultConfig();
        }

        public void EnsureRows(FossickMine mine, FossickGenerationState generationState, int targetRows)
        {
            if (mine == null || generationState == null || targetRows <= mine.RowCount)
            {
                return;
            }

            var additionalRows = targetRows - mine.RowCount;
            var generatedMine = FossickMineLayoutBuilder.BuildAdditional(config, generationState, additionalRows, mine.RowCount, null);
            mine.AppendGeneratedMine(generatedMine);
        }

        public void EnsureGeneratedRowsAhead(FossickMine mine, FossickGenerationState generationState)
        {
            if (mine == null)
            {
                return;
            }

            EnsureRows(mine, generationState, mine.TopVisibleRow + GetGenerationBufferRows());
        }

        public void PruneRowsBehind(FossickMine mine)
        {
            if (mine == null)
            {
                return;
            }

            mine.PruneRowsBefore(mine.TopVisibleRow - GetRetentionRowsBehind());
        }

        public int GetGenerationBufferRows()
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

        public int GetRetentionRowsBehind()
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
    }
}
