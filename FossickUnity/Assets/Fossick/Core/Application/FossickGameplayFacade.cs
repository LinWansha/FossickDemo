using System;
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

            var beforeSnapshot = session.CreateSnapshot();
            var result = session.Execute(command);
            var afterSnapshot = session.CreateSnapshot();

            if (result == null || !result.isApplied)
            {
                adapters.Telemetry?.Track(command, result, afterSnapshot);
                return result;
            }

            adapters.Reward?.Commit(result, session.Data.Rewards);
            adapters.Storage?.Save(session.CaptureGameplayData());
            adapters.Telemetry?.Track(command, result, afterSnapshot);

            var presentation = new PresentationCompletion(
                () => adapters.View?.Refresh(session.Data, result, afterSnapshot));
            var context = new FossickAnimationContext(command, result, beforeSnapshot, afterSnapshot);
            if (adapters.Animation == null || !adapters.Animation.Play(context, presentation.Complete))
            {
                presentation.Complete();
            }

            return result;
        }

        public FossickActionResult UseTool(FossickToolType toolType, FossickPosition target)
        {
            return Execute(new FossickUseToolCommand(toolType, target));
        }

        private sealed class PresentationCompletion
        {
            private Action onCompleted;

            public PresentationCompletion(Action onCompleted)
            {
                this.onCompleted = onCompleted;
            }

            public void Complete()
            {
                var callback = onCompleted;
                onCompleted = null;
                callback?.Invoke();
            }
        }
    }
}
