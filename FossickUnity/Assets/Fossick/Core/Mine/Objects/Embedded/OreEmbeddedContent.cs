namespace Fossick.Core.Mine.Objects
{
    public sealed class OreEmbeddedContent : FossickEmbeddedContent
    {
        public OreEmbeddedContent(FossickEntityPayload payload, string attachmentAssetId, FossickPosition position)
            : base("embedded_ore", payload, attachmentAssetId, position)
        {
        }
    }
}
