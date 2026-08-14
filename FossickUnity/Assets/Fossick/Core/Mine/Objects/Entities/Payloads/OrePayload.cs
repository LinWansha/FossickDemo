using Fossick.Core;
using Fossick.Core.Definition.Config;

namespace Fossick.Core.Mine.Objects
{
    public sealed class OrePayload : FossickEntityPayload
    {
        public OrePayload(string oreId, int score)
            : base(FossickElementType.Ore, oreId, score)
        {
            OreId = oreId;
            Score = Amount;
        }

        public string OreId { get; }
        public int Score { get; }
    }
}
