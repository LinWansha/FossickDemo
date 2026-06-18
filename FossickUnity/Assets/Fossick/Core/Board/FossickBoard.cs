using System.Collections.Generic;
using Fossick.Core.Config;
using Fossick.Core.Generation;
using Fossick.Core.Rewards;
using Fossick.Core.Save;

namespace Fossick.Core.Board
{
    public sealed class FossickFogReveal
    {
        public int x;
        public int y;
        public FossickFogType fogBefore;
        public FossickFogType fogAfter;
    }

    public sealed class FossickBoard
    {
        private readonly List<FossickCellState[]> rows = new List<FossickCellState[]>();
        private int firstLoadedRow;

        public FossickBoardSpec Spec { get; }
        public int TopVisibleRow { get; private set; }
        public int Depth { get; private set; }
        public int FirstLoadedRow => firstLoadedRow;
        public int LoadedRowCount => rows.Count;
        public int RowCount => firstLoadedRow + rows.Count;

        public FossickBoard(FossickBoardSpec spec)
        {
            Spec = spec.IsValid ? spec : FossickBoardSpec.Default;
        }

        public void AppendFragment(FossickFragmentConfig fragment)
        {
            if (fragment == null)
            {
                return;
            }

            for (var y = 0; y < fragment.height; y++)
            {
                var absoluteRow = RowCount;
                var row = CreateEmptyRow(absoluteRow);
                for (var i = 0; i < fragment.cells.Count; i++)
                {
                    var cellConfig = fragment.cells[i];
                    if (cellConfig == null || cellConfig.y != y || cellConfig.x < 0 || cellConfig.x >= Spec.width)
                    {
                        continue;
                    }

                    row[cellConfig.x] = CreateCell(cellConfig, cellConfig.x, absoluteRow);
                }

                rows.Add(row);
            }
        }

        public void AppendGeneratedMine(FossickGeneratedMine mine)
        {
            if (mine == null || mine.rows == null)
            {
                return;
            }

            for (var i = RowCount; i < mine.rows.Count; i++)
            {
                AppendGeneratedRow(mine.rows[i]);
            }
        }

        public void AppendAdditionalGeneratedMine(FossickGeneratedMine mine)
        {
            if (mine == null || mine.rows == null)
            {
                return;
            }

            for (var i = 0; i < mine.rows.Count; i++)
            {
                AppendGeneratedRow(mine.rows[i]);
            }
        }

        public void AppendGeneratedRow(FossickGeneratedMineRow generatedRow)
        {
            var absoluteRow = RowCount;
            var row = CreateEmptyRow(absoluteRow);
            if (generatedRow != null && generatedRow.cells != null)
            {
                for (var x = 0; x < Spec.width && x < generatedRow.cells.Length; x++)
                {
                    row[x] = CreateCell(generatedRow.cells[x], x, absoluteRow);
                }
            }

            rows.Add(row);
        }

        public IReadOnlyList<FossickCellState[]> GetVisibleRows()
        {
            return GetRowsWindow(TopVisibleRow, Spec.visibleHeight);
        }

        public IReadOnlyList<FossickCellState[]> GetRowsWindow(int startRow, int rowCount)
        {
            var window = new List<FossickCellState[]>();
            if (rowCount <= 0)
            {
                return window;
            }

            for (var offset = 0; offset < rowCount; offset++)
            {
                var rowIndex = startRow + offset;
                var localRow = rowIndex - firstLoadedRow;
                if (localRow < 0 || localRow >= rows.Count)
                {
                    window.Add(CreateEmptyRow(rowIndex));
                }
                else
                {
                    window.Add(rows[localRow]);
                }
            }

            return window;
        }

        public FossickCellState GetCell(int x, int y)
        {
            var rowIndex = TopVisibleRow + y;
            return GetCellAtAbsoluteRow(x, rowIndex);
        }

        public FossickCellState GetCellAtAbsoluteRow(int x, int rowIndex)
        {
            var localRow = rowIndex - firstLoadedRow;
            if (x < 0 || x >= Spec.width || localRow < 0 || localRow >= rows.Count)
            {
                return null;
            }

            return rows[localRow][x];
        }

        public IEnumerable<FossickCellState> EnumerateCells()
        {
            for (var localY = 0; localY < rows.Count; localY++)
            {
                var row = rows[localY];
                if (row == null)
                {
                    continue;
                }

                for (var x = 0; x < row.Length; x++)
                {
                    if (row[x] != null)
                    {
                        yield return row[x];
                    }
                }
            }
        }

        public FossickSaveState CreateSaveState(int seed, FossickProgressState progress)
        {
            var save = new FossickSaveState
            {
                schemaVersion = FossickSaveState.CurrentSchemaVersion,
                seed = seed,
                loadedStartRow = firstLoadedRow,
                topVisibleRow = TopVisibleRow,
                depth = Depth
            };

            if (progress != null)
            {
                save.oreFound = progress.oreFound;
                save.collectionFound = progress.collectionFound;
                save.toolUsed = progress.toolUsed;
            }

            for (var localY = 0; localY < rows.Count; localY++)
            {
                var row = rows[localY];
                if (row == null)
                {
                    continue;
                }

                var absoluteY = firstLoadedRow + localY;
                save.loadedRows.Add(CreateSavedRow(row, absoluteY));
                for (var x = 0; x < row.Length; x++)
                {
                    var cell = row[x];
                    if (cell == null)
                    {
                        continue;
                    }

                    var key = GetCellSaveKey(x, absoluteY);
                    if (cell.generatedWithObstacle && cell.terrain == FossickTerrainType.Empty)
                    {
                        save.destroyedCells.Add(key);
                    }

                    if (cell.collected)
                    {
                        save.collectedCells.Add(key);
                    }

                    if (cell.fog == FossickFogType.None)
                    {
                        save.visibleCells.Add(key);
                    }

                }
            }

            return save;
        }

        public void LoadSavedRows(List<FossickSavedMineRow> savedRows, int loadedStartRow)
        {
            rows.Clear();
            firstLoadedRow = loadedStartRow < 0 ? 0 : loadedStartRow;
            if (savedRows == null || savedRows.Count == 0)
            {
                firstLoadedRow = 0;
                return;
            }

            savedRows.Sort((a, b) => a.rowIndex.CompareTo(b.rowIndex));
            firstLoadedRow = savedRows[0].rowIndex < 0 ? 0 : savedRows[0].rowIndex;
            for (var i = 0; i < savedRows.Count; i++)
            {
                rows.Add(CreateRowFromSave(savedRows[i]));
            }
        }

        public void ApplySaveState(FossickSaveState save)
        {
            if (save == null)
            {
                return;
            }

            TopVisibleRow = ClampRowIndex(save.topVisibleRow);
            Depth = save.depth < 0 ? 0 : save.depth;
            ApplyDestroyedCells(save.destroyedCells);
            ApplyCollectedCells(save.collectedCells);
            ApplyVisibleCells(save.visibleCells);
        }

        public bool TryScrollDown()
        {
            if (!CanScrollDown())
            {
                return false;
            }

            TopVisibleRow++;
            Depth++;
            return true;
        }

        public bool CanScrollDown()
        {
            if (RowCount < TopVisibleRow + Spec.visibleHeight + 1)
            {
                return false;
            }

            return BottomVisibleRowHasVisibleEmptyCell();
        }

        private bool BottomVisibleRowHasVisibleEmptyCell()
        {
            var rowIndex = TopVisibleRow + Spec.visibleHeight - 1;
            if (rowIndex < 0 || rowIndex >= RowCount)
            {
                return false;
            }

            var localRow = rowIndex - firstLoadedRow;
            if (localRow < 0 || localRow >= rows.Count)
            {
                return false;
            }

            var row = rows[localRow];
            for (var x = 0; x < row.Length; x++)
            {
                var cell = row[x];
                if (cell != null && !cell.HasObstacle && cell.fog == FossickFogType.None)
                {
                    return true;
                }
            }

            return false;
        }

        public List<FossickFogReveal> RefreshFogFromOpenSpace()
        {
            var reveals = new List<FossickFogReveal>();
            if (rows.Count == 0)
            {
                return reveals;
            }

            var visibleBottom = TopVisibleRow + Spec.visibleHeight - 1;
            var maxRow = visibleBottom + 1 < RowCount ? visibleBottom + 1 : RowCount - 1;
            var visitStartRow = TopVisibleRow;
            var visited = new bool[maxRow - visitStartRow + 1, Spec.width];
            var queue = new Queue<FossickCellState>();

            for (var y = TopVisibleRow; y <= visibleBottom && y < RowCount; y++)
            {
                for (var x = 0; x < Spec.width; x++)
                {
                    var cell = GetCellAtAbsoluteRow(x, y);
                    if (cell != null && !cell.HasObstacle && cell.fog == FossickFogType.None)
                    {
                        EnqueueOpenCell(x, y, visitStartRow, maxRow, visited, queue);
                    }
                }
            }

            while (queue.Count > 0)
            {
                var cell = queue.Dequeue();
                RevealCell(cell, reveals);

                VisitNeighbor(cell.x - 1, cell.y, visitStartRow, maxRow, visited, queue, reveals);
                VisitNeighbor(cell.x + 1, cell.y, visitStartRow, maxRow, visited, queue, reveals);
                VisitNeighbor(cell.x, cell.y - 1, visitStartRow, maxRow, visited, queue, reveals);
                VisitNeighbor(cell.x, cell.y + 1, visitStartRow, maxRow, visited, queue, reveals);
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
        }

        private void VisitNeighbor(int x, int y, int visitStartRow, int maxRow, bool[,] visited, Queue<FossickCellState> queue, List<FossickFogReveal> reveals)
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

        private void EnqueueOpenCell(int x, int y, int visitStartRow, int maxRow, bool[,] visited, Queue<FossickCellState> queue)
        {
            var cell = GetCellAtAbsoluteRow(x, y);
            var localVisitY = y - visitStartRow;
            if (cell == null || y < visitStartRow || y > maxRow || localVisitY < 0 || localVisitY >= visited.GetLength(0) || cell.HasObstacle || visited[localVisitY, x])
            {
                return;
            }

            visited[localVisitY, x] = true;
            queue.Enqueue(cell);
        }

        private static void RevealCell(FossickCellState cell, List<FossickFogReveal> reveals)
        {
            if (cell == null)
            {
                return;
            }

            if (cell.fog == FossickFogType.None)
            {
                return;
            }

            var before = cell.fog;
            cell.fog = FossickFogType.None;
            reveals.Add(new FossickFogReveal
            {
                x = cell.x,
                y = cell.y,
                fogBefore = before,
                fogAfter = cell.fog
            });
        }

        private FossickCellState[] CreateEmptyRow(int y)
        {
            var row = new FossickCellState[Spec.width];
            for (var x = 0; x < Spec.width; x++)
            {
                row[x] = new FossickCellState
                {
                    x = x,
                    y = y,
                    terrain = FossickTerrainType.Empty,
                    fog = FossickFogType.None,
                    generatedWithObstacle = false
                };
            }

            return row;
        }

        private FossickSavedMineRow CreateSavedRow(FossickCellState[] row, int absoluteY)
        {
            var saved = new FossickSavedMineRow
            {
                rowIndex = absoluteY
            };

            for (var x = 0; x < Spec.width; x++)
            {
                saved.cells.Add(CreateSavedCell(row == null ? null : row[x], x, absoluteY));
            }

            return saved;
        }

        private FossickSavedCellState CreateSavedCell(FossickCellState cell, int x, int absoluteY)
        {
            if (cell == null)
            {
                return new FossickSavedCellState
                {
                    x = x,
                    y = absoluteY,
                    terrain = FossickTerrainType.Empty,
                    fog = FossickFogType.None
                };
            }

            return new FossickSavedCellState
            {
                x = x,
                y = absoluteY,
                backgroundId = cell.backgroundId,
                rewardBackgroundId = cell.rewardBackgroundId,
                terrain = cell.terrain,
                hp = cell.hp,
                reward = cell.reward,
                decorations = cell.decorations == null ? new List<string>() : new List<string>(cell.decorations),
                fog = cell.fog,
                collected = cell.collected,
                generatedWithObstacle = cell.generatedWithObstacle
            };
        }

        private FossickCellState[] CreateRowFromSave(FossickSavedMineRow savedRow)
        {
            var absoluteY = savedRow == null ? RowCount : savedRow.rowIndex;
            var row = CreateEmptyRow(absoluteY);
            if (savedRow == null || savedRow.cells == null)
            {
                return row;
            }

            for (var i = 0; i < savedRow.cells.Count; i++)
            {
                var savedCell = savedRow.cells[i];
                if (savedCell == null || savedCell.x < 0 || savedCell.x >= Spec.width)
                {
                    continue;
                }

                row[savedCell.x] = CreateCellFromSave(savedCell, absoluteY);
            }

            return row;
        }

        private FossickCellState CreateCellFromSave(FossickSavedCellState savedCell, int absoluteY)
        {
            return new FossickCellState
            {
                x = savedCell.x,
                y = absoluteY,
                backgroundId = savedCell.backgroundId,
                rewardBackgroundId = savedCell.rewardBackgroundId,
                terrain = savedCell.terrain,
                hp = savedCell.hp,
                reward = savedCell.reward,
                decorations = savedCell.decorations == null ? new string[0] : savedCell.decorations.ToArray(),
                fog = savedCell.fog,
                collected = savedCell.collected,
                generatedWithObstacle = savedCell.generatedWithObstacle
            };
        }

        private FossickCellState CreateCell(FossickCellConfig config, int x, int absoluteY)
        {
            if (config == null)
            {
                return new FossickCellState
                {
                    x = x,
                    y = absoluteY,
                    terrain = FossickTerrainType.Empty,
                    fog = FossickFogType.None,
                    generatedWithObstacle = false
                };
            }

            return new FossickCellState
            {
                x = config.x,
                y = absoluteY,
                backgroundId = config.backgroundId,
                rewardBackgroundId = config.rewardBackgroundId,
                terrain = config.terrain,
                hp = config.hp,
                reward = config.reward ?? config.element,
                decorations = ResolveDecorations(config),
                fog = config.fog,
                generatedWithObstacle = config.terrain != FossickTerrainType.Empty
            };
        }

        private void ApplyDestroyedCells(List<string> destroyedCells)
        {
            if (destroyedCells == null)
            {
                return;
            }

            for (var i = 0; i < destroyedCells.Count; i++)
            {
                if (!TryParseCellSaveKey(destroyedCells[i], out var x, out var y))
                {
                    continue;
                }

                var cell = GetCellAtAbsoluteRow(x, y);
                if (cell == null)
                {
                    continue;
                }

                cell.terrain = FossickTerrainType.Empty;
                cell.hp = 0;
            }
        }

        private void ApplyCollectedCells(List<string> collectedCells)
        {
            if (collectedCells == null)
            {
                return;
            }

            for (var i = 0; i < collectedCells.Count; i++)
            {
                if (!TryParseCellSaveKey(collectedCells[i], out var x, out var y))
                {
                    continue;
                }

                var cell = GetCellAtAbsoluteRow(x, y);
                if (cell != null)
                {
                    cell.collected = true;
                }
            }
        }

        private void ApplyVisibleCells(List<string> visibleCells)
        {
            if (visibleCells == null)
            {
                return;
            }

            for (var i = 0; i < visibleCells.Count; i++)
            {
                if (!TryParseCellSaveKey(visibleCells[i], out var x, out var y))
                {
                    continue;
                }

                var cell = GetCellAtAbsoluteRow(x, y);
                if (cell != null)
                {
                    cell.fog = FossickFogType.None;
                }
            }
        }

        private int ClampRowIndex(int rowIndex)
        {
            if (rows.Count == 0)
            {
                return 0;
            }

            if (rowIndex < 0)
            {
                return firstLoadedRow;
            }

            if (rowIndex < firstLoadedRow)
            {
                return firstLoadedRow;
            }

            return rowIndex >= RowCount ? RowCount - 1 : rowIndex;
        }

        private static string GetCellSaveKey(int x, int y)
        {
            return x + ":" + y;
        }

        private static bool TryParseCellSaveKey(string key, out int x, out int y)
        {
            x = -1;
            y = -1;
            if (string.IsNullOrEmpty(key))
            {
                return false;
            }

            var split = key.Split(':');
            return split.Length == 2 && int.TryParse(split[0], out x) && int.TryParse(split[1], out y);
        }

        private static string[] ResolveDecorations(FossickCellConfig config)
        {
            var source = config.decorations != null && config.decorations.Count > 0 ? config.decorations : config.decor;
            if (source == null || source.Count == 0)
            {
                return new string[0];
            }

            var result = new List<string>();
            for (var i = 0; i < source.Count; i++)
            {
                var id = source[i];
                if (!IsReservedElementArtId(id))
                {
                    result.Add(id);
                }
            }

            return result.ToArray();
        }

        private static bool IsReservedElementArtId(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return false;
            }

            switch (id)
            {
                case "small_rock":
                case "gold_pile":
                case "pickaxe":
                case "dynamite":
                case "tnt":
                case "radar":
                case "coin_pile":
                case "score_gem":
                case "ore_copper":
                case "ore_orange":
                case "ore_silver":
                case "ore_blue":
                case "ore_gold":
                case "ore_yellow":
                case "ore_gem":
                case "ore_crystal":
                case "treasure_chest":
                case "collection_piece":
                    return true;
            }

            int numericId;
            return int.TryParse(id, out numericId) && numericId >= 1 && numericId <= 37;
        }
    }
}
