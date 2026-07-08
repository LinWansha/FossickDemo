using Fossick.Core.Application;
using Fossick.Core.Application.Commands;
using Fossick.Core.Application.Results;
using Fossick.Core.Application.Adapters;
using Fossick.Core.Data;
using Fossick.Core.Definition.Config;
using Fossick.Core.Mine;

namespace Fossick.Core.Application
{
    public sealed class FossickGameplayFacade
    {
        private readonly FossickGameplaySession session;
        private readonly FossickGameplayAdapters adapters;

        public FossickGameplayFacade(FossickGameplaySession session, FossickGameplayAdapters adapters)
        {
            this.session = session;
            this.adapters = adapters ?? FossickGameplayAdapters.Empty;
        }

        public FossickGameplayData Data => session == null ? null : session.Data;
        public FossickSnapshot Snapshot => session == null ? null : session.CreateSnapshot();

        public FossickActionResult Execute(FossickCommand command)
        {
            if (session == null)
            {
                return null;
            }

            var result = session.Execute(command);
            var snapshot = session.CreateSnapshot();

            if (result == null || !result.isApplied)
            {
                adapters.Telemetry?.Track(command, result, snapshot);
                return result;
            }

            adapters.View?.Refresh(session.Data, result, snapshot);
            adapters.Animation?.Play(result, snapshot);
            adapters.Reward?.Commit(result, session.Data.Rewards);
            adapters.Storage?.Save(session.CaptureGameplayData());
            adapters.Telemetry?.Track(command, result, snapshot);

            return result;
        }

        public FossickActionResult UseTool(FossickToolType toolType, FossickPosition target)
        {
            return Execute(new FossickUseToolCommand(toolType, target));
        }
    }
}
