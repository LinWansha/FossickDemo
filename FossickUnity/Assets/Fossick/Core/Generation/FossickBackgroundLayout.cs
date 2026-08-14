using System.Collections.Generic;
using Fossick.Core.Definition.Config;

namespace Fossick.Core.Generation
{
    public sealed class FossickBackgroundLayout
    {
        public const int RegionHeight = 3;

        private readonly int seed;
        private readonly IReadOnlyList<string> backgroundIds;

        public FossickBackgroundLayout(int seed, FossickVisualConfig visual)
        {
            this.seed = seed;
            backgroundIds = visual.backgroundIds;
        }

        public int GetRegionStartRow(int absoluteRow)
        {
            return absoluteRow - PositiveModulo(absoluteRow, RegionHeight);
        }

        public string GetBackgroundId(int absoluteRow)
        {
            var regionIndex = GetRegionStartRow(absoluteRow) / RegionHeight;
            var index = (int)(Mix((uint)seed, (uint)regionIndex) % (uint)backgroundIds.Count);
            return backgroundIds[index];
        }

        private static uint Mix(uint seedValue, uint regionIndex)
        {
            var value = seedValue ^ (regionIndex + 0x9e3779b9u + (seedValue << 6) + (seedValue >> 2));
            value ^= value >> 16;
            value *= 0x7feb352du;
            value ^= value >> 15;
            value *= 0x846ca68bu;
            value ^= value >> 16;
            return value;
        }

        private static int PositiveModulo(int value, int divisor)
        {
            var result = value % divisor;
            return result < 0 ? result + divisor : result;
        }
    }
}
