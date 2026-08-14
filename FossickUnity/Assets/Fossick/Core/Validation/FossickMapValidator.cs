using System.Collections.Generic;
using Fossick.Core.Definition.Config;

namespace Fossick.Core.Validation
{
    public static class FossickMapValidator
    {
        public static FossickValidationResult Validate(FossickMapProjectConfig project)
        {
            var result = new FossickValidationResult();

            if (project == null)
            {
                result.Add(FossickValidationSeverity.Error, "Map project is null.", category: FossickValidationCategory.MapDefinition);
                return result;
            }

            if (project.activity != FossickContentIds.Activity)
            {
                result.Add(FossickValidationSeverity.Error, "Project activity must be Fossick.", category: FossickValidationCategory.MapDefinition);
            }

            if (project.fragmentLibrary == null)
            {
                result.Add(FossickValidationSeverity.Error, "Fragment library is missing.", category: FossickValidationCategory.MapDefinition);
            }

            if (project.generationRules == null)
            {
                result.Add(FossickValidationSeverity.Error, "Generation rules are missing.", category: FossickValidationCategory.GenerationRules);
            }

            if (project.mapDefinition == null)
            {
                result.Add(FossickValidationSeverity.Error, "Map definition is missing.", category: FossickValidationCategory.MapDefinition);
            }

            if (project.fragmentLibrary == null || project.generationRules == null || project.mapDefinition == null)
            {
                return result;
            }

            if (project.mapDefinition.fragmentLibraryId != project.fragmentLibrary.libraryId)
            {
                result.Add(FossickValidationSeverity.Error, "Map definition references a different fragment library.", category: FossickValidationCategory.MapDefinition);
            }

            if (project.mapDefinition.generationRulesId != project.generationRules.rulesId)
            {
                result.Add(FossickValidationSeverity.Error, "Map definition references different generation rules.", category: FossickValidationCategory.MapDefinition);
            }

            var generation = project.generationRules.generation;
            if (generation == null)
            {
                result.Add(FossickValidationSeverity.Error, "Generation config is missing.", category: FossickValidationCategory.GenerationRules);
                return result;
            }

            if (generation.difficultyCounts == null)
            {
                result.Add(FossickValidationSeverity.Error, "Generation config is incomplete.", category: FossickValidationCategory.GenerationRules);
                return result;
            }

            Append(result, Validate(project.ToRuntimeConfig()));
            return result;
        }

        public static FossickValidationResult Validate(FossickMapConfig config)
        {
            var result = new FossickValidationResult();

            if (config == null)
            {
                result.Add(FossickValidationSeverity.Error, "Map config is null.", category: FossickValidationCategory.MapDefinition);
                return result;
            }

            if (config.activity != FossickContentIds.Activity)
            {
                result.Add(FossickValidationSeverity.Error, "Activity must be Fossick.", category: FossickValidationCategory.MapDefinition);
            }

            var boardSpec = config.BoardSpec;
            if (!boardSpec.IsValid)
            {
                result.Add(FossickValidationSeverity.Error, "Board width and visible height must be greater than zero.", category: FossickValidationCategory.MapDefinition);
            }

            if (config.visual == null)
            {
                result.Add(FossickValidationSeverity.Error, "Visual config is missing.", category: FossickValidationCategory.MapDefinition);
            }
            else
            {
                ValidateVisual(config.visual, result);
            }

            ValidateGeneration(config, result);
            ValidateFragments(config, result);

            return result;
        }

        private static void ValidateVisual(FossickVisualConfig visual, FossickValidationResult result)
        {
            if (visual.backgroundIds == null || visual.backgroundIds.Count == 0)
            {
                result.Add(FossickValidationSeverity.Error, "At least one mine background is required.", category: FossickValidationCategory.MapDefinition);
                return;
            }

            var ids = new HashSet<string>();
            for (var i = 0; i < visual.backgroundIds.Count; i++)
            {
                var id = visual.backgroundIds[i];
                if (string.IsNullOrEmpty(id))
                {
                    result.Add(FossickValidationSeverity.Error, "Mine background id cannot be empty.", category: FossickValidationCategory.MapDefinition);
                }
                else if (!ids.Add(id))
                {
                    result.Add(FossickValidationSeverity.Error, $"Mine background {id} is duplicated.", category: FossickValidationCategory.MapDefinition);
                }
            }
        }

        private static void Append(FossickValidationResult target, FossickValidationResult source)
        {
            for (var i = 0; i < source.issues.Count; i++)
            {
                target.issues.Add(source.issues[i]);
            }
        }

        private static void ValidateGeneration(FossickMapConfig config, FossickValidationResult result)
        {
            var generation = config.generation;
            if (generation == null)
            {
                result.Add(FossickValidationSeverity.Error, "Generation config is missing.", category: FossickValidationCategory.GenerationRules);
                return;
            }

            if (generation.regularGroupSize <= 0)
            {
                result.Add(FossickValidationSeverity.Error, "Regular group size must be greater than zero.", category: FossickValidationCategory.GenerationRules);
            }

            if (generation.rewardInsertMin <= 0 || generation.rewardInsertMax <= 0 || generation.rewardInsertMin > generation.rewardInsertMax)
            {
                result.Add(FossickValidationSeverity.Error, "Reward insert range is invalid.", category: FossickValidationCategory.GenerationRules);
            }

            var total = 0;
            if (generation.difficultyCounts == null || generation.difficultyCounts.Count == 0)
            {
                result.Add(FossickValidationSeverity.Error, "At least one difficulty count is required.", category: FossickValidationCategory.GenerationRules);
                return;
            }

            var difficulties = new HashSet<int>();
            for (var i = 0; i < generation.difficultyCounts.Count; i++)
            {
                var entry = generation.difficultyCounts[i];
                if (entry == null)
                {
                    result.Add(FossickValidationSeverity.Error, "Difficulty count entry is null.", category: FossickValidationCategory.GenerationRules);
                    continue;
                }

                if (entry.difficulty <= 0)
                {
                    result.Add(FossickValidationSeverity.Error, "Difficulty must be greater than zero.", category: FossickValidationCategory.GenerationRules);
                }

                if (entry.count <= 0)
                {
                    result.Add(FossickValidationSeverity.Error, "Difficulty count must be greater than zero.", category: FossickValidationCategory.GenerationRules);
                }

                if (!difficulties.Add(entry.difficulty))
                {
                    result.Add(FossickValidationSeverity.Error, $"Difficulty {entry.difficulty} is duplicated in generation config.", category: FossickValidationCategory.GenerationRules);
                }

                total += entry.count;
            }

            if (generation.regularGroupSize > 0 && total != generation.regularGroupSize)
            {
                result.Add(FossickValidationSeverity.Error, $"Difficulty counts total {total}, but regular group size is {generation.regularGroupSize}.", category: FossickValidationCategory.GenerationRules);
            }
        }

        private static void ValidateFragments(FossickMapConfig config, FossickValidationResult result)
        {
            if (config.fragments == null || config.fragments.Count == 0)
            {
                result.Add(FossickValidationSeverity.Error, "At least one fragment is required.", category: FossickValidationCategory.Template);
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

                var reward = cell.reward;
                if (reward != null && reward.type != FossickElementType.None)
                {
                    if (cell.terrain == FossickTerrainType.Unbreakable)
                    {
                        result.Add(FossickValidationSeverity.Error, "Reward is buried under unbreakable terrain.", fragment.id, cell.x, cell.y);
                    }

                    if ((reward.type == FossickElementType.Ore || reward.type == FossickElementType.Item) && !CanAttachBuriedElement(cell))
                    {
                        result.Add(FossickValidationSeverity.Error, "Buried reward must be attached to diggable terrain.", fragment.id, cell.x, cell.y);
                    }

                    if (reward.type == FossickElementType.Collection &&
                        reward.id == FossickContentIds.Reward.CollectionBox &&
                        !CanAttachBuriedElement(cell))
                    {
                        result.Add(FossickValidationSeverity.Error, "Collection box must be buried in diggable terrain.", fragment.id, cell.x, cell.y);
                    }

                    if (reward.type == FossickElementType.Collection &&
                        reward.id != FossickContentIds.Reward.CollectionBox)
                    {
                        result.Add(FossickValidationSeverity.Error, "Collection items cannot be placed directly on the map.", fragment.id, cell.x, cell.y);
                    }

                    if (reward.type == FossickElementType.Coin &&
                        FossickContentIds.Reward.IsCoinDropPlaceholder(reward.id) &&
                        cell.terrain != FossickTerrainType.Empty &&
                        !CanAttachBuriedElement(cell))
                    {
                        result.Add(FossickValidationSeverity.Error, "Coin drops must be placed on an empty cell or buried in diggable terrain.", fragment.id, cell.x, cell.y);
                    }

                }

            }

            ValidateRewardBackground(fragment, result);
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
                    result.Add(FossickValidationSeverity.Error, $"Generation requires difficulty {entry.difficulty}, but no regular fragment uses it.", category: FossickValidationCategory.GenerationRules);
                }
            }
        }

        private static void ValidateRewardBackground(FossickFragmentConfig fragment, FossickValidationResult result)
        {
            if (fragment == null || string.IsNullOrEmpty(fragment.rewardBackgroundId))
            {
                return;
            }

            if (fragment.type != FossickFragmentType.Reward)
            {
                result.Add(FossickValidationSeverity.Warning, "Reward background is usually reserved for reward fragments.", fragment.id);
            }

            if (!FossickRewardBackgroundSpec.TryGetSize(fragment.rewardBackgroundId, out var width, out var height))
            {
                result.Add(FossickValidationSeverity.Error, $"Unknown reward background id {fragment.rewardBackgroundId}.", fragment.id);
                return;
            }

            if (fragment.rewardBackgroundX < 0 ||
                fragment.rewardBackgroundY < 0 ||
                fragment.rewardBackgroundX + width > fragment.width ||
                fragment.rewardBackgroundY + height > fragment.height)
            {
                result.Add(FossickValidationSeverity.Error, "Reward background does not fit inside the fragment.", fragment.id);
            }
        }

        private static bool CanAttachBuriedElement(FossickCellConfig cell)
        {
            return cell != null
                && (cell.terrain == FossickTerrainType.Dirt || cell.terrain == FossickTerrainType.Stone);
        }
    }
}
