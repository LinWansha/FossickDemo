namespace Fossick.Core.Mine.Objects
{
    public sealed class BackgroundRegion : FossickRegionObject
    {
        public BackgroundRegion(string regionId, FossickRect bounds, string assetId)
            : base(regionId, FossickVisualLayer.Background, bounds, assetId)
        {
        }
    }
}
