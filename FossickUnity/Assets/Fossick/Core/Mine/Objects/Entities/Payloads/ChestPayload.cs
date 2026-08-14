using Fossick.Core;
using Fossick.Core.Definition.Config;

namespace Fossick.Core.Mine.Objects
{
    public sealed class ChestPayload : FossickEntityPayload
    {
        public ChestPayload(string chestId, int amount)
            : base(FossickElementType.Chest, chestId, amount)
        {
        }
    }
}
