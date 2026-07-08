namespace Fossick.Core.Mine.Objects
{
    public sealed class CollectionEntity : FossickPickupEntity
    {
        public CollectionEntity(FossickRewardPayload payload, FossickPosition position)
            : base("pickup_collection", payload, position)
        {
        }
    }
}
