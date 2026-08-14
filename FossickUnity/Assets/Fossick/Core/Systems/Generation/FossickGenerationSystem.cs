using System;
using Fossick.Core.Definition.Config;
using Fossick.Core.Generation;
using Fossick.Core.Data;
using Fossick.Core.Mine;

namespace Fossick.Core.Systems
{
    public sealed class FossickGenerationSystem : FossickSystem
    {
        private const int PrefetchVisibleScreens = 4;
        private const int MinimumRowsAhead = 24;
        private const int RetainRowsBehind = 12;

        private readonly FossickMapConfig config;

        public FossickGenerationSystem(FossickMapConfig config)
            : base("Generation")
        {
            this.config = config ?? throw new ArgumentNullException(nameof(config));
        }

        public void EnsureRows(FossickMine mine, FossickGenerationData generationState, int targetRows)
        {
            if (mine == null || generationState == null || targetRows <= mine.RowCount)
            {
                return;
            }

            var additionalRows = targetRows - mine.RowCount;
            var generatedMine = FossickMineLayoutBuilder.BuildAdditional(config, generationState, additionalRows, mine.RowCount, null);
            mine.AppendGeneratedMine(generatedMine);
        }

        public void EnsureGeneratedRowsAhead(FossickMine mine, FossickGenerationData generationState)
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
            var visibleHeight = config.visibleHeight;
            var rowsAhead = Math.Max(visibleHeight * PrefetchVisibleScreens, MinimumRowsAhead);
            return visibleHeight + rowsAhead;
        }

        public int GetRetentionRowsBehind()
        {
            return RetainRowsBehind;
        }
    }
}
