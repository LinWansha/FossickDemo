using Fossick.Core;
using Fossick.Core.Definition.Config;

namespace Fossick.Core.Mine.Objects
{
    public static class FossickTerrainFactory
    {
        public static FossickTerrainBlock Create(FossickTerrainType terrain, FossickPosition position)
        {
            return Create(terrain, 0, position);
        }

        public static FossickTerrainBlock Create(FossickTerrainType terrain, int hp, FossickPosition position)
        {
            switch (terrain)
            {
                case FossickTerrainType.Dirt:
                    return new FossickDirtTerrain(hp, position);
                case FossickTerrainType.Stone:
                    return new FossickStoneTerrain(hp, position);
                case FossickTerrainType.Unbreakable:
                    return new FossickBedrockTerrain(position);
                case FossickTerrainType.Explosives:
                    return new FossickExplosivesTerrain(hp, position);
                default:
                    return null;
            }
        }
    }
}
