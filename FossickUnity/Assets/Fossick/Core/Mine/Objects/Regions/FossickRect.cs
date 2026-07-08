namespace Fossick.Core.Mine.Objects
{
    public struct FossickRect
    {
        public readonly int x;
        public readonly int y;
        public readonly int width;
        public readonly int height;

        public FossickRect(int x, int y, int width, int height)
        {
            this.x = x;
            this.y = y;
            this.width = width < 0 ? 0 : width;
            this.height = height < 0 ? 0 : height;
        }

        public bool Contains(FossickPosition position)
        {
            return position.x >= x && position.x < x + width && position.y >= y && position.y < y + height;
        }
    }
}
