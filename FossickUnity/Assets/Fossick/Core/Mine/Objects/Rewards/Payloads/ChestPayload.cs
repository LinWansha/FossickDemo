using Fossick.Core;
using Fossick.Core.Definition.Config;

namespace Fossick.Core.Mine.Objects
{
    public sealed class ChestPayload : FossickRewardPayload
    {
        public ChestPayload(string chestId, int amount)
            : base(FossickElementType.Chest, chestId, amount)
        {
        }
    }
}
