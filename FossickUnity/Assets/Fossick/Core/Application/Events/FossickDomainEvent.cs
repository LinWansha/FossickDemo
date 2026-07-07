using Fossick.Core.Definition.Config;

namespace Fossick.Core.Application.Events
{
    public enum FossickDomainEventType
    {
        ToolUsed = 0,
        TerrainDamaged = 1,
        TerrainDestroyed = 2,
        EmbeddedContentRevealed = 3,
        PickupCollected = 4,
        FogRevealed = 5,
        MineScrolled = 6
    }

    public sealed class FossickDomainEvent
    {
        public FossickDomainEventType type;
        public int x;
        public int y;
        public FossickElementType elementType;
        public string id;
        public int amount;

        public static FossickDomainEvent PickupCollected(int x, int y, FossickElementType elementType, string id, int amount)
        {
            return new FossickDomainEvent
            {
                type = FossickDomainEventType.PickupCollected,
                x = x,
                y = y,
                elementType = elementType,
                id = id,
                amount = amount
            };
        }

        public static FossickDomainEvent FogRevealed(int x, int y)
        {
            return new FossickDomainEvent
            {
                type = FossickDomainEventType.FogRevealed,
                x = x,
                y = y
            };
        }

        public static FossickDomainEvent MineScrolled(int depth)
        {
            return new FossickDomainEvent
            {
                type = FossickDomainEventType.MineScrolled,
                amount = depth
            };
        }
    }
}
