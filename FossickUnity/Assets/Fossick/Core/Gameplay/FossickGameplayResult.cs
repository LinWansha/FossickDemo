using Fossick.Core.Actions;
using Fossick.Core.Config;

namespace Fossick.Core.Gameplay
{
    public sealed class FossickGameplayActionResult
    {
        public FossickToolType toolType;
        public bool notEnoughTool;
        public bool actionWasApplied;
        public FossickActionResult action;
        public int scoreBefore;
        public int scoreAfter;
    }

    public sealed class FossickSettlementResult
    {
        public int depth;
        public int oreFound;
        public int collectionFound;
        public int toolUsed;
        public int remainingCoinAmount;
    }
}
