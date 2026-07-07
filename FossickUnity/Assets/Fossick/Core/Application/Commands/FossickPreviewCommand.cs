namespace Fossick.Core.Application.Commands
{
    public sealed class FossickPreviewCommand : FossickCommand
    {
        public FossickPreviewCommand(int seed, int startDepth)
            : base("preview")
        {
            Seed = seed;
            StartDepth = startDepth < 0 ? 0 : startDepth;
        }

        public int Seed { get; }
        public int StartDepth { get; }
    }
}
