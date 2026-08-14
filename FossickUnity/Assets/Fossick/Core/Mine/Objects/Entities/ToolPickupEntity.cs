namespace Fossick.Core.Mine.Objects
{
    public sealed class ToolPickupEntity : FossickPickupEntity
    {
        public ToolPickupEntity(FossickEntityPayload payload, FossickPosition position)
            : base("pickup_tool", payload, position)
        {
        }
    }
}
