namespace Fossick.Core.Visual.Tiling
{
    public readonly struct FossickAutoTileResult
    {
        public readonly int spriteIndex;
        public readonly int mask;
        public readonly bool specialCase;

        public FossickAutoTileResult(int spriteIndex, int mask, bool specialCase)
        {
            this.spriteIndex = spriteIndex;
            this.mask = mask;
            this.specialCase = specialCase;
        }
    }
}
