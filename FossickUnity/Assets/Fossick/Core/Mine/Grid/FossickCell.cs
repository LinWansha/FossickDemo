using System.Collections.Generic;
using Fossick.Core.Mine.Objects;

namespace Fossick.Core.Mine
{
    public sealed class FossickCell
    {
        private readonly List<FossickDecorationObject> decorations = new List<FossickDecorationObject>();

        public FossickCell(FossickPosition position)
        {
            Position = position;
            Fog = new FossickFogState(true);
        }

        public FossickPosition Position { get; }
        public FossickTerrainBlock Terrain { get; set; }
        public FossickEmbeddedContent FossickEmbeddedContent { get; set; }
        public FossickPickupEntity Pickup { get; private set; }
        public FossickFogState Fog { get; set; }
        public IReadOnlyList<FossickDecorationObject> Decorations => decorations;
        public bool HasObstacle => HasBlockingTerrain;
        public bool HasBlockingTerrain => Terrain != null && Terrain.IsObstacle && !Terrain.IsDestroyed;
        public bool HasPickup => Pickup != null && !Pickup.Collected;
        public bool IsVisible => Fog == null || Fog.IsVisible;
        public bool HasDiggableTerrain => Terrain != null && Terrain.CanDig;
        public bool HasTriggerableTerrain => Terrain != null && Terrain.IsTriggerable && !Terrain.IsDestroyed;
        public bool IsPassable => !HasObstacle;
        public bool HasCollectablePickup => Pickup != null && !Pickup.Collected;

        public void ClearTerrain()
        {
            Terrain = null;
        }

        public void ClearEmbeddedContent()
        {
            FossickEmbeddedContent = null;
        }

        public void SetPickup(FossickPickupEntity entity)
        {
            Pickup = entity;
        }

        public void ClearPickup()
        {
            Pickup = null;
        }

        public void AddDecoration(FossickDecorationObject decoration)
        {
            if (decoration != null)
            {
                decorations.Add(decoration);
            }
        }
    }
}
