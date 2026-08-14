using System;
using System.Collections.Generic;
using System.Linq;

namespace Fossick.Core.Definition.Config
{
    [Serializable]
    public sealed class FossickMapConfig
    {
        public int version = 1;
        public string activity = FossickContentIds.Activity;
        public int boardWidth = FossickBoardSpec.DefaultWidth;
        public int visibleHeight = FossickBoardSpec.DefaultVisibleHeight;
        public FossickGenerationConfig generation = new FossickGenerationConfig();
        public FossickVisualConfig visual = new FossickVisualConfig();
        public List<FossickFragmentConfig> fragments = new List<FossickFragmentConfig>();

        public FossickBoardSpec BoardSpec => new FossickBoardSpec(boardWidth, visibleHeight);
    }

    [Serializable]
    public sealed class FossickFragmentLibraryConfig
    {
        public int version = 1;
        public string activity = FossickContentIds.Activity;
        public string libraryId = FossickContentIds.MapProject.DefaultFragmentLibrary;
        public int boardWidth = FossickBoardSpec.DefaultWidth;
        public List<FossickFragmentConfig> fragments = new List<FossickFragmentConfig>();
    }

    [Serializable]
    public sealed class FossickGenerationRulesConfig
    {
        public int version = 1;
        public string activity = FossickContentIds.Activity;
        public string rulesId = FossickContentIds.MapProject.DefaultGenerationRules;
        public int boardWidth = FossickBoardSpec.DefaultWidth;
        public int visibleHeight = FossickBoardSpec.DefaultVisibleHeight;
        public FossickGenerationConfig generation = new FossickGenerationConfig();
        public FossickVisualConfig visual = new FossickVisualConfig();
    }

    [Serializable]
    public sealed class FossickMapDefinitionConfig
    {
        public int version = 1;
        public string activity = FossickContentIds.Activity;
        public string mapId = FossickContentIds.MapProject.DefaultMap;
        public string fragmentLibraryId = FossickContentIds.MapProject.DefaultFragmentLibrary;
        public string generationRulesId = FossickContentIds.MapProject.DefaultGenerationRules;
    }

    [Serializable]
    public sealed class FossickMapProjectConfig
    {
        public int version = 1;
        public string activity = FossickContentIds.Activity;
        public FossickFragmentLibraryConfig fragmentLibrary = new FossickFragmentLibraryConfig();
        public FossickGenerationRulesConfig generationRules = new FossickGenerationRulesConfig();
        public FossickMapDefinitionConfig mapDefinition = new FossickMapDefinitionConfig();

        public static FossickMapProjectConfig FromRuntimeConfig(FossickMapConfig config)
        {
            var project = new FossickMapProjectConfig();
            project.version = config.version;
            project.activity = config.activity;

            project.fragmentLibrary = new FossickFragmentLibraryConfig
            {
                version = config.version,
                activity = project.activity,
                boardWidth = config.boardWidth,
                fragments = config.fragments
            };

            project.generationRules = new FossickGenerationRulesConfig
            {
                version = config.version,
                activity = project.activity,
                boardWidth = config.boardWidth,
                visibleHeight = config.visibleHeight,
                generation = CloneGenerationWithoutMapOverrides(config.generation),
                visual = config.visual
            };

            project.mapDefinition = new FossickMapDefinitionConfig
            {
                version = config.version,
                activity = project.activity,
                fragmentLibraryId = project.fragmentLibrary.libraryId,
                generationRulesId = project.generationRules.rulesId
            };

            return project;
        }

        public FossickMapConfig ToRuntimeConfig()
        {
            var config = new FossickMapConfig();
            config.version = version;
            config.activity = activity;
            config.boardWidth = fragmentLibrary.boardWidth;
            config.fragments = fragmentLibrary.fragments;
            config.boardWidth = generationRules.boardWidth;
            config.visibleHeight = generationRules.visibleHeight;
            config.generation = CloneGenerationWithoutMapOverrides(generationRules.generation);
            config.visual = generationRules.visual;

            config.generation.sequenceOverrides = new List<FossickSequenceOverrideConfig>();
            config.generation.rowOverrides = new List<FossickRowOverrideConfig>();

            return config;
        }

        private static FossickGenerationConfig CloneGenerationWithoutMapOverrides(FossickGenerationConfig source)
        {
            var clone = new FossickGenerationConfig
            {
                regularGroupSize = source.regularGroupSize,
                rewardInsertMin = source.rewardInsertMin,
                rewardInsertMax = source.rewardInsertMax,
                difficultyCounts = source.difficultyCounts
                    .Select(count => count == null
                        ? null
                        : new FossickDifficultyCount { difficulty = count.difficulty, count = count.count })
                    .ToList(),
                sequenceOverrides = new List<FossickSequenceOverrideConfig>(),
                rowOverrides = new List<FossickRowOverrideConfig>()
            };

            return clone;
        }

    }

    [Serializable]
    public sealed class FossickGenerationConfig
    {
        public int regularGroupSize = 10;
        public List<FossickDifficultyCount> difficultyCounts = new List<FossickDifficultyCount>
        {
            new FossickDifficultyCount { difficulty = 1, count = 7 },
            new FossickDifficultyCount { difficulty = 2, count = 2 },
            new FossickDifficultyCount { difficulty = 3, count = 1 }
        };
        public int rewardInsertMin = 4;
        public int rewardInsertMax = 6;
        [NonSerialized]
        public List<FossickSequenceOverrideConfig> sequenceOverrides = new List<FossickSequenceOverrideConfig>();
        [NonSerialized]
        public List<FossickRowOverrideConfig> rowOverrides = new List<FossickRowOverrideConfig>();
    }

    [Serializable]
    public sealed class FossickDifficultyCount
    {
        public int difficulty;
        public int count;
    }

    [Serializable]
    public sealed class FossickSequenceOverrideConfig
    {
        public int sequenceIndex;
        public FossickFragmentConfig fragment;
    }

    [Serializable]
    public sealed class FossickRowOverrideConfig
    {
        public int startRow;
        public FossickFragmentConfig fragment;
    }

    [Serializable]
    public sealed class FossickVisualConfig
    {
        public string dirtAutoTileSet = "Dirt";
        public List<string> backgroundIds = new List<string>
        {
            FossickContentIds.Background.MineDefault,
            FossickContentIds.Background.MineMap,
            FossickContentIds.Background.MineVariant
        };
    }

    [Serializable]
    public sealed class FossickFragmentConfig
    {
        public int id;
        public FossickFragmentType type = FossickFragmentType.Regular;
        public string rewardBackgroundId;
        public int rewardBackgroundX;
        public int rewardBackgroundY;
        public int difficulty;
        public int weight = 1;
        public List<string> tags = new List<string>();
        public int width = FossickBoardSpec.DefaultWidth;
        public int height = FossickBoardSpec.DefaultVisibleHeight;
        public List<FossickCellConfig> cells = new List<FossickCellConfig>();
    }

    [Serializable]
    public sealed class FossickCellConfig
    {
        public int x;
        public int y;
        public FossickTerrainType terrain = FossickTerrainType.Empty;
        public FossickElementConfig reward;
        public List<string> decorations = new List<string>();
        public FossickFogType fog = FossickFogType.Covered;
    }

    [Serializable]
    public sealed class FossickElementConfig
    {
        public FossickElementType type = FossickElementType.None;
        public string id;
    }
}
