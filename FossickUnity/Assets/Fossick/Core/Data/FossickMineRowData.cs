using System;
using System.Collections.Generic;

namespace Fossick.Core.Data
{
    [Serializable]
    public sealed class FossickMineRowData
    {
        public int rowIndex;
        public List<FossickCellData> cells = new List<FossickCellData>();
    }
}
