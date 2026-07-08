namespace Fossick.Core.Mine.Objects
{
    public sealed class CoinPickupEntity : FossickPickupEntity
    {
        public CoinPickupEntity(FossickRewardPayload payload, FossickPosition position)
            : base("pickup_coin", payload, position)
        {
        }
    }
}
