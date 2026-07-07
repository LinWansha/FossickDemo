using Fossick.Core.Application;
using Fossick.Core.Application.Results;
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

    public sealed class OrePayload : FossickRewardPayload
    {
        public OrePayload(string oreId, int score)
            : base(FossickElementType.Ore, oreId, score)
        {
            OreId = oreId;
            Score = Amount;
        }

        public string OreId { get; }
        public int Score { get; }
    }

    public sealed class CoinPayload : FossickRewardPayload
    {
        public CoinPayload(string coinId, int amount)
            : base(FossickElementType.Coin, coinId, amount)
        {
        }
    }

    public sealed class ToolPayload : FossickRewardPayload
    {
        public ToolPayload(string toolId, int count)
            : base(FossickElementType.Item, toolId, count)
        {
        }
    }

    public sealed class ChestPayload : FossickRewardPayload
    {
        public ChestPayload(string chestId, int amount)
            : base(FossickElementType.Chest, chestId, amount)
        {
        }
    }

    public sealed class CollectionPayload : FossickRewardPayload
    {
        public CollectionPayload(string collectionId, int amount)
            : base(FossickElementType.Collection, collectionId, amount)
        {
            CollectionId = collectionId;
        }

        public string CollectionId { get; }
    }
}
