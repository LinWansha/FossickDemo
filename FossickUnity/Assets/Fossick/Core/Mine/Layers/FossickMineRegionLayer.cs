using System.Collections.Generic;
using Fossick.Core.Mine.Objects;

namespace Fossick.Core.Mine
{
    public sealed class FossickMineRegionLayer
    {
        private readonly List<FossickRegionObject> regions = new List<FossickRegionObject>();

        public IReadOnlyList<FossickRegionObject> Regions => regions;

        public void Add(FossickRegionObject region)
        {
            if (region != null)
            {
                regions.Add(region);
            }
        }

        public void Clear()
        {
            regions.Clear();
        }

        public FossickRegionObject FindAt(FossickPosition position)
        {
            for (var i = regions.Count - 1; i >= 0; i--)
            {
                var region = regions[i];
                if (region != null && region.Contains(position))
                {
                    return region;
                }
            }

            return null;
        }

        public FossickRegionObject FindAt(FossickPosition position, FossickVisualLayer layer)
        {
            for (var i = regions.Count - 1; i >= 0; i--)
            {
                var region = regions[i];
                if (region != null && region.Layer == layer && region.Contains(position))
                {
                    return region;
                }
            }

            return null;
        }

        public void PruneBefore(int rowIndex)
        {
            for (var i = regions.Count - 1; i >= 0; i--)
            {
                var region = regions[i];
                if (region != null && region.Bounds.y + region.Bounds.height <= rowIndex)
                {
                    regions.RemoveAt(i);
                }
            }
        }
    }
}
