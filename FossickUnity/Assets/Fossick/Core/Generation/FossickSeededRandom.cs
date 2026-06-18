namespace Fossick.Core.Generation
{
    public sealed class FossickSeededRandom
    {
        private const uint Multiplier = 1664525u;
        private const uint Increment = 1013904223u;

        public int Seed { get; }
        public int State => unchecked((int)state);

        private uint state;

        public FossickSeededRandom(int seed)
            : this(seed, seed)
        {
        }

        public FossickSeededRandom(int seed, int state)
        {
            Seed = seed;
            this.state = unchecked((uint)state);
            if (this.state == 0u)
            {
                this.state = 0x6d2b79f5u;
            }
        }

        public int RangeInclusive(int min, int max)
        {
            if (min > max)
            {
                var temp = min;
                min = max;
                max = temp;
            }

            var range = (uint)(max - min + 1);
            return min + (int)(NextUInt() % range);
        }

        public int RangeExclusive(int min, int max)
        {
            if (min >= max)
            {
                return min;
            }

            var range = (uint)(max - min);
            return min + (int)(NextUInt() % range);
        }

        private uint NextUInt()
        {
            state = unchecked(state * Multiplier + Increment);
            return state;
        }
    }
}
