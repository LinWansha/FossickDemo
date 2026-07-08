using Fossick.Core.Application.Adapters;

namespace Fossick.Core.Application
{
    public sealed class FossickGameplayAdapters
    {
        public static readonly FossickGameplayAdapters Empty = new FossickGameplayAdapters();

        public IFossickViewAdapter View { get; set; }
        public IFossickAnimationAdapter Animation { get; set; }
        public IFossickRewardAdapter Reward { get; set; }
        public IFossickStorageAdapter Storage { get; set; }
        public IFossickTelemetryAdapter Telemetry { get; set; }
    }
}
