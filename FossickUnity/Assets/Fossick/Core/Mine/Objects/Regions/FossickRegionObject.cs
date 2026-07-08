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

}
