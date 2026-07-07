using System.Collections.Generic;
using Fossick.Core.Application;
using Fossick.Core.Application.Results;
using Fossick.Core.Definition.Config;
using Fossick.Core.Application.Events;
using Fossick.Core.Mine;
using Fossick.Core.Mine.Objects;

namespace Fossick.Core.Systems
{
    public sealed class FossickVisibilitySystem : FossickSystem
    {
        public FossickVisibilitySystem()
            : base("Visibility")
        {
        }

        public void RefreshFromOpenSpace(FossickMine mine, FossickActionResult result)
        {
            if (mine == null)
            {
                return;
            }

            AddFogRevealDeltas(mine.RefreshFogFromOpenSpace(), result);
        }

        public void ApplyRadarReveal(FossickCell cell, FossickActionResult result)
        {
            if (cell == null || result == null || cell.IsVisible)
            {
                return;
            }

            result.cellDeltas.Add(new FossickCellDelta
            {
                x = cell.Position.x,
                y = cell.Position.y,
                fogBefore = FossickFogType.Covered,
                fogAfter = FossickFogType.None
            });

            if (cell.Fog == null)
            {
                cell.Fog = new FossickFogState(true);
            }
            else
            {
                cell.Fog.Reveal();
            }

            result.isApplied = true;
            AddStep(result, FossickActionStepType.RadarScanned, cell.Position.x, cell.Position.y);
            result.domainEvents.Add(FossickDomainEvent.FogRevealed(cell.Position.x, cell.Position.y));
        }

        public void AddFogRevealDeltas(IReadOnlyList<FossickMineFogReveal> reveals, FossickActionResult result)
        {
            if (reveals == null || result == null)
            {
                return;
            }

            for (var i = 0; i < reveals.Count; i++)
            {
                var reveal = reveals[i];
                if (reveal == null)
                {
                    continue;
                }

                result.cellDeltas.Add(new FossickCellDelta
                {
                    x = reveal.x,
                    y = reveal.y,
                    fogBefore = reveal.wasVisible ? FossickFogType.None : FossickFogType.Covered,
                    fogAfter = reveal.isVisible ? FossickFogType.None : FossickFogType.Covered
                });
                AddStep(result, FossickActionStepType.FogRevealed, reveal.x, reveal.y);
                result.domainEvents.Add(FossickDomainEvent.FogRevealed(reveal.x, reveal.y));
            }
        }

        private static void AddStep(FossickActionResult result, FossickActionStepType type, int x, int y)
        {
            result.steps.Add(new FossickActionStep
            {
                type = type,
                x = x,
                y = y
            });
        }
    }
}
