using System.Collections.Generic;
using Fossick.Core.Config;

namespace Fossick.Core.Validation
{
    public static class FossickMapValidator
    {
        public static FossickValidationResult Validate(FossickMapConfig config)
        {
            var result = new FossickValidationResult();

            if (config == null)
            {
                result.Add(FossickValidationSeverity.Error, "Map config is null.");
                return result;
            }

            if (config.activity != "Fossick")
            {
                result.Add(FossickValidationSeverity.Error, "Activity must be Fossick.");
            }

            var boardSpec = config.BoardSpec;
            if (!boardSpec.IsValid)
            {
                result.Add(FossickValidationSeverity.Error, "Board width and visible height must be greater than zero.");
            }

            ValidateGeneration(config, result);
            ValidateFragments(config, result);

            return result;
        }

        private static void ValidateGeneration(FossickMapConfig config, FossickValidationResult result)
        {
            var generation = config.generation;
            if (generation == null)
            {
                result.Add(FossickValidationSeverity.Error, "Generation config is missing.");
                return;
            }

            if (generation.regularGroupSize <= 0)
            {
                result.Add(FossickValidationSeverity.Error, "Regular group size must be greater than zero.");
            }

            if (generation.rewardInsertMin <= 0 || generation.rewardInsertMax <= 0 || generation.rewardInsertMin > generation.rewardInsertMax)
            {
                result.Add(FossickValidationSeverity.Error, "Reward insert range is invalid.");
            }

            var total = 0;
            if (generation.difficultyCounts == null || generation.difficultyCounts.Count == 0)
            {
                result.Add(FossickValidationSeverity.Error, "At least one difficulty count is required.");
                return;
            }

            var difficulties = new HashSet<int>();
            for (var i = 0; i < generation.difficultyCounts.Count; i++)
            {
                var entry = generation.difficultyCounts[i];
                if (entry == null)
                {
                    result.Add(FossickValidationSeverity.Error, "Difficulty count entry is null.");
                    continue;
                }

                if (entry.difficulty <= 0)
                {
                    result.Add(FossickValidationSeverity.Error, "Difficulty must be greater than zero.");
                }

                if (entry.count <= 0)
                {
                    result.Add(FossickValidationSeverity.Error, "Difficulty count must be greater than zero.");
                }

                if (!difficulties.Add(entry.difficulty))
                {
                    result.Add(FossickValidationSeverity.Error, $"Difficulty {entry.difficulty} is duplicated in generation config.");
                }

                total += entry.count;
            }

            if (generation.regularGroupSize > 0 && total != generation.regularGroupSize)
            {
                result.Add(FossickValidationSeverity.Warning, $"Difficulty counts total {total}, but regular group size is {generation.regularGroupSize}.");
            }
        }

        private static void ValidateFragments(FossickMapConfig config, FossickValidationResult result)
        {
            if (config.fragments == null || config.fragments.Count == 0)
            {
                result.Add(FossickValidationSeverity.Error, "At least one fragment is required.");
                return;
            }

            var ids = new HashSet<int>();
            var regularDifficulties = new HashSet<int>();
            var hasReward = false;
            var hasTutorial = false;

            for (var i = 0; i < config.fragments.Count; i++)
            {
                var fragment = config.fragments[i];
                if (fragment == null)
                {
                    result.Add(FossickValidationSeverity.Error, "Fragment entry is null.");
                    continue;
                }

                if (!ids.Add(fragment.id))
                {
                    result.Add(FossickValidationSeverity.Error, $"Fragment id {fragment.id} is duplicated.", fragment.id);
                }

                if (fragment.width != config.boardWidth)
                {
                    result.Add(FossickValidationSeverity.Error, $"Fragment width {fragment.width} must match board width {config.boardWidth}.", fragment.id);
                }

                if (fragment.height <= 0)
                {
                    result.Add(FossickValidationSeverity.Error, "Fragment height must be greater than zero.", fragment.id);
                }

                if (fragment.type == FossickFragmentType.Regular)
                {
                    if (fragment.difficulty <= 0)
                    {
                        result.Add(FossickValidationSeverity.Error, "Regular fragment must have a difficulty greater than zero.", fragment.id);
                    }
                    else
                    {
                        regularDifficulties.Add(fragment.difficulty);
                    }
                }
                else if (fragment.type == FossickFragmentType.Reward)
                {
                    hasReward = true;
                }
                else if (fragment.type == FossickFragmentType.Tutorial)
                {
                    hasTutorial = true;
                }

                ValidateCells(config, fragment, result);
            }

            if (!hasTutorial)
            {
                result.Add(FossickValidationSeverity.Warning, "No tutorial fragments found.");
            }

            if (!hasReward)
            {
                result.Add(FossickValidationSeverity.Warning, "No reward fragments found.");
            }

            ValidateDifficultyCoverage(config, regularDifficulties, result);
        }

        private static void ValidateCells(FossickMapConfig config, FossickFragmentConfig fragment, FossickValidationResult result)
        {
            if (fragment.cells == null)
            {
                result.Add(FossickValidationSeverity.Warning, "Fragment cells are missing. Empty cells will be assumed.", fragment.id);
                return;
            }

            var occupied = new HashSet<int>();
            for (var i = 0; i < fragment.cells.Count; i++)
            {
                var cell = fragment.cells[i];
                if (cell == null)
                {
                    result.Add(FossickValidationSeverity.Error, "Cell entry is null.", fragment.id);
                    continue;
                }

                if (cell.x < 0 || cell.x >= config.boardWidth || cell.y < 0 || cell.y >= fragment.height)
                {
                    result.Add(FossickValidationSeverity.Error, "Cell coordinate is out of bounds.", fragment.id, cell.x, cell.y);
                    continue;
                }

                var key = cell.y * config.boardWidth + cell.x;
                if (!occupied.Add(key))
                {
                    result.Add(FossickValidationSeverity.Error, "Cell coordinate is duplicated.", fragment.id, cell.x, cell.y);
                }

                if (cell.hp < 0)
                {
                    result.Add(FossickValidationSeverity.Error, "Cell hp cannot be negative.", fragment.id, cell.x, cell.y);
                }

                if (cell.terrain != FossickTerrainType.Empty && cell.hp == 0 && cell.terrain != FossickTerrainType.Unbreakable)
                {
                    result.Add(FossickValidationSeverity.Warning, "Breakable terrain should have hp greater than zero.", fragment.id, cell.x, cell.y);
                }

                var reward = cell.reward ?? cell.element;
                if (reward != null && reward.type != FossickElementType.None)
                {
                    if (reward.amount < 0)
                    {
                        result.Add(FossickValidationSeverity.Error, "Reward amount cannot be negative.", fragment.id, cell.x, cell.y);
                    }

                    if (cell.terrain == FossickTerrainType.Unbreakable)
                    {
                        result.Add(FossickValidationSeverity.Warning, "Reward is buried under unbreakable terrain.", fragment.id, cell.x, cell.y);
                    }

                    if (reward.type == FossickElementType.Ore && !CanAttachOre(cell))
                    {
                        result.Add(FossickValidationSeverity.Error, "Ore must be attached to diggable terrain.", fragment.id, cell.x, cell.y);
                    }
                }

                if (cell.decorations != null && cell.decor != null && cell.decorations.Count > 0 && cell.decor.Count > 0)
                {
                    result.Add(FossickValidationSeverity.Warning, "Cell uses both legacy decor and layered decorations.", fragment.id, cell.x, cell.y);
                }

                if (!string.IsNullOrEmpty(cell.rewardBackgroundId) && fragment.type != FossickFragmentType.Reward)
                {
                    result.Add(FossickValidationSeverity.Warning, "Reward background is usually reserved for reward fragments.", fragment.id, cell.x, cell.y);
                }
            }
        }

        private static void ValidateDifficultyCoverage(FossickMapConfig config, HashSet<int> regularDifficulties, FossickValidationResult result)
        {
            if (config.generation == null || config.generation.difficultyCounts == null)
            {
                return;
            }

            for (var i = 0; i < config.generation.difficultyCounts.Count; i++)
            {
                var entry = config.generation.difficultyCounts[i];
                if (entry != null && entry.difficulty > 0 && !regularDifficulties.Contains(entry.difficulty))
                {
                    result.Add(FossickValidationSeverity.Error, $"Generation requires difficulty {entry.difficulty}, but no regular fragment uses it.");
                }
            }
        }

        private static bool CanAttachOre(FossickCellConfig cell)
        {
            return cell != null
                && cell.terrain != FossickTerrainType.Empty
                && cell.terrain != FossickTerrainType.Unbreakable
                && cell.hp > 0;
        }
    }
}
