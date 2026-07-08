namespace Fossick.Core.Mine.Objects
{
    public sealed class RewardBackdropRegion : FossickRegionObject
    {
        public RewardBackdropRegion(string regionId, FossickRect bounds, string assetId)
            : base(regionId, FossickVisualLayer.RewardBackground, bounds, assetId)
        {
        }
    }
}
