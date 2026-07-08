using Fossick.Core;
using Fossick.Core.Definition.Config;

namespace Fossick.Core.Mine.Objects
{
    public abstract class FossickPickupEntity : FossickCellObject
    {
        protected FossickPickupEntity(string objectId, FossickRewardPayload payload, FossickPosition position)
            : base(objectId, FossickVisualLayer.Reward, position)
        {
            Payload = payload;
        }

        public FossickRewardPayload Payload { get; }
        public bool Collected { get; private set; }

        public bool Collect()
        {
            if (Collected || Payload == null)
            {
                return false;
            }

            Collected = true;
            Visible = false;
            return true;
        }

        public static FossickPickupEntity FromPayload(FossickRewardPayload payload, FossickPosition position)
        {
            if (payload == null)
            {
                return null;
            }

            switch (payload.ElementType)
            {
                case FossickElementType.Ore:
                    return new OrePickupEntity(payload, position);
                case FossickElementType.Coin:
                    return new CoinPickupEntity(payload, position);
                case FossickElementType.Item:
                    return new ToolPickupEntity(payload, position);
                case FossickElementType.Chest:
                    return new ChestPickupEntity(payload, position);
                case FossickElementType.Collection:
                    return new CollectionEntity(payload, position);
                default:
                    return null;
            }
        }
    }

}
