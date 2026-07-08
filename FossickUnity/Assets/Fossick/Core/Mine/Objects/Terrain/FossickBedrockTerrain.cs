using Fossick.Core;
using Fossick.Core.Definition.Config;

namespace Fossick.Core.Mine.Objects
{
    public sealed class FossickBedrockTerrain : FossickTerrainBlock
    {
        public const string Id = "terrain_bedrock";

        public FossickBedrockTerrain(FossickPosition position)
            : base(Id, FossickTerrainType.Unbreakable, 0, position)
        {
        }

        public override bool CanDig => false;

        public override bool Damage(int damage)
        {
            return false;
        }
    }
}
