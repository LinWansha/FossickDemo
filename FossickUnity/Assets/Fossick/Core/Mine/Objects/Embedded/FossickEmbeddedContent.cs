using Fossick.Core;
using Fossick.Core.Definition.Config;

namespace Fossick.Core.Mine.Objects
{
    public abstract class FossickEmbeddedContent : FossickCellObject
    {
        protected FossickEmbeddedContent(string objectId, FossickEntityPayload payload, string attachmentAssetId, FossickPosition position)
            : base(objectId, FossickVisualLayer.TerrainAttachment, position)
        {
            Payload = payload;
            AttachmentAssetId = attachmentAssetId;
        }

        public FossickEntityPayload Payload { get; }
        public string AttachmentAssetId { get; }

        public FossickPickupEntity SpawnPickup(FossickPosition position)
        {
            return FossickPickupEntity.FromPayload(Payload, position);
        }

        public static FossickEmbeddedContent FromPayload(FossickEntityPayload payload, string attachmentAssetId, FossickPosition position)
        {
            if (payload == null)
            {
                return null;
            }

            switch (payload.ElementType)
            {
                case FossickElementType.Ore:
                    return new OreEmbeddedContent(payload, attachmentAssetId, position);
                case FossickElementType.Item:
                    return new ToolEmbeddedContent(payload, attachmentAssetId, position);
                case FossickElementType.Chest:
                    return new ChestEmbeddedContent(payload, attachmentAssetId, position);
                case FossickElementType.Collection:
                    return new CollectionEmbeddedContent(payload, attachmentAssetId, position);
                case FossickElementType.Coin:
                    return new CoinEmbeddedContent(payload, attachmentAssetId, position);
                default:
                    return null;
            }
        }
    }

}
