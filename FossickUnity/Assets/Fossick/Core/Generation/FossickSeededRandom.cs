using System;

namespace Fossick.Core.Generation
{
    public sealed class FossickSeededRandom
    {
        private readonly Random random;

        public int Seed { get; }

        public FossickSeededRandom(int seed)
        {
            Seed = seed;
            random = new Random(seed);
        }

        public int RangeInclusive(int min, int max)
        {
            if (min > max)
            {
                var temp = min;
                min = max;
                max = temp;
            }

            return random.Next(min, max + 1);
        }

        public int RangeExclusive(int min, int max)
        {
            return random.Next(min, max);
        }
    }
}
