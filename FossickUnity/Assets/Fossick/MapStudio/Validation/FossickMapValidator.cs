using System.Collections.Generic;
using Fossick.Core.Definition.Config;

namespace Fossick.MapStudio.Validation
{
    public static class FossickMapValidator
    {
        public static FossickValidationResult Validate(FossickMapConfig config)
        {
            var result = new FossickValidationResult();

            if (config == null)
            {
                result.Add(FossickValidationSeverity.Error, "Map config is null.", category: FossickValidationCategory.MapDefinition);
                return result;
            }

            if (config.activity != "Fossick")
            {
                result.Add(FossickValidationSeverity.Error, "Activity must be Fossick.", category: FossickValidationCategory.MapDefinition);
            }

            var boardSpec = config.BoardSpec;
            if (!boardSpec.IsValid)
            {
                result.Add(FossickValidationSeverity.Error, "Board width and visible height must be greater than zero.", category: FossickValidationCategory.MapDefinition);
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

            ValidateSmallCoinDrop(generation.smallCoinDrop, result);
        }

        private static void ValidateSmallCoinDrop(FossickSmallCoinDropConfig smallCoinDrop, FossickValidationResult result)
        {
            if (smallCoinDrop == null || !smallCoinDrop.enabled)
            {
                return;
            }

            if (smallCoinDrop.chancePerMille <= 0 || smallCoinDrop.chancePerMille > 1000)
            {
                result.Add(FossickValidationSeverity.Error, "Small coin drop chance must be between 1 and 1000 per mille.", category: FossickValidationCategory.GenerationRules);
            }

            if (smallCoinDrop.amounts == null || smallCoinDrop.amounts.Count == 0)
            {
                result.Add(FossickValidationSeverity.Error, "Small coin drop needs at least one amount entry.", category: FossickValidationCategory.GenerationRules);
                return;
            }

            var hasValidAmount = false;
            for (var i = 0; i < smallCoinDrop.amounts.Count; i++)
            {
                var entry = smallCoinDrop.amounts[i];
                if (entry == null)
                {
                    result.Add(FossickValidationSeverity.Error, "Small coin drop amount entry is null.", category: FossickValidationCategory.GenerationRules);
                    continue;
                }

                if (entry.amount <= 0)
                {
                    result.Add(FossickValidationSeverity.Error, "Small coin drop amount must be greater than zero.", category: FossickValidationCategory.GenerationRules);
                }

                if (entry.weight <= 0)
                {
                    result.Add(FossickValidationSeverity.Error, "Small coin drop amount weight must be greater than zero.", category: FossickValidationCategory.GenerationRules);
                }

                hasValidAmount |= entry.amount > 0 && entry.weight > 0;
            }

            if (!hasValidAmount)
            {
                result.Add(FossickValidationSeverity.Error, "Small coin drop must contain at least one valid weighted amount.", category: FossickValidationCategory.GenerationRules);
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

                if (cell.hp < 0)
                {
                    result.Add(FossickValidationSeverity.Error, "Cell hp cannot be negative.", fragment.id, cell.x, cell.y);
                }

                if (cell.terrain != FossickTerrainType.Empty && cell.hp == 0 && cell.terrain != FossickTerrainType.Unbreakable)
                {
                    result.Add(FossickValidationSeverity.Warning, "Breakable terrain should have hp greater than zero.", fragment.id, cell.x, cell.y);
                }

                var reward = cell.reward;
                if (reward != null && reward.type != FossickElementType.None)
                {
                    if (reward.amount < 0)
                    {
                        result.Add(FossickValidationSeverity.Error, "Reward amount cannot be negative.", fragment.id, cell.x, cell.y);
                    }

                    if (cell.terrain == FossickTerrainType.Unbreakable)
                    {
                        result.Add(FossickValidationSeverity.Error, "Reward is buried under unbreakable terrain.", fragment.id, cell.x, cell.y);
                    }

                    if ((reward.type == FossickElementType.Ore || reward.type == FossickElementType.Item) && !CanAttachBuriedElement(cell))
                    {
                        result.Add(FossickValidationSeverity.Error, "Buried reward must be attached to diggable terrain.", fragment.id, cell.x, cell.y);
                    }

                }

                if (!string.IsNullOrEmpty(cell.rewardBackgroundId) && fragment.type != FossickFragmentType.Reward)
                {
                    result.Add(FossickValidationSeverity.Warning, "Reward background is usually reserved for reward fragments.", fragment.id, cell.x, cell.y);
                }
            }

            ValidateRewardBackgroundRegions(config, fragment, result);
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

        private static void ValidateRewardBackgroundRegions(FossickMapConfig config, FossickFragmentConfig fragment, FossickValidationResult result)
        {
            if (config == null || fragment == null || fragment.cells == null || config.boardWidth <= 0 || fragment.height <= 0)
            {
                return;
            }

            var ids = new string[fragment.height, config.boardWidth];
            for (var i = 0; i < fragment.cells.Count; i++)
            {
                var cell = fragment.cells[i];
                if (cell == null
                    || cell.x < 0
                    || cell.x >= config.boardWidth
                    || cell.y < 0
                    || cell.y >= fragment.height
                    || string.IsNullOrEmpty(cell.rewardBackgroundId))
                {
                    continue;
                }

                ids[cell.y, cell.x] = cell.rewardBackgroundId;
            }

            var visited = new bool[fragment.height, config.boardWidth];
            for (var y = 0; y < fragment.height; y++)
            {
                for (var x = 0; x < config.boardWidth; x++)
                {
                    if (visited[y, x] || string.IsNullOrEmpty(ids[y, x]))
                    {
                        continue;
                    }

                    ValidateRewardBackgroundRegion(fragment, result, ids, visited, x, y, config.boardWidth, fragment.height);
                }
            }
        }

        private static void ValidateRewardBackgroundRegion(
            FossickFragmentConfig fragment,
            FossickValidationResult result,
            string[,] ids,
            bool[,] visited,
            int startX,
            int startY,
            int width,
            int height)
        {
            var id = ids[startY, startX];
            var queue = new Queue<int>();
            queue.Enqueue(startY * width + startX);
            visited[startY, startX] = true;

            var minX = startX;
            var maxX = startX;
            var minY = startY;
            var maxY = startY;
            var count = 0;

            while (queue.Count > 0)
            {
                var key = queue.Dequeue();
                var x = key % width;
                var y = key / width;
                count++;

                if (x < minX)
                {
                    minX = x;
                }

                if (x > maxX)
                {
                    maxX = x;
                }

                if (y < minY)
                {
                    minY = y;
                }

                if (y > maxY)
                {
                    maxY = y;
                }

                EnqueueRewardBackgroundNeighbor(ids, visited, queue, id, x - 1, y, width, height);
                EnqueueRewardBackgroundNeighbor(ids, visited, queue, id, x + 1, y, width, height);
                EnqueueRewardBackgroundNeighbor(ids, visited, queue, id, x, y - 1, width, height);
                EnqueueRewardBackgroundNeighbor(ids, visited, queue, id, x, y + 1, width, height);
            }

            if (!TryGetRewardBackgroundSize(id, out var expectedWidth, out var expectedHeight))
            {
                result.Add(FossickValidationSeverity.Warning, $"Unknown reward background id {id}.", fragment.id, startX, startY);
                return;
            }

            var actualWidth = maxX - minX + 1;
            var actualHeight = maxY - minY + 1;
            if (actualWidth != expectedWidth || actualHeight != expectedHeight || count != expectedWidth * expectedHeight)
            {
                result.Add(FossickValidationSeverity.Error, "Reward background region shape is invalid.", fragment.id, startX, startY);
            }
        }

        private static void EnqueueRewardBackgroundNeighbor(
            string[,] ids,
            bool[,] visited,
            Queue<int> queue,
            string id,
            int x,
            int y,
            int width,
            int height)
        {
            if (x < 0 || x >= width || y < 0 || y >= height || visited[y, x] || ids[y, x] != id)
            {
                return;
            }

            visited[y, x] = true;
            queue.Enqueue(y * width + x);
        }

        private static bool TryGetRewardBackgroundSize(string id, out int width, out int height)
        {
            if (id == "treasure_room_3x2")
            {
                width = 3;
                height = 2;
                return true;
            }

            if (id == "treasure_room_5x2")
            {
                width = 5;
                height = 2;
                return true;
            }

            if (id == "treasure_room" || id == "treasure_room_7x2")
            {
                width = 7;
                height = 2;
                return true;
            }

            width = 0;
            height = 0;
            return false;
        }

        private static bool CanAttachBuriedElement(FossickCellConfig cell)
        {
            return cell != null
                && (cell.terrain == FossickTerrainType.Dirt || cell.terrain == FossickTerrainType.Stone)
                && cell.hp > 0;
        }
    }
}
