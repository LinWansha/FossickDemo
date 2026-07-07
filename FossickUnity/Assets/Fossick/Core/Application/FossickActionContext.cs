using Fossick.Core.Application.Commands;
using Fossick.Core.Definition.Config;
using Fossick.Core.Mine;
using Fossick.Core.State;

namespace Fossick.Core.Application
{
    public sealed class FossickActionContext
    {
        public FossickActionContext(FossickMapConfig config, FossickRuntimeState state, FossickCommand command, int seed)
        {
            Config = config ?? FossickSampleMapFactory.CreateDefaultConfig();
            State = state;
            Command = command;
            Seed = seed;
        }

        public FossickMapConfig Config { get; }
        public FossickRuntimeState State { get; }
        public FossickCommand Command { get; }
        public int Seed { get; }
    }
}
