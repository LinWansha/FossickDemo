using Fossick.Core;
using Fossick.Core.Definition.Config;

namespace Fossick.Core.Mine.Objects
{
    public sealed class FossickExplosivesTerrain : FossickTerrainBlock
    {
        public const string Id = "explosive_crate";
        public const int BlastRadius = 1;
        public const int BlastDamage = 2;

        public FossickExplosivesTerrain(int hp, FossickPosition position)
            : base(Id, FossickTerrainType.Explosives, hp <= 0 ? 1 : hp, position)
        {
        }

        public override bool CanDig => true;
        public override bool IsTriggerable => true;
    }
}
