namespace Fossick.Core.Mine.Objects
{
    public sealed class ToolEmbeddedContent : FossickEmbeddedContent
    {
        public ToolEmbeddedContent(FossickRewardPayload payload, string attachmentAssetId, FossickPosition position)
            : base("embedded_tool", payload, attachmentAssetId, position)
        {
        }
    }
}
