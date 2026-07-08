using Fossick.Core;
using Fossick.Core.Definition.Config;

namespace Fossick.Core.Mine.Objects
{
    public abstract class FossickTerrainBlock : FossickCellObject
    {
        protected FossickTerrainBlock(string objectId, FossickTerrainType terrain, int hp, FossickPosition position)
            : base(objectId, FossickVisualLayer.Terrain, position)
        {
            Terrain = terrain;
            MaxHp = hp < 0 ? 0 : hp;
            Hp = MaxHp;
        }

        public FossickTerrainType Terrain { get; }
        public int MaxHp { get; }
        public int Hp { get; private set; }
        public bool IsObstacle => Terrain != FossickTerrainType.Empty && !IsDestroyed;
        public bool IsDestroyed => CanDig && Hp <= 0;
        public virtual bool IsTriggerable => false;
        public abstract bool CanDig { get; }

        public virtual bool Damage(int damage)
        {
            if (!CanDig || Hp <= 0)
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
    }
}
