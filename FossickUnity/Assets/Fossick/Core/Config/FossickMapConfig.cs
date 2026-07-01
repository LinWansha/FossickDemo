using System;
using System.Collections.Generic;
using System.Linq;

namespace Fossick.Core.Config
{
    [Serializable]
    public sealed class FossickMapConfig
    {
        public int version = 1;
        public string activity = "Fossick";
        public int boardWidth = FossickBoardSpec.DefaultWidth;
        public int visibleHeight = FossickBoardSpec.DefaultVisibleHeight;
        public FossickGenerationConfig generation = new FossickGenerationConfig();
        public FossickGameplayConfig gameplay = new FossickGameplayConfig();
        public FossickToolRulesConfig tools = new FossickToolRulesConfig();
        public FossickVisualConfig visual = new FossickVisualConfig();
        public List<FossickFragmentConfig> fragments = new List<FossickFragmentConfig>();

        public FossickBoardSpec BoardSpec => new FossickBoardSpec(boardWidth, visibleHeight);
    }

    [Serializable]
    public sealed class FossickFragmentLibraryConfig
    {
        public int version = 1;
        public string activity = "Fossick";
        public string libraryId = "fossick_default_fragments";
        public int boardWidth = FossickBoardSpec.DefaultWidth;
        public List<FossickFragmentConfig> fragments = new List<FossickFragmentConfig>();
    }

    [Serializable]
    public sealed class FossickGenerationRulesConfig
    {
        public int version = 1;
        public string activity = "Fossick";
        public string rulesId = "fossick_default_rules";
        public int boardWidth = FossickBoardSpec.DefaultWidth;
        public int visibleHeight = FossickBoardSpec.DefaultVisibleHeight;
        public FossickGenerationConfig generation = new FossickGenerationConfig();
        public FossickGameplayConfig gameplay = new FossickGameplayConfig();
        public FossickToolRulesConfig tools = new FossickToolRulesConfig();
        public FossickVisualConfig visual = new FossickVisualConfig();
    }

    [Serializable]
    public sealed class FossickMapDefinitionConfig
    {
        public int version = 1;
        public string activity = "Fossick";
        public string mapId = "fossick_default_map";
        public string fragmentLibraryId = "fossick_default_fragments";
        public string generationRulesId = "fossick_default_rules";
        public int seed = 12345;
    }

    [Serializable]
    public sealed class FossickMapProjectConfig
    {
        public int version = 1;
        public string activity = "Fossick";
        public FossickFragmentLibraryConfig fragmentLibrary = new FossickFragmentLibraryConfig();
        public FossickGenerationRulesConfig generationRules = new FossickGenerationRulesConfig();
        public FossickMapDefinitionConfig mapDefinition = new FossickMapDefinitionConfig();

        public static FossickMapProjectConfig FromRuntimeConfig(FossickMapConfig config, int seed)
        {
            var project = new FossickMapProjectConfig();
            if (config == null)
            {
                return project;
            }

            project.version = config.version;
            project.activity = string.IsNullOrEmpty(config.activity) ? "Fossick" : config.activity;

            project.fragmentLibrary = new FossickFragmentLibraryConfig
            {
                version = config.version,
                activity = project.activity,
                boardWidth = config.boardWidth,
                fragments = config.fragments ?? new List<FossickFragmentConfig>()
            };

            var generation = config.generation ?? new FossickGenerationConfig();
            project.generationRules = new FossickGenerationRulesConfig
            {
                version = config.version,
                activity = project.activity,
                boardWidth = config.boardWidth,
                visibleHeight = config.visibleHeight,
                generation = CloneGenerationWithoutMapOverrides(generation),
                gameplay = config.gameplay ?? new FossickGameplayConfig(),
                tools = config.tools ?? new FossickToolRulesConfig(),
                visual = config.visual ?? new FossickVisualConfig()
            };

            project.mapDefinition = new FossickMapDefinitionConfig
            {
                version = config.version,
                activity = project.activity,
                seed = seed,
                fragmentLibraryId = project.fragmentLibrary.libraryId,
                generationRulesId = project.generationRules.rulesId
            };

            return project;
        }

        public FossickMapConfig ToRuntimeConfig()
        {
            var config = new FossickMapConfig();
            config.version = version;
            config.activity = string.IsNullOrEmpty(activity) ? "Fossick" : activity;

            if (fragmentLibrary != null)
            {
                config.boardWidth = fragmentLibrary.boardWidth;
                config.fragments = fragmentLibrary.fragments ?? new List<FossickFragmentConfig>();
            }

            if (generationRules != null)
            {
                config.boardWidth = generationRules.boardWidth;
                config.visibleHeight = generationRules.visibleHeight;
                config.generation = CloneGenerationWithoutMapOverrides(generationRules.generation ?? new FossickGenerationConfig());
                config.gameplay = generationRules.gameplay ?? new FossickGameplayConfig();
                config.tools = generationRules.tools ?? new FossickToolRulesConfig();
                config.visual = generationRules.visual ?? new FossickVisualConfig();
            }

            if (config.generation == null)
            {
                config.generation = new FossickGenerationConfig();
            }

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
                prefetchVisibleScreens = source.prefetchVisibleScreens,
                minimumRowsAhead = source.minimumRowsAhead,
                retainRowsBehind = source.retainRowsBehind,
                difficultyCounts = source.difficultyCounts == null
                    ? new List<FossickDifficultyCount>()
                    : source.difficultyCounts
                        .Select(count => new FossickDifficultyCount { difficulty = count.difficulty, count = count.count })
                        .ToList(),
                sequenceOverrides = new List<FossickSequenceOverrideConfig>(),
                rowOverrides = new List<FossickRowOverrideConfig>()
            };

            return clone;
        }
    }

    [Serializable]
    public sealed class FossickToolRulesConfig
    {
        public FossickToolShapeConfig dynamite = FossickToolShapeConfig.CreateCross();
        public FossickToolShapeConfig tnt = FossickToolShapeConfig.CreateSquare(1);
        public FossickToolShapeConfig radar = FossickToolShapeConfig.CreateDiamond(2);
    }

    [Serializable]
    public sealed class FossickToolShapeConfig
    {
        public List<FossickToolOffset> offsets = new List<FossickToolOffset>();

        public static FossickToolShapeConfig CreateCross()
        {
            var shape = new FossickToolShapeConfig();
            shape.offsets.Add(new FossickToolOffset { x = 0, y = 0 });
            shape.offsets.Add(new FossickToolOffset { x = 1, y = 0 });
            shape.offsets.Add(new FossickToolOffset { x = -1, y = 0 });
            shape.offsets.Add(new FossickToolOffset { x = 0, y = 1 });
            shape.offsets.Add(new FossickToolOffset { x = 0, y = -1 });
            return shape;
        }

        public static FossickToolShapeConfig CreateSquare(int radius)
        {
            var shape = new FossickToolShapeConfig();
            for (var y = -radius; y <= radius; y++)
            {
                for (var x = -radius; x <= radius; x++)
                {
                    shape.offsets.Add(new FossickToolOffset { x = x, y = y });
                }
            }

            return shape;
        }

        public static FossickToolShapeConfig CreateDiamond(int radius)
        {
            var shape = new FossickToolShapeConfig();
            for (var y = -radius; y <= radius; y++)
            {
                for (var x = -radius; x <= radius; x++)
                {
                    if (Math.Abs(x) + Math.Abs(y) <= radius)
                    {
                        shape.offsets.Add(new FossickToolOffset { x = x, y = y });
                    }
                }
            }

            return shape;
        }
    }

    [Serializable]
    public sealed class FossickToolOffset
    {
        public int x;
        public int y;
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
        public int prefetchVisibleScreens = 4;
        public int minimumRowsAhead = 24;
        public int retainRowsBehind = 12;
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
    }

    [Serializable]
    public sealed class FossickGameplayConfig
    {
        public int startingPickaxes = 20;
        public int startingDynamite = 3;
        public int startingTnt = 1;
        public int startingRadar = 1;
    }

    [Serializable]
    public sealed class FossickFragmentConfig
    {
        public int id;
        public FossickFragmentType type = FossickFragmentType.Regular;
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
        public string backgroundId;
        public string rewardBackgroundId;
        public FossickTerrainType terrain = FossickTerrainType.Empty;
        public int hp;
        public FossickElementConfig reward;
        public List<string> decorations = new List<string>();
        public FossickFogType fog = FossickFogType.Covered;

        // Legacy fields kept so existing draft JSON keeps loading while the editor migrates to explicit layers.
        public FossickElementConfig element;
        public List<string> decor = new List<string>();
        public bool mask;
    }

    [Serializable]
    public sealed class FossickElementConfig
    {
        public FossickElementType type = FossickElementType.None;
        public string id;
        public int amount;
    }
}
