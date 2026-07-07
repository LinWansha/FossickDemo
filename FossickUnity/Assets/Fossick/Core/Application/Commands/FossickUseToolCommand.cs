using Fossick.Core.Definition.Config;
using Fossick.Core.Mine;

namespace Fossick.Core.Application.Commands
{
    public sealed class FossickUseToolCommand : FossickCommand
    {
        public FossickUseToolCommand(FossickToolType toolType, FossickPosition target)
            : base("use_tool")
        {
            ToolType = toolType;
            Target = target;
        }

        public FossickToolType ToolType { get; }
        public FossickPosition Target { get; }
    }
}
