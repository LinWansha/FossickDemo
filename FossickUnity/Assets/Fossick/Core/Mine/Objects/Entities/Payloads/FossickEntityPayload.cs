using Fossick.Core.Application;
using Fossick.Core.Application.Results;
using Fossick.Core;
using Fossick.Core.Definition.Config;
using System;

namespace Fossick.Core.Mine.Objects
{
    public abstract class FossickEntityPayload
    {
        protected FossickEntityPayload(FossickElementType elementType, string id, int amount)
        {
            ElementType = elementType;
            Id = id;
            Amount = amount;
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

        public static FossickEntityPayload FromConfig(
            FossickElementConfig config,
            IFossickRewardProvider rewardProvider)
        {
            if (config == null || config.type == FossickElementType.None)
            {
                return null;
            }

            var amount = rewardProvider.GetValue(config.type, config.id);

            switch (config.type)
            {
                case FossickElementType.Ore:
                    return new OrePayload(config.id, amount);
                case FossickElementType.Coin:
                    return new CoinPayload(config.id, amount);
                case FossickElementType.Item:
                    return new ToolPayload(config.id, amount);
                case FossickElementType.Chest:
                    return new ChestPayload(config.id, amount);
                case FossickElementType.Collection:
                    return new CollectionPayload(config.id, amount);
                default:
                    return null;
            }
        }
    }

}
