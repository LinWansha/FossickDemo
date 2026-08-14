namespace Fossick.Core.Mine.Objects
{
    public sealed class CoinPickupEntity : FossickPickupEntity
    {
        public CoinPickupEntity(FossickEntityPayload payload, FossickPosition position)
            : base("pickup_coin", payload, position)
        {
        }
    }
}
