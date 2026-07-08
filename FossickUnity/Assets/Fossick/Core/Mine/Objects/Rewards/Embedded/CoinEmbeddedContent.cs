namespace Fossick.Core.Mine.Objects
{
    public sealed class CoinEmbeddedContent : FossickEmbeddedContent
    {
        public CoinEmbeddedContent(FossickRewardPayload payload, string attachmentAssetId, FossickPosition position)
            : base("embedded_coin", payload, attachmentAssetId, position)
        {
        }
    }
}
