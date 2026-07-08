using Fossick.Core;
using Fossick.Core.Definition.Config;

namespace Fossick.Core.Mine.Objects
{
    public sealed class ToolPayload : FossickRewardPayload
    {
        public ToolPayload(string toolId, int count)
            : base(FossickElementType.Item, toolId, count)
        {
        }
    }
}
