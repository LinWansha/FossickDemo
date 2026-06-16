using System;
using System.Collections.Generic;

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
        public List<FossickSequenceOverrideConfig> sequenceOverrides = new List<FossickSequenceOverrideConfig>();
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
