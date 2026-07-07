using System;

namespace Fossick.Core.Definition.Config
{
    [Serializable]
    public struct FossickBoardSpec
    {
        public const int DefaultWidth = 7;
        public const int DefaultVisibleHeight = 6;

        public int width;
        public int visibleHeight;

        public FossickBoardSpec(int width, int visibleHeight)
        {
            this.width = width;
            this.visibleHeight = visibleHeight;
        }

        public static FossickBoardSpec Default => new FossickBoardSpec(DefaultWidth, DefaultVisibleHeight);

        public bool IsValid => width > 0 && visibleHeight > 0;
    }
}
