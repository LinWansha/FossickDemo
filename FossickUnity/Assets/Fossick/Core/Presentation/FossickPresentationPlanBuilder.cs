using Fossick.Core.Actions;
using Fossick.Core.Config;

namespace Fossick.Core.Presentation
{
    public static class FossickPresentationPlanBuilder
    {
        public static FossickPresentationPlan BuildRejected(FossickToolType toolType, int x, int y, string reason)
        {
            var plan = new FossickPresentationPlan
            {
                toolType = toolType,
                targetX = x,
                targetY = y,
                isApplied = false,
                toolConsumed = false,
                invalidReason = reason
            };
            plan.events.Add(new FossickPresentationEvent
            {
                type = FossickPresentationEventType.InvalidTarget,
                sourceStepType = FossickActionStepType.InvalidTarget,
                toolType = toolType,
                x = x,
                y = y
            });
            return plan;
        }

        public static FossickPresentationPlan Build(FossickActionResult action)
        {
            if (action == null)
            {
                return null;
            }

            var plan = new FossickPresentationPlan
            {
                toolType = action.toolType,
                targetX = action.targetX,
                targetY = action.targetY,
                isApplied = action.isApplied,
                isCollectOnly = action.isCollectOnly,
                toolConsumed = action.toolConsumed,
                invalidReason = action.invalidReason,
                depthBeforeAction = action.depthBeforeAction,
                depthAfterAction = action.depthAfterAction,
                totalScrollRows = action.scrollCount
            };

            var scrollIndex = 0;
            for (var i = 0; i < action.steps.Count; i++)
            {
                var step = action.steps[i];
                if (!TryMapEventType(step.type, out var eventType))
                {
                    continue;
                }

                var presentationEvent = new FossickPresentationEvent
                {
                    type = eventType,
                    sourceStepType = step.type,
                    toolType = action.toolType,
                    x = step.x,
                    y = step.y,
                    elementType = step.elementType,
                    id = step.id,
                    amount = step.amount
                };

                ApplyMatchingDelta(action, presentationEvent);
                if (step.type == FossickActionStepType.BoardScrolled)
                {
                    presentationEvent.scrollRows = 1;
                    presentationEvent.depthBefore = action.depthBeforeAction + scrollIndex;
                    scrollIndex++;
                    presentationEvent.depthAfter = action.depthBeforeAction + scrollIndex;
                }

                plan.events.Add(presentationEvent);
            }

            return plan;
        }

        private static bool TryMapEventType(FossickActionStepType stepType, out FossickPresentationEventType eventType)
        {
            switch (stepType)
            {
                case FossickActionStepType.InvalidTarget:
                    eventType = FossickPresentationEventType.InvalidTarget;
                    return true;
                case FossickActionStepType.ToolConsumed:
                    eventType = FossickPresentationEventType.ToolConsumed;
                    return true;
                case FossickActionStepType.ObstacleHit:
                    eventType = FossickPresentationEventType.ObstacleHit;
                    return true;
                case FossickActionStepType.ObstacleBroken:
                    eventType = FossickPresentationEventType.ObstacleBroken;
                    return true;
                case FossickActionStepType.RewardRevealed:
                    eventType = FossickPresentationEventType.RewardSpawned;
                    return true;
                case FossickActionStepType.RewardCollected:
                    eventType = FossickPresentationEventType.RewardCollected;
                    return true;
                case FossickActionStepType.RewardAutoCollected:
                    eventType = FossickPresentationEventType.RewardAutoCollected;
                    return true;
                case FossickActionStepType.RewardMissed:
                    eventType = FossickPresentationEventType.RewardMissed;
                    return true;
                case FossickActionStepType.FogRevealed:
                    eventType = FossickPresentationEventType.FogRevealed;
                    return true;
                case FossickActionStepType.RadarScanned:
                    eventType = FossickPresentationEventType.RadarScanned;
                    return true;
                case FossickActionStepType.BoardScrolled:
                    eventType = FossickPresentationEventType.BoardScrolled;
                    return true;
                default:
                    eventType = FossickPresentationEventType.InvalidTarget;
                    return false;
            }
        }

        private static void ApplyMatchingDelta(FossickActionResult action, FossickPresentationEvent presentationEvent)
        {
            for (var i = 0; i < action.cellDeltas.Count; i++)
            {
                var delta = action.cellDeltas[i];
                if (delta.x != presentationEvent.x || delta.y != presentationEvent.y)
                {
                    continue;
                }

                presentationEvent.terrainBefore = delta.terrainBefore;
                presentationEvent.terrainAfter = delta.terrainAfter;
                presentationEvent.hpBefore = delta.hpBefore;
                presentationEvent.hpAfter = delta.hpAfter;
                presentationEvent.fogBefore = delta.fogBefore;
                presentationEvent.fogAfter = delta.fogAfter;
                return;
            }
        }
    }
}
