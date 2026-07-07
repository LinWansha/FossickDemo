using Fossick.Core.Definition.Config;

namespace Fossick.Core.Mine.Objects
{
    public sealed class FossickTerrainInstance : FossickCellObject
    {
        public FossickTerrainInstance(FossickTerrainType terrain, int hp, FossickPosition position)
            : base(terrain.ToString(), FossickVisualLayer.Terrain, position)
        {
            Terrain = terrain;
            MaxHp = hp <= 0 ? GetDefaultHp(terrain) : hp;
            Hp = MaxHp;
        }

        public FossickTerrainType Terrain { get; }
        public int MaxHp { get; }
        public int Hp { get; private set; }
        public bool IsObstacle => Terrain != FossickTerrainType.Empty;
        public bool CanDig => Terrain != FossickTerrainType.Empty && Terrain != FossickTerrainType.Unbreakable && Hp > 0;
        public bool IsDestroyed => !IsObstacle || Hp <= 0;

        public bool Damage(int damage)
        {
            if (!CanDig)
            {
                return false;
            }

            Hp -= damage <= 0 ? 1 : damage;
            if (Hp < 0)
            {
                Hp = 0;
            }

            return true;
        }

        private static int GetDefaultHp(FossickTerrainType terrain)
        {
            return terrain == FossickTerrainType.Stone ? 2 : terrain == FossickTerrainType.Empty ? 0 : 1;
        }
    }
}
