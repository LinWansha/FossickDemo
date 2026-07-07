using Fossick.Core.Definition.Config;

namespace Fossick.Core.Generation
{
    public sealed class FossickGeneratedFragment
    {
        public FossickFragmentConfig config;
        public int sequenceIndex;
        public bool insertedAsReward;

        public FossickGeneratedFragment(FossickFragmentConfig config, int sequenceIndex, bool insertedAsReward)
        {
            this.config = config;
            this.sequenceIndex = sequenceIndex;
            this.insertedAsReward = insertedAsReward;
        }
    }
}
