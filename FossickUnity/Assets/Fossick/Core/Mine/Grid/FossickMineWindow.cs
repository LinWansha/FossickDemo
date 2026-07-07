namespace Fossick.Core.Mine
{
    public sealed class FossickMineWindow
    {
        public FossickMineWindow(int topDepth, int visibleWidth, int visibleHeight)
        {
            TopDepth = topDepth < 0 ? 0 : topDepth;
            VisibleWidth = visibleWidth < 0 ? 0 : visibleWidth;
            VisibleHeight = visibleHeight < 0 ? 0 : visibleHeight;
        }

        public int TopDepth { get; private set; }
        public int VisibleWidth { get; }
        public int VisibleHeight { get; }

        public void MoveTo(int topDepth)
        {
            TopDepth = topDepth < 0 ? 0 : topDepth;
        }
    }
}
