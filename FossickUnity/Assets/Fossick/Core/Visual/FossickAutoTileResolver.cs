using Fossick.Core.Config;

namespace Fossick.Core.Visual
{
    public static class FossickAutoTileResolver
    {
        private const int Up = 1;
        private const int Down = 2;
        private const int Left = 4;
        private const int Right = 8;

        public static FossickAutoTileResult Resolve(FossickFragmentConfig fragment, FossickCellConfig cell)
        {
            if (fragment == null || cell == null || cell.terrain == FossickTerrainType.Empty)
            {
                return new FossickAutoTileResult(0, 0, false);
            }

            var mask = 0;
            var up = IsConnected(fragment, cell, 0, -1);
            var down = IsConnected(fragment, cell, 0, 1);
            var left = IsConnected(fragment, cell, -1, 0);
            var right = IsConnected(fragment, cell, 1, 0);

            if (up)
            {
                mask |= Up;
            }

            if (down)
            {
                mask |= Down;
            }

            if (left)
            {
                mask |= Left;
            }

            if (right)
            {
                mask |= Right;
            }

            var spriteIndex = ResolveSpriteIndex(fragment, cell, mask);
            return new FossickAutoTileResult(spriteIndex, mask, spriteIndex > 16);
        }

        private static int ResolveSpriteIndex(FossickFragmentConfig fragment, FossickCellConfig cell, int mask)
        {
            switch (mask)
            {
                case 0:
                    return 16;
                case Down | Right:
                    return 8;
                case Down | Left:
                    return 4;
                case Up | Right:
                    return 2;
                case Up | Left:
                    return 1;
                case Left | Right:
                    return 12;
                case Up | Down:
                    return 10;
                case Down | Left | Right:
                    return 12;
                case Up | Left | Right:
                    return 3;
                case Up | Down | Right:
                    return ResolveLeftEdgeDetail(fragment, cell);
                case Up | Down | Left:
                    return ResolveRightEdgeDetail(fragment, cell);
                case Up | Down | Left | Right:
                    return ResolveInnerDetail(fragment, cell);
                case Down:
                    return 8;
                case Up:
                    return 2;
                case Right:
                    return 8;
                case Left:
                    return 4;
                default:
                    return 15;
            }
        }

        private static int ResolveLeftEdgeDetail(FossickFragmentConfig fragment, FossickCellConfig cell)
        {
            if (!IsConnected(fragment, cell, -1, -1))
            {
                return 14;
            }

            if (!IsConnected(fragment, cell, -1, 1))
            {
                return 11;
            }

            return 10;
        }

        private static int ResolveRightEdgeDetail(FossickFragmentConfig fragment, FossickCellConfig cell)
        {
            if (!IsConnected(fragment, cell, 1, -1))
            {
                return 13;
            }

            if (!IsConnected(fragment, cell, 1, 1))
            {
                return 7;
            }

            return 5;
        }

        private static int ResolveInnerDetail(FossickFragmentConfig fragment, FossickCellConfig cell)
        {
            var emptyUpLeft = !IsConnected(fragment, cell, -1, -1);
            var emptyUpRight = !IsConnected(fragment, cell, 1, -1);
            var emptyDownLeft = !IsConnected(fragment, cell, -1, 1);
            var emptyDownRight = !IsConnected(fragment, cell, 1, 1);

            if (emptyUpLeft)
            {
                return 9;
            }

            if (emptyUpRight)
            {
                return 6;
            }

            if (emptyDownLeft)
            {
                return 18;
            }

            if (emptyDownRight)
            {
                return 17;
            }

            return 15;
        }

        private static bool IsConnected(FossickFragmentConfig fragment, FossickCellConfig origin, int dx, int dy)
        {
            var other = FindCell(fragment, origin.x + dx, origin.y + dy);
            return other != null && other.terrain == origin.terrain;
        }

        private static FossickCellConfig FindCell(FossickFragmentConfig fragment, int x, int y)
        {
            if (x < 0 || x >= fragment.width || y < 0 || y >= fragment.height || fragment.cells == null)
            {
                return null;
            }

            for (var i = 0; i < fragment.cells.Count; i++)
            {
                var cell = fragment.cells[i];
                if (cell != null && cell.x == x && cell.y == y)
                {
                    return cell;
                }
            }

            return null;
        }

        private static bool CellMatches(FossickFragmentConfig fragment, int x, int y, FossickTerrainType terrain)
        {
            var cell = FindCell(fragment, x, y);
            return cell != null && cell.terrain == terrain;
        }
    }
}
