using System.Collections.Generic;
using Fossick.Core.Definition.Config;

namespace Fossick.Core.Visual.Tiling
{
    public static class FossickAutoTileResolver
    {
        private const int TopLeft = 1;
        private const int TopRight = 2;
        private const int BottomLeft = 4;
        private const int BottomRight = 8;

        public static int ResolveConfigCornerAssetIndex(IReadOnlyList<FossickCellConfig[]> rows, int cornerX, int cornerY, FossickTerrainType terrain)
        {
            if (rows == null || terrain == FossickTerrainType.Empty)
            {
                return 0;
            }

            return ResolveMask(
                ConfigCellMatches(rows, cornerX - 1, cornerY - 1, terrain),
                ConfigCellMatches(rows, cornerX, cornerY - 1, terrain),
                ConfigCellMatches(rows, cornerX - 1, cornerY, terrain),
                ConfigCellMatches(rows, cornerX, cornerY, terrain)).spriteIndex;
        }

        public static int ResolveCornerAssetIndex(bool topLeft, bool topRight, bool bottomLeft, bool bottomRight)
        {
            return ResolveMask(topLeft, topRight, bottomLeft, bottomRight).spriteIndex;
        }

        public static FossickAutoTileResult ResolveMask(bool topLeft, bool topRight, bool bottomLeft, bool bottomRight)
        {
            var mask = 0;
            if (topLeft)
            {
                mask |= TopLeft;
            }

            if (topRight)
            {
                mask |= TopRight;
            }

            if (bottomLeft)
            {
                mask |= BottomLeft;
            }

            if (bottomRight)
            {
                mask |= BottomRight;
            }

            return new FossickAutoTileResult(MapCornerMaskToSpriteIndex(mask), mask, false);
        }

        private static int MapCornerMaskToSpriteIndex(int mask)
        {
            switch (mask)
            {
                case TopRight | BottomLeft:
                    return 9;
                case TopLeft | BottomRight:
                    return 6;
                default:
                    return mask;
            }
        }

        private static bool ConfigCellMatches(IReadOnlyList<FossickCellConfig[]> rows, int x, int y, FossickTerrainType terrain)
        {
            if (y < 0 || y >= rows.Count)
            {
                return false;
            }

            var row = rows[y];
            if (row == null || x < 0 || x >= row.Length)
            {
                return false;
            }

            var cell = row[x];
            return cell != null && cell.terrain == terrain;
        }
    }
}
