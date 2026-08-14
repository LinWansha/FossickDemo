using System.Collections.Generic;
using System;
using Fossick.Core.Definition.Config;
using Fossick.Core.Generation;
using Fossick.Core.Data;
using Fossick.Core.Mine.Objects;

namespace Fossick.Core.Mine
{
    public sealed class FossickMineFogReveal
    {
        public int x;
        public int y;
        public bool wasVisible;
        public bool isVisible;
    }

    public sealed class FossickMine
    {
        private readonly List<FossickMineRow> rows = new List<FossickMineRow>();
        private readonly IFossickRewardProvider rewardProvider;
        private int firstLoadedRow;

        public FossickMine(
            FossickBoardSpec spec,
            FossickBackgroundLayout backgroundLayout,
            IFossickRewardProvider rewardProvider)
        {
            Spec = spec;
            BackgroundLayout = backgroundLayout;
            this.rewardProvider = rewardProvider;
            Window = new FossickMineWindow(0, Spec.width, Spec.visibleHeight);
            RegionLayer = new FossickMineRegionLayer();
        }

        public FossickBoardSpec Spec { get; }
        public FossickBackgroundLayout BackgroundLayout { get; }
        public FossickMineWindow Window { get; }
        public FossickMineRegionLayer RegionLayer { get; }
        public int TopVisibleRow { get; private set; }
        public int Depth { get; private set; }
        public int FirstLoadedRow => firstLoadedRow;
        public int LoadedRowCount => rows.Count;
        public int RowCount => firstLoadedRow + rows.Count;
        public IReadOnlyList<FossickMineRow> Rows => rows;
        public IReadOnlyList<FossickRegionObject> Regions => RegionLayer.Regions;

        public void AddRow(FossickCell[] row)
        {
            rows.Add(new FossickMineRow(RowCount, row));
        }

        public void AppendFragment(FossickFragmentConfig fragment)
        {
            var fragmentStartRow = RowCount;
            for (var y = 0; y < fragment.height; y++)
            {
                var absoluteRow = RowCount;
                var row = FossickRuntimeObjectFactory.CreateEmptyRow(Spec, absoluteRow);
                for (var i = 0; i < fragment.cells.Count; i++)
                {
                    var cellConfig = fragment.cells[i];
                    if (cellConfig == null || cellConfig.y != y || cellConfig.x < 0 || cellConfig.x >= Spec.width)
                    {
                        continue;
                    }

                    row[cellConfig.x] = FossickRuntimeObjectFactory.CreateCell(
                        cellConfig,
                        cellConfig.x,
                        absoluteRow,
                        rewardProvider);
                }

                rows.Add(new FossickMineRow(absoluteRow, row));
            }

            AddRewardBackgroundRegion(fragment, fragmentStartRow);
        }

        public void AppendGeneratedMine(FossickGeneratedMine mine)
        {
            var mineStartRow = RowCount;
            for (var i = 0; i < mine.rows.Count; i++)
            {
                AppendGeneratedRow(mine.rows[i]);
            }

            for (var i = 0; i < mine.fragments.Count; i++)
            {
                var span = mine.fragments[i];
                AddRewardBackgroundRegion(span.config, mineStartRow + span.startRow);
            }
        }

        public void RestoreRows(
            int loadedStartRow,
            IReadOnlyList<FossickMineRowData> savedRows,
            int topVisibleRow)
        {
            rows.Clear();
            RegionLayer.Clear();
            firstLoadedRow = loadedStartRow;
            TopVisibleRow = firstLoadedRow;
            Depth = firstLoadedRow;
            Window.MoveTo(TopVisibleRow);

            for (var rowOffset = 0; rowOffset < savedRows.Count; rowOffset++)
            {
                var absoluteRow = firstLoadedRow + rowOffset;
                var savedRow = savedRows[rowOffset];
                var row = FossickRuntimeObjectFactory.CreateEmptyRow(Spec, absoluteRow);
                for (var i = 0; i < savedRow.cells.Count; i++)
                {
                    var cellData = savedRow.cells[i];
                    row[cellData.x] = FossickRuntimeObjectFactory.CreateCell(
                        cellData,
                        cellData.x,
                        absoluteRow,
                        rewardProvider);
                }

                rows.Add(new FossickMineRow(absoluteRow, row));
            }

            MoveWindowTo(topVisibleRow);
        }

        public void RebuildRewardBackgroundRegions(
            FossickMapConfig config,
            IReadOnlyList<int> generatedFragmentIds)
        {
            var fragmentsById = new Dictionary<int, FossickFragmentConfig>();
            for (var i = 0; i < config.fragments.Count; i++)
            {
                var fragment = config.fragments[i];
                fragmentsById.Add(fragment.id, fragment);
            }

            var overridesBySequence = new Dictionary<int, FossickFragmentConfig>();
            for (var i = 0; i < config.generation.sequenceOverrides.Count; i++)
            {
                var sequenceOverride = config.generation.sequenceOverrides[i];
                overridesBySequence.Add(sequenceOverride.sequenceIndex, sequenceOverride.fragment);
            }

            var absoluteStartRow = 0;
            for (var i = 0; i < generatedFragmentIds.Count; i++)
            {
                var fragment = overridesBySequence.TryGetValue(i, out var sequenceFragment)
                    ? sequenceFragment
                    : fragmentsById[generatedFragmentIds[i]];
                var absoluteEndRow = absoluteStartRow + fragment.height;
                if (absoluteEndRow > firstLoadedRow && absoluteStartRow < RowCount)
                {
                    AddRewardBackgroundRegion(fragment, absoluteStartRow);
                }

                absoluteStartRow = absoluteEndRow;
            }
        }

        public void AppendGeneratedRow(FossickGeneratedMineRow generatedRow)
        {
            var absoluteRow = RowCount;
            var row = FossickRuntimeObjectFactory.CreateEmptyRow(Spec, absoluteRow);
            for (var x = 0; x < Spec.width; x++)
            {
                row[x] = FossickRuntimeObjectFactory.CreateCell(
                    generatedRow.cells[x],
                    x,
                    absoluteRow,
                    rewardProvider);
            }

            rows.Add(new FossickMineRow(absoluteRow, row));
        }

        public IReadOnlyList<FossickCell[]> GetVisibleRows()
        {
            return GetRowsWindow(TopVisibleRow, Spec.visibleHeight);
        }

        public IReadOnlyList<FossickCell[]> GetRowsWindow(int startRow, int rowCount)
        {
            var window = new List<FossickCell[]>();
            if (rowCount <= 0)
            {
                return window;
            }

            for (var offset = 0; offset < rowCount; offset++)
            {
                var absoluteRow = startRow + offset;
                var localRow = absoluteRow - firstLoadedRow;
                if (localRow < 0 || localRow >= rows.Count)
                {
                    window.Add(FossickRuntimeObjectFactory.CreateEmptyRow(Spec, absoluteRow));
                }
                else
                {
                    window.Add(rows[localRow].CellArray);
                }
            }

            return window;
        }

        public FossickCell GetCell(int x, int y)
        {
            return GetCellAtAbsoluteRow(x, TopVisibleRow + y);
        }

        public FossickCell GetCellAtAbsoluteRow(int x, int y)
        {
            var localRow = y - firstLoadedRow;
            if (x < 0 || x >= Spec.width || localRow < 0 || localRow >= rows.Count)
            {
                return null;
            }

            return rows[localRow].GetCell(x);
        }

        public IEnumerable<FossickCell> EnumerateCells()
        {
            for (var localY = 0; localY < rows.Count; localY++)
            {
                var row = rows[localY];
                if (row == null)
                {
                    continue;
                }

                for (var x = 0; x < row.Cells.Count; x++)
                {
                    if (row.Cells[x] != null)
                    {
                        yield return row.Cells[x];
                    }
                }
            }
        }

        public bool MoveWindowTo(int topVisibleRow)
        {
            if (topVisibleRow < 0 || RowCount < topVisibleRow + Spec.visibleHeight)
            {
                return false;
            }

            TopVisibleRow = topVisibleRow;
            Depth = topVisibleRow;
            Window.MoveTo(topVisibleRow);
            return true;
        }

        public bool TryScrollDown()
        {
            if (!CanScrollDown())
            {
                return false;
            }

            MoveWindowTo(TopVisibleRow + 1);
            return true;
        }

        public bool CanScrollDown()
        {
            if (RowCount < TopVisibleRow + Spec.visibleHeight + 1)
            {
                return false;
            }

            return TopTwoVisibleRowsHaveNoDiggableTerrain() && HasVisibleOpenPathToBottomRow();
        }

        public List<FossickMineFogReveal> RefreshFogFromOpenSpace()
        {
            var reveals = new List<FossickMineFogReveal>();
            if (rows.Count == 0)
            {
                return reveals;
            }

            var visibleBottom = TopVisibleRow + Spec.visibleHeight - 1;
            var maxRow = visibleBottom + 1 < RowCount ? visibleBottom + 1 : RowCount - 1;
            var visitStartRow = TopVisibleRow;
            var visited = new bool[maxRow - visitStartRow + 1, Spec.width];
            var queue = new Queue<FossickCell>();

            for (var y = TopVisibleRow; y <= visibleBottom && y < RowCount; y++)
            {
                for (var x = 0; x < Spec.width; x++)
                {
                    var cell = GetCellAtAbsoluteRow(x, y);
                    if (cell != null && cell.IsPassable && cell.IsVisible)
                    {
                        EnqueueOpenCell(x, y, visitStartRow, maxRow, visited, queue);
                    }
                }
            }

            while (queue.Count > 0)
            {
                var cell = queue.Dequeue();
                RevealCell(cell, reveals);

                VisitNeighbor(cell.Position.x - 1, cell.Position.y, visitStartRow, maxRow, visited, queue, reveals);
                VisitNeighbor(cell.Position.x + 1, cell.Position.y, visitStartRow, maxRow, visited, queue, reveals);
                VisitNeighbor(cell.Position.x, cell.Position.y - 1, visitStartRow, maxRow, visited, queue, reveals);
                VisitNeighbor(cell.Position.x, cell.Position.y + 1, visitStartRow, maxRow, visited, queue, reveals);
            }

            return reveals;
        }

        public void PruneRowsBefore(int rowIndex)
        {
            if (rowIndex <= firstLoadedRow || rows.Count == 0)
            {
                return;
            }

            var pruneBefore = rowIndex > TopVisibleRow ? TopVisibleRow : rowIndex;
            var removeCount = pruneBefore - firstLoadedRow;
            if (removeCount <= 0)
            {
                return;
            }

            if (removeCount > rows.Count)
            {
                removeCount = rows.Count;
            }

            rows.RemoveRange(0, removeCount);
            firstLoadedRow += removeCount;
            RegionLayer.PruneBefore(firstLoadedRow);
        }

        public void AddRegion(FossickRegionObject region)
        {
            RegionLayer.Add(region);
        }

        private void AddRewardBackgroundRegion(FossickFragmentConfig fragment, int absoluteStartRow)
        {
            if (fragment == null || string.IsNullOrEmpty(fragment.rewardBackgroundId))
            {
                return;
            }

            if (!FossickRewardBackgroundSpec.TryGetSize(fragment.rewardBackgroundId, out var width, out var height))
            {
                return;
            }

            var startX = fragment.rewardBackgroundX;
            var startY = fragment.rewardBackgroundY;
            if (startX < 0 ||
                startY < 0 ||
                startX + width > fragment.width ||
                startY + height > fragment.height)
            {
                return;
            }

            var bounds = new FossickRect(startX, absoluteStartRow + startY, width, height);
            AddRegion(new RewardBackdropRegion(
                CreateRewardBackgroundRegionId(absoluteStartRow),
                bounds,
                fragment.rewardBackgroundId));
        }

        private static string CreateRewardBackgroundRegionId(int absoluteStartRow)
        {
            return "reward_background_" + absoluteStartRow;
        }

        private bool TopTwoVisibleRowsHaveNoDiggableTerrain()
        {
            if (Spec.visibleHeight < 2)
            {
                return false;
            }

            for (var offset = 0; offset < 2; offset++)
            {
                var rowIndex = TopVisibleRow + offset;
                if (rowIndex < firstLoadedRow || rowIndex >= RowCount)
                {
                    return false;
                }

                var row = rows[rowIndex - firstLoadedRow];
                for (var x = 0; x < row.Cells.Count; x++)
                {
                    if (row.Cells[x] != null && row.Cells[x].HasDiggableTerrain)
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        private bool HasVisibleOpenPathToBottomRow()
        {
            if (Spec.visibleHeight <= 0)
            {
                return false;
            }

            var visited = new bool[Spec.visibleHeight, Spec.width];
            var queue = new Queue<FossickCell>();
            var seedRows = Spec.visibleHeight < 2 ? Spec.visibleHeight : 2;

            for (var y = 0; y < seedRows; y++)
            {
                for (var x = 0; x < Spec.width; x++)
                {
                    EnqueueVisiblePassableCell(x, y, visited, queue);
                }
            }

            while (queue.Count > 0)
            {
                var cell = queue.Dequeue();
                var visibleY = cell.Position.y - TopVisibleRow;
                if (visibleY == Spec.visibleHeight - 1)
                {
                    return true;
                }

                EnqueueVisiblePassableCell(cell.Position.x - 1, visibleY, visited, queue);
                EnqueueVisiblePassableCell(cell.Position.x + 1, visibleY, visited, queue);
                EnqueueVisiblePassableCell(cell.Position.x, visibleY - 1, visited, queue);
                EnqueueVisiblePassableCell(cell.Position.x, visibleY + 1, visited, queue);
            }

            return false;
        }

        private void EnqueueVisiblePassableCell(int x, int visibleY, bool[,] visited, Queue<FossickCell> queue)
        {
            if (x < 0 || x >= Spec.width || visibleY < 0 || visibleY >= Spec.visibleHeight || visited[visibleY, x])
            {
                return;
            }

            var cell = GetCellAtAbsoluteRow(x, TopVisibleRow + visibleY);
            if (cell == null || !cell.IsPassable || !cell.IsVisible)
            {
                return;
            }

            visited[visibleY, x] = true;
            queue.Enqueue(cell);
        }

        private void VisitNeighbor(int x, int y, int visitStartRow, int maxRow, bool[,] visited, Queue<FossickCell> queue, List<FossickMineFogReveal> reveals)
        {
            var cell = GetCellAtAbsoluteRow(x, y);
            if (cell == null || y < visitStartRow || y > maxRow)
            {
                return;
            }

            if (cell.HasObstacle)
            {
                RevealCell(cell, reveals);
                return;
            }

            EnqueueOpenCell(x, y, visitStartRow, maxRow, visited, queue);
        }

        private void EnqueueOpenCell(int x, int y, int visitStartRow, int maxRow, bool[,] visited, Queue<FossickCell> queue)
        {
            var cell = GetCellAtAbsoluteRow(x, y);
            var localVisitY = y - visitStartRow;
            if (cell == null || y < visitStartRow || y > maxRow || localVisitY < 0 || localVisitY >= visited.GetLength(0) || !cell.IsPassable || visited[localVisitY, x])
            {
                return;
            }

            visited[localVisitY, x] = true;
            queue.Enqueue(cell);
        }

        private static void RevealCell(FossickCell cell, List<FossickMineFogReveal> reveals)
        {
            if (cell == null || cell.IsVisible)
            {
                return;
            }

            var wasVisible = cell.IsVisible;
            if (cell.Fog == null)
            {
                cell.Fog = new FossickFogState(true);
            }
            else
            {
                cell.Fog.Reveal();
            }

            reveals.Add(new FossickMineFogReveal
            {
                x = cell.Position.x,
                y = cell.Position.y,
                wasVisible = wasVisible,
                isVisible = cell.IsVisible
            });
        }
    }
}
