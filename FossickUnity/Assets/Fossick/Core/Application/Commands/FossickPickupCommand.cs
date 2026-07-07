using Fossick.Core.Mine;

namespace Fossick.Core.Application.Commands
{
    public sealed class FossickPickupCommand : FossickCommand
    {
        public FossickPickupCommand(FossickPosition target)
            : base("pickup")
        {
            Target = target;
        }

        public FossickPosition Target { get; }
    }
}
