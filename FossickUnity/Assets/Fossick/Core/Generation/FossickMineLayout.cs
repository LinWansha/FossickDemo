using System.Collections.Generic;
using Fossick.Core.Config;

namespace Fossick.Core.Generation
{
    public sealed class FossickGeneratedMine
    {
        public readonly List<FossickGeneratedMineRow> rows = new List<FossickGeneratedMineRow>();
        public readonly List<FossickGeneratedFragmentSpan> fragments = new List<FossickGeneratedFragmentSpan>();
    }

    public sealed class FossickGeneratedFragmentSpan
    {
        public FossickFragmentConfig config;
        public int sequenceIndex;
        public int fragmentId;
        public FossickFragmentType fragmentType;
        public bool insertedAsReward;
        public int difficulty;
        public int startRow;
        public int height;
    }

    public sealed class FossickGeneratedMineRow
    {
        public int rowIndex;
        public int localRow;
        public FossickGeneratedFragmentSpan fragment;
        public FossickCellConfig[] cells;
    }

    public static class FossickMineLayoutBuilder
    {
        public static FossickGeneratedMine Build(FossickMapConfig config, int seed, int targetRows)
        {
            return Build(config, seed, targetRows, null);
        }

        public static FossickGeneratedMine Build(FossickMapConfig config, int seed, int targetRows, IDictionary<int, FossickFragmentConfig> sequenceOverrides)
        {
            var mine = new FossickGeneratedMine();
            if (config == null || targetRows <= 0)
            {
                return mine;
            }

            var generator = new FossickFragmentGenerator(config, seed);
            var initial = generator.GenerateInitialFragments();
            for (var i = 0; i < initial.Count && mine.rows.Count < targetRows; i++)
            {
                AppendFragment(mine, initial[i], sequenceOverrides);
            }

            while (mine.rows.Count < targetRows)
            {
                AppendFragment(mine, generator.Next(), sequenceOverrides);
            }

            ApplyRowOverrides(mine, config.generation == null ? null : config.generation.rowOverrides);
            return mine;
        }

        private static void ApplyRowOverrides(FossickGeneratedMine mine, List<FossickRowOverrideConfig> rowOverrides)
        {
            if (mine == null || rowOverrides == null)
            {
                return;
            }

            for (var i = 0; i < rowOverrides.Count; i++)
            {
                var item = rowOverrides[i];
                if (item == null || item.fragment == null || item.fragment.height <= 0)
                {
                    continue;
                }

                for (var y = 0; y < item.fragment.height; y++)
                {
                    var rowIndex = item.startRow + y;
                    if (rowIndex < 0 || rowIndex >= mine.rows.Count)
                    {
                        continue;
                    }

                    var row = mine.rows[rowIndex];
                    if (row == null)
                    {
                        continue;
                    }

                    var width = row.cells == null ? item.fragment.width : row.cells.Length;
                    row.cells = new FossickCellConfig[width];
                    for (var x = 0; x < width; x++)
                    {
                        row.cells[x] = CloneCell(FindCell(item.fragment, x, y), x, y);
                    }
                }
            }
        }

        private static void AppendFragment(FossickGeneratedMine mine, FossickGeneratedFragment generated, IDictionary<int, FossickFragmentConfig> sequenceOverrides)
        {
            var originalFragment = generated.config;
            var fragment = ResolveFragment(generated, sequenceOverrides);
            if (fragment == null)
            {
                return;
            }

            var span = new FossickGeneratedFragmentSpan
            {
                config = fragment,
                sequenceIndex = generated.sequenceIndex,
                fragmentId = originalFragment == null ? fragment.id : originalFragment.id,
                fragmentType = originalFragment == null ? fragment.type : originalFragment.type,
                insertedAsReward = generated.insertedAsReward,
                difficulty = originalFragment == null ? fragment.difficulty : originalFragment.difficulty,
                startRow = mine.rows.Count,
                height = fragment.height
            };
            mine.fragments.Add(span);

            for (var y = 0; y < fragment.height; y++)
            {
                var row = new FossickGeneratedMineRow
                {
                    rowIndex = mine.rows.Count,
                    localRow = y,
                    fragment = span,
                    cells = new FossickCellConfig[fragment.width]
                };

                for (var x = 0; x < fragment.width; x++)
                {
                    row.cells[x] = CloneCell(FindCell(fragment, x, y), x, y);
                }

                mine.rows.Add(row);
            }
        }

        private static FossickFragmentConfig ResolveFragment(FossickGeneratedFragment generated, IDictionary<int, FossickFragmentConfig> sequenceOverrides)
        {
            if (generated == null)
            {
                return null;
            }

            if (sequenceOverrides != null && sequenceOverrides.TryGetValue(generated.sequenceIndex, out var fragmentOverride) && fragmentOverride != null)
            {
                return fragmentOverride;
            }

            return generated.config;
        }

        private static FossickCellConfig FindCell(FossickFragmentConfig fragment, int x, int y)
        {
            if (fragment.cells == null)
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

        private static FossickCellConfig CloneCell(FossickCellConfig source, int x, int y)
        {
            if (source == null)
            {
                return new FossickCellConfig
                {
                    x = x,
                    y = y,
                    terrain = FossickTerrainType.Empty
                };
            }

            return new FossickCellConfig
            {
                x = x,
                y = y,
                backgroundId = source.backgroundId,
                rewardBackgroundId = source.rewardBackgroundId,
                terrain = source.terrain,
                hp = source.hp,
                reward = source.reward,
                decorations = source.decorations == null ? new List<string>() : new List<string>(source.decorations),
                fog = source.fog,
                element = source.element,
                decor = source.decor == null ? new List<string>() : new List<string>(source.decor),
                mask = source.mask
            };
        }
    }
}
