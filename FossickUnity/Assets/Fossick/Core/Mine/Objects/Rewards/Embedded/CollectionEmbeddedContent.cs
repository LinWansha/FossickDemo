namespace Fossick.Core.Mine.Objects
{
    public sealed class CollectionEmbeddedContent : FossickEmbeddedContent
    {
        public CollectionEmbeddedContent(FossickRewardPayload payload, string attachmentAssetId, FossickPosition position)
            : base("embedded_collection", payload, attachmentAssetId, position)
        {
        }
    }
}
