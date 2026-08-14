namespace Fossick.Core.Mine.Objects
{
    public sealed class OrePickupEntity : FossickPickupEntity
    {
        public OrePickupEntity(FossickEntityPayload payload, FossickPosition position)
            : base("pickup_ore", payload, position)
        {
        }
    }
}
