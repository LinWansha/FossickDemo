using Fossick.Core.Definition.Config;
using UnityEngine;

namespace Fossick.Preview.Controllers
{
    public sealed class FossickPreviewRewardProvider : IFossickRewardProvider
    {
        private const int TerrainCoinDropChancePerMille = 100;

        public int GetValue(FossickElementType elementType, string id)
        {
            switch (elementType)
            {
                case FossickElementType.Ore:
                    return GetOreScore(id);
                case FossickElementType.Coin:
                    return GetCoinValue(id);
                case FossickElementType.Item:
                case FossickElementType.Collection:
                case FossickElementType.Chest:
                    return 1;
                default:
                    return 0;
            }
        }

        public string PickCoinDropId()
        {
            var roll = Random.Range(0, 100);
            if (roll < 65)
            {
                return FossickContentIds.Reward.CoinDropSmall;
            }

            return roll < 90
                ? FossickContentIds.Reward.CoinDropMedium
                : FossickContentIds.Reward.CoinDropLarge;
        }

        public bool TryPickTerrainCoinDropId(out string id)
        {
            if (Random.Range(1, 1001) > TerrainCoinDropChancePerMille)
            {
                id = null;
                return false;
            }

            id = PickCoinDropId();
            return true;
        }

        private static int GetOreScore(string id)
        {
            switch (id)
            {
                case FossickContentIds.Reward.OreCopper:
                    return 10;
                case FossickContentIds.Reward.OreSilver:
                    return 20;
                case FossickContentIds.Reward.OreGold:
                    return 40;
                case FossickContentIds.Reward.OreGem:
                    return 80;
                default:
                    return 0;
            }
        }

        private static int GetCoinValue(string id)
        {
            switch (id)
            {
                case FossickContentIds.Reward.CoinDropSmall:
                case FossickContentIds.Reward.CoinPileSmall:
                    return 5;
                case FossickContentIds.Reward.CoinDropMedium:
                    return 10;
                case FossickContentIds.Reward.CoinDropLarge:
                case FossickContentIds.Reward.CoinPileLarge:
                    return 20;
                default:
                    return 0;
            }
        }
    }
}
