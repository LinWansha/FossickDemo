namespace Fossick.Core.Mine.Objects
{
    public sealed class ToolPickupEntity : FossickPickupEntity
    {
        public ToolPickupEntity(FossickRewardPayload payload, FossickPosition position)
            : base("pickup_tool", payload, position)
        {
        }
    }
}
