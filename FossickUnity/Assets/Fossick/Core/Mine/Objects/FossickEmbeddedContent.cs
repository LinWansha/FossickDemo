using Fossick.Core.Definition.Config;

namespace Fossick.Core.Mine.Objects
{
    public abstract class FossickEmbeddedContent : FossickCellObject
    {
        protected FossickEmbeddedContent(string objectId, FossickRewardPayload payload, string attachmentAssetId, FossickPosition position)
            : base(objectId, FossickVisualLayer.TerrainAttachment, position)
        {
            Payload = payload;
            AttachmentAssetId = attachmentAssetId;
        }

        public FossickRewardPayload Payload { get; }
        public string AttachmentAssetId { get; }

        public FossickPickupEntity SpawnPickup(FossickPosition position)
        {
            return FossickPickupEntity.FromPayload(Payload, position);
        }

        public static FossickEmbeddedContent FromPayload(FossickRewardPayload payload, string attachmentAssetId, FossickPosition position)
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

    public sealed class OreEmbeddedContent : FossickEmbeddedContent
    {
        public OreEmbeddedContent(FossickRewardPayload payload, string attachmentAssetId, FossickPosition position)
            : base("embedded_ore", payload, attachmentAssetId, position)
        {
        }
    }

    public sealed class ToolEmbeddedContent : FossickEmbeddedContent
    {
        public ToolEmbeddedContent(FossickRewardPayload payload, string attachmentAssetId, FossickPosition position)
            : base("embedded_tool", payload, attachmentAssetId, position)
        {
        }
    }

    public sealed class ChestEmbeddedContent : FossickEmbeddedContent
    {
        public ChestEmbeddedContent(FossickRewardPayload payload, string attachmentAssetId, FossickPosition position)
            : base("embedded_chest", payload, attachmentAssetId, position)
        {
        }
    }

    public sealed class CollectionEmbeddedContent : FossickEmbeddedContent
    {
        public CollectionEmbeddedContent(FossickRewardPayload payload, string attachmentAssetId, FossickPosition position)
            : base("embedded_collection", payload, attachmentAssetId, position)
        {
        }
    }

    public sealed class CoinEmbeddedContent : FossickEmbeddedContent
    {
        public CoinEmbeddedContent(FossickRewardPayload payload, string attachmentAssetId, FossickPosition position)
            : base("embedded_coin", payload, attachmentAssetId, position)
        {
        }
    }
}
