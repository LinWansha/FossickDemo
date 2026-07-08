using System;
using System.Collections.Generic;

namespace Fossick.Core.Data
{
    [Serializable]
    public sealed class FossickMineData
    {
        public int loadedStartRow;
        public int topVisibleRow;
        public int depth;
        public List<FossickMineRowData> loadedRows = new List<FossickMineRowData>();
    }
}
