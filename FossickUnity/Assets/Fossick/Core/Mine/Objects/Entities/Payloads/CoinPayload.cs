using Fossick.Core;
using Fossick.Core.Definition.Config;

namespace Fossick.Core.Mine.Objects
{
    public sealed class CoinPayload : FossickEntityPayload
    {
        public CoinPayload(string coinId, int amount)
            : base(FossickElementType.Coin, coinId, amount)
        {
        }
    }
}
