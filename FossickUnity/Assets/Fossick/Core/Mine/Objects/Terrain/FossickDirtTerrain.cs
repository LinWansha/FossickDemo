using Fossick.Core;
using Fossick.Core.Definition.Config;

namespace Fossick.Core.Mine.Objects
{
    public sealed class FossickDirtTerrain : FossickTerrainBlock
    {
        public const string Id = "terrain_dirt";

        public FossickDirtTerrain(int hp, FossickPosition position)
            : base(Id, FossickTerrainType.Dirt, hp <= 0 ? 1 : hp, position)
        {
        }

        public override bool CanDig => true;
    }
}
