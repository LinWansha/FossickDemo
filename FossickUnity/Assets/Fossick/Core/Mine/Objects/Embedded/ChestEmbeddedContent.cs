namespace Fossick.Core.Mine.Objects
{
    public sealed class ChestEmbeddedContent : FossickEmbeddedContent
    {
        public ChestEmbeddedContent(FossickEntityPayload payload, string attachmentAssetId, FossickPosition position)
            : base("embedded_chest", payload, attachmentAssetId, position)
        {
        }
    }
}
