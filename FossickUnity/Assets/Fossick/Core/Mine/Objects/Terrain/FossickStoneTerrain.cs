using Fossick.Core;
using Fossick.Core.Definition.Config;

namespace Fossick.Core.Mine.Objects
{
    public sealed class FossickStoneTerrain : FossickTerrainBlock
    {
        public const string Id = "terrain_stone";

        public FossickStoneTerrain(int hp, FossickPosition position)
            : base(Id, FossickTerrainType.Stone, hp <= 0 ? 2 : hp, position)
        {
        }

        public override bool CanDig => true;
    }
}
