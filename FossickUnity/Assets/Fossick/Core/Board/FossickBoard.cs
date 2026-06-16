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

        public FossickBoardSpec Spec { get; }
        public int TopVisibleRow { get; private set; }
        public int Depth { get; private set; }
        public int RowCount => rows.Count;

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
                var row = CreateEmptyRow(rows.Count);
                for (var i = 0; i < fragment.cells.Count; i++)
                {
                    var cellConfig = fragment.cells[i];
                    if (cellConfig == null || cellConfig.y != y || cellConfig.x < 0 || cellConfig.x >= Spec.width)
                    {
                        continue;
                    }

                    row[cellConfig.x] = CreateCell(cellConfig, cellConfig.x, rows.Count);
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

            for (var i = rows.Count; i < mine.rows.Count; i++)
            {
                AppendGeneratedRow(mine.rows[i]);
            }
        }

        public void AppendGeneratedRow(FossickGeneratedMineRow generatedRow)
        {
            var row = CreateEmptyRow(rows.Count);
            if (generatedRow != null && generatedRow.cells != null)
            {
                for (var x = 0; x < Spec.width && x < generatedRow.cells.Length; x++)
                {
                    row[x] = CreateCell(generatedRow.cells[x], x, rows.Count);
                }
            }

            rows.Add(row);
        }

        public IReadOnlyList<FossickCellState[]> GetVisibleRows()
        {
            var visible = new List<FossickCellState[]>();
            for (var offset = 0; offset < Spec.visibleHeight; offset++)
            {
                var rowIndex = TopVisibleRow + offset;
                if (rowIndex >= rows.Count)
                {
                    visible.Add(CreateEmptyRow(rowIndex));
                }
                else
                {
                    visible.Add(rows[rowIndex]);
                }
            }

            return visible;
        }

        public FossickCellState GetCell(int x, int y)
        {
            var rowIndex = TopVisibleRow + y;
            return GetCellAtAbsoluteRow(x, rowIndex);
        }

        public FossickCellState GetCellAtAbsoluteRow(int x, int rowIndex)
        {
            if (x < 0 || x >= Spec.width || rowIndex < 0 || rowIndex >= rows.Count)
            {
                return null;
            }

            return rows[rowIndex][x];
        }

        public IEnumerable<FossickCellState> EnumerateCells()
        {
            for (var y = 0; y < rows.Count; y++)
            {
                var row = rows[y];
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
                topVisibleRow = TopVisibleRow,
                depth = Depth
            };

            if (progress != null)
            {
                save.oreFound = progress.oreFound;
                save.collectionFound = progress.collectionFound;
                save.toolUsed = progress.toolUsed;
            }

            for (var y = 0; y < rows.Count; y++)
            {
                var row = rows[y];
                if (row == null)
                {
                    continue;
                }

                for (var x = 0; x < row.Length; x++)
                {
                    var cell = row[x];
                    if (cell == null)
                    {
                        continue;
                    }

                    var key = GetCellSaveKey(x, y);
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
            if (rows.Count < Spec.visibleHeight + 1)
            {
                return false;
            }

            return BottomVisibleRowHasVisibleEmptyCell();
        }

        private bool BottomVisibleRowHasVisibleEmptyCell()
        {
            var rowIndex = TopVisibleRow + Spec.visibleHeight - 1;
            if (rowIndex < 0 || rowIndex >= rows.Count)
            {
                return false;
            }

            var row = rows[rowIndex];
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
            var maxRow = visibleBottom + 1 < rows.Count ? visibleBottom + 1 : rows.Count - 1;
            var visited = new bool[maxRow + 1, Spec.width];
            var queue = new Queue<FossickCellState>();

            for (var y = TopVisibleRow; y <= visibleBottom && y < rows.Count; y++)
            {
                for (var x = 0; x < Spec.width; x++)
                {
                    var cell = GetCellAtAbsoluteRow(x, y);
                    if (cell != null && !cell.HasObstacle && cell.fog == FossickFogType.None)
                    {
                        EnqueueOpenCell(x, y, maxRow, visited, queue);
                    }
                }
            }

            while (queue.Count > 0)
            {
                var cell = queue.Dequeue();
                RevealCell(cell, reveals);

                VisitNeighbor(cell.x - 1, cell.y, maxRow, visited, queue, reveals);
                VisitNeighbor(cell.x + 1, cell.y, maxRow, visited, queue, reveals);
                VisitNeighbor(cell.x, cell.y - 1, maxRow, visited, queue, reveals);
                VisitNeighbor(cell.x, cell.y + 1, maxRow, visited, queue, reveals);
            }

            return reveals;
        }

        private void VisitNeighbor(int x, int y, int maxRow, bool[,] visited, Queue<FossickCellState> queue, List<FossickFogReveal> reveals)
        {
            var cell = GetCellAtAbsoluteRow(x, y);
            if (cell == null || y < TopVisibleRow || y > maxRow)
            {
                return;
            }

            if (cell.HasObstacle)
            {
                RevealCell(cell, reveals);
                return;
            }

            EnqueueOpenCell(x, y, maxRow, visited, queue);
        }

        private void EnqueueOpenCell(int x, int y, int maxRow, bool[,] visited, Queue<FossickCellState> queue)
        {
            var cell = GetCellAtAbsoluteRow(x, y);
            if (cell == null || y < TopVisibleRow || y > maxRow || cell.HasObstacle || visited[y, x])
            {
                return;
            }

            visited[y, x] = true;
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
                return 0;
            }

            return rowIndex >= rows.Count ? rows.Count - 1 : rowIndex;
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
