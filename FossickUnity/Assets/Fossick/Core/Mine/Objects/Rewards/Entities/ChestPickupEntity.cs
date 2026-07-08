namespace Fossick.Core.Mine.Objects
{
    public sealed class ChestPickupEntity : FossickPickupEntity
    {
        public ChestPickupEntity(FossickRewardPayload payload, FossickPosition position)
            : base("pickup_chest", payload, position)
        {
        }
    }
}
