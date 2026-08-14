namespace Fossick.Core.Mine.Objects
{
    public sealed class ToolEmbeddedContent : FossickEmbeddedContent
    {
        public ToolEmbeddedContent(FossickEntityPayload payload, string attachmentAssetId, FossickPosition position)
            : base("embedded_tool", payload, attachmentAssetId, position)
        {
        }
    }
}
