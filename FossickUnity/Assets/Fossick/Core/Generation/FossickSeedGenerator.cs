using System;

namespace Fossick.Core.Generation
{
    public static class FossickSeedGenerator
    {
        private const uint FnvOffsetBasis = 2166136261u;
        private const uint FnvPrime = 16777619u;

        public static int CreatePlayerSeed(string playerId, int activityId, string activitySubType)
        {
            if (string.IsNullOrEmpty(playerId))
            {
                throw new ArgumentException("Player id cannot be empty.", nameof(playerId));
            }

            return CreateStableSeed($"{playerId}|{activityId}|{activitySubType}");
        }

        public static int CreatePreviewSeed()
        {
            return ToPositiveSeed(Guid.NewGuid().GetHashCode());
        }

        private static int CreateStableSeed(string source)
        {
            unchecked
            {
                var hash = FnvOffsetBasis;
                for (var index = 0; index < source.Length; index++)
                {
                    hash = (hash ^ source[index]) * FnvPrime;
                }

                return ToPositiveSeed((int)hash);
            }
        }

        private static int ToPositiveSeed(int value)
        {
            var seed = value & int.MaxValue;
            return seed == 0 ? 1 : seed;
        }
    }
}
