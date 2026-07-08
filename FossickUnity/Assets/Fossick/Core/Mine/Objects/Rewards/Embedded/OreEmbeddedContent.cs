namespace Fossick.Core.Mine.Objects
{
    public sealed class OreEmbeddedContent : FossickEmbeddedContent
    {
        public OreEmbeddedContent(FossickRewardPayload payload, string attachmentAssetId, FossickPosition position)
            : base("embedded_ore", payload, attachmentAssetId, position)
        {
        }
    }
}
