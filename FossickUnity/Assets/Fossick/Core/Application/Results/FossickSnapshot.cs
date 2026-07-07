using Fossick.Core.Definition.Config;

namespace Fossick.Core.Application.Results
{
    public sealed class FossickSnapshot
    {
        public FossickSnapshot(FossickBoardSpec spec, int topVisibleRow, int depth, int loadedRows)
        {
            Spec = spec;
            TopVisibleRow = topVisibleRow;
            Depth = depth;
            LoadedRows = loadedRows;
        }

        public FossickBoardSpec Spec { get; }
        public int TopVisibleRow { get; }
        public int Depth { get; }
        public int LoadedRows { get; }
    }
}
