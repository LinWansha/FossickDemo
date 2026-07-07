namespace Fossick.Core.Mine.Objects
{
    public abstract class FossickRegionObject
    {
        protected FossickRegionObject(string regionId, FossickVisualLayer layer, FossickRect bounds, string assetId)
        {
            RegionId = regionId;
            Layer = layer;
            Bounds = bounds;
            AssetId = assetId;
        }

        public string RegionId { get; }
        public FossickVisualLayer Layer { get; }
        public FossickRect Bounds { get; }
        public string AssetId { get; }

        public bool Contains(FossickPosition position)
        {
            return Bounds.Contains(position);
        }
    }

    public sealed class BackgroundRegion : FossickRegionObject
    {
        public BackgroundRegion(string regionId, FossickRect bounds, string assetId)
            : base(regionId, FossickVisualLayer.Background, bounds, assetId)
        {
        }
    }

    public sealed class RewardBackdropRegion : FossickRegionObject
    {
        public RewardBackdropRegion(string regionId, FossickRect bounds, string assetId)
            : base(regionId, FossickVisualLayer.RewardBackground, bounds, assetId)
        {
        }
    }

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
