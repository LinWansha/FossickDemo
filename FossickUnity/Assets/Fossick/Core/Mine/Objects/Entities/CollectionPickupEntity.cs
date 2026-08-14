namespace Fossick.Core.Mine.Objects
{
    public sealed class CollectionPickupEntity : FossickPickupEntity
    {
        public CollectionPickupEntity(FossickEntityPayload payload, FossickPosition position)
            : base("pickup_collection", payload, position)
        {
        }
    }
}
