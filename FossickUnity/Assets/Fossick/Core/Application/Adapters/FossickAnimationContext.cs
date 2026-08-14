using Fossick.Core.Application.Commands;
using Fossick.Core.Application.Results;

namespace Fossick.Core.Application.Adapters
{
    public sealed class FossickAnimationContext
    {
        public FossickAnimationContext(
            FossickCommand command,
            FossickActionResult result,
            FossickSnapshot beforeSnapshot,
            FossickSnapshot afterSnapshot)
        {
            Command = command;
            Result = result;
            BeforeSnapshot = beforeSnapshot;
            AfterSnapshot = afterSnapshot;
        }

        public FossickCommand Command { get; }
        public FossickActionResult Result { get; }
        public FossickSnapshot BeforeSnapshot { get; }
        public FossickSnapshot AfterSnapshot { get; }
    }
}
