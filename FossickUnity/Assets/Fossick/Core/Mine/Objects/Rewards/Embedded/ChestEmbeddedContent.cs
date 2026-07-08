namespace Fossick.Core.Mine.Objects
{
    public sealed class ChestEmbeddedContent : FossickEmbeddedContent
    {
        public ChestEmbeddedContent(FossickRewardPayload payload, string attachmentAssetId, FossickPosition position)
            : base("embedded_chest", payload, attachmentAssetId, position)
        {
        }
    }
}
