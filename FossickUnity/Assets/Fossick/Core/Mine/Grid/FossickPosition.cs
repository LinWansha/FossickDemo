using System;

namespace Fossick.Core.Mine
{
    public struct FossickPosition : IEquatable<FossickPosition>
    {
        public readonly int x;
        public readonly int y;

        public FossickPosition(int x, int y)
        {
            this.x = x;
            this.y = y;
        }

        public bool Equals(FossickPosition other)
        {
            return x == other.x && y == other.y;
        }

        public override bool Equals(object obj)
        {
            return obj is FossickPosition other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (x * 397) ^ y;
            }
        }

        public override string ToString()
        {
            return x + "," + y;
        }
    }
}
