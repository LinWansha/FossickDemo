namespace Fossick.Core.Mine.Objects
{
    public sealed class CollectionEmbeddedContent : FossickEmbeddedContent
    {
        public CollectionEmbeddedContent(FossickEntityPayload payload, string attachmentAssetId, FossickPosition position)
            : base("embedded_collection", payload, attachmentAssetId, position)
        {
        }
    }
}
