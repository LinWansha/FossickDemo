using System.Collections.Generic;
using System;

namespace Fossick.Core.Mine
{
    public sealed class FossickMineRow
    {
        private readonly FossickCell[] cells;

        public FossickMineRow(int depth, FossickCell[] cells)
        {
            Depth = depth;
            this.cells = cells;
        }

        public int Depth { get; }
        public IReadOnlyList<FossickCell> Cells => cells;

        internal FossickCell[] CellArray => cells;

        public FossickCell GetCell(int x)
        {
            return x < 0 || x >= cells.Length ? null : cells[x];
        }
    }
}
