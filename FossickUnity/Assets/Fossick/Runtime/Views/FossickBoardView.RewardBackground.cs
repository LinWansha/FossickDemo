using Fossick.Core.Mine;
using Fossick.Core.Mine.Objects;
using Fossick.Core.Visual;

namespace Fossick.Runtime.Views
{
    public sealed partial class FossickBoardView
    {
        private int RenderRewardBackgroundRegions(FossickMine mine, float cellSize)
        {
            var used = 0;
            var firstRow = currentFirstRenderedRow;
            var lastRow = firstRow + currentRenderedRowCount;
            var regions = mine.Regions;
            for (var i = 0; i < regions.Count; i++)
            {
                if (!(regions[i] is RewardBackdropRegion region) ||
                    region.Bounds.y >= lastRow ||
                    region.Bounds.y + region.Bounds.height <= firstRow)
                {
                    continue;
                }

                var sprite = FossickArtLibrary.GetRewardBackgroundSprite(region.AssetId);
                used = RenderOptionalSprite(
                    rewardBackgroundImages,
                    rewardBackgroundRoot,
                    "Reward Background Region",
                    used,
                    sprite,
                    sprite != null,
                    region.Bounds.x * cellSize,
                    GetRenderTop(region.Bounds.y - firstRow, cellSize),
                    region.Bounds.width * cellSize,
                    region.Bounds.height * cellSize,
                    false);
            }

            DisableUnused(rewardBackgroundImages, used);
            return used;
        }
    }
}
