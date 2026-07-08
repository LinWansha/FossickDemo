using Fossick.Core.Application;
using Fossick.Core.Application.Results;
using Fossick.Core;
using Fossick.Core.Definition.Config;

namespace Fossick.Core.Mine.Objects
{
    public abstract class FossickRewardPayload
    {
        protected FossickRewardPayload(FossickElementType elementType, string id, int amount)
        {
            ElementType = elementType;
            Id = id;
            Amount = amount <= 0 ? 1 : amount;
        }

        public FossickElementType ElementType { get; }
        public string Id { get; }
        public int Amount { get; }

        public FossickRewardEvent ToRewardEvent(FossickPosition position)
        {
            return new FossickRewardEvent
            {
                elementType = ElementType,
                id = Id,
                amount = Amount,
                x = position.x,
                y = position.y
            };
        }

        public static FossickRewardPayload FromConfig(FossickElementConfig config)
        {
            if (config == null || config.type == FossickElementType.None)
            {
                return null;
            }

            switch (config.type)
            {
                case FossickElementType.Ore:
                    return new OrePayload(config.id, config.amount);
                case FossickElementType.Coin:
                    return new CoinPayload(config.id, config.amount);
                case FossickElementType.Item:
                    return new ToolPayload(config.id, config.amount);
                case FossickElementType.Chest:
                    return new ChestPayload(config.id, config.amount);
                case FossickElementType.Collection:
                    return new CollectionPayload(config.id, config.amount);
                default:
                    return null;
            }
        }
    }

}
