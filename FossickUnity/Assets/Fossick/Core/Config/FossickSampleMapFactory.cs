namespace Fossick.Core.Config
{
    public static class FossickSampleMapFactory
    {
        public static FossickMapConfig CreateDefaultConfig()
        {
            var config = new FossickMapConfig();
            config.fragments.Add(CreateFragment(1001, FossickFragmentType.Tutorial, 0, config.boardWidth, config.visibleHeight));
            config.fragments.Add(CreateFragment(2001, FossickFragmentType.Regular, 1, config.boardWidth, config.visibleHeight));
            config.fragments.Add(CreateFragment(2002, FossickFragmentType.Regular, 2, config.boardWidth, config.visibleHeight));
            config.fragments.Add(CreateFragment(2003, FossickFragmentType.Regular, 3, config.boardWidth, config.visibleHeight));
            config.fragments.Add(CreateFragment(3001, FossickFragmentType.Reward, 0, config.boardWidth, 3));
            return config;
        }

        private static FossickFragmentConfig CreateFragment(int id, FossickFragmentType type, int difficulty, int width, int height)
        {
            var fragment = new FossickFragmentConfig
            {
                id = id,
                type = type,
                difficulty = difficulty,
                width = width,
                height = height
            };

            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width; x++)
                {
                    var terrain = GetSampleTerrain(type, difficulty, x, y, width, height);
                    fragment.cells.Add(new FossickCellConfig
                    {
                        x = x,
                        y = y,
                        backgroundId = "mine_default",
                        rewardBackgroundId = type == FossickFragmentType.Reward && y < 2 ? "treasure_room" : null,
                        terrain = terrain,
                        hp = GetHp(terrain),
                        reward = GetSampleElement(type, x, y, width, height),
                        decorations = new System.Collections.Generic.List<string>(),
                        fog = terrain == FossickTerrainType.Empty ? FossickFogType.None : FossickFogType.Covered,
                        mask = terrain != FossickTerrainType.Empty
                    });
                }
            }

            return fragment;
        }

        private static FossickTerrainType GetSampleTerrain(FossickFragmentType type, int difficulty, int x, int y, int width, int height)
        {
            if (type == FossickFragmentType.Reward)
            {
                return FossickTerrainType.Empty;
            }

            if (type == FossickFragmentType.Tutorial && y == 0)
            {
                return FossickTerrainType.Empty;
            }

            if (type == FossickFragmentType.Tutorial && y == height - 1 && x == width / 2)
            {
                return FossickTerrainType.Empty;
            }

            if (type == FossickFragmentType.Regular && y == height - 1 && x == width / 2)
            {
                return FossickTerrainType.Empty;
            }

            if (x == 0 || x == width - 1)
            {
                return y % 3 == 0 ? FossickTerrainType.Stone : FossickTerrainType.Dirt;
            }

            if (difficulty >= 2 && (x + y) % 5 == 0)
            {
                return FossickTerrainType.Stone;
            }

            return FossickTerrainType.Dirt;
        }

        private static int GetHp(FossickTerrainType terrain)
        {
            if (terrain == FossickTerrainType.Stone)
            {
                return 2;
            }

            if (terrain == FossickTerrainType.Dirt)
            {
                return 1;
            }

            return 0;
        }

        private static FossickElementConfig GetSampleElement(FossickFragmentType type, int x, int y, int width, int height)
        {
            if (type == FossickFragmentType.Reward && y == height / 2 && x > 1 && x < width - 2)
            {
                return new FossickElementConfig
                {
                    type = FossickElementType.Coin,
                    id = "coin_pile",
                    amount = 25
                };
            }

            if (type != FossickFragmentType.Reward && x == width / 2 && y == height / 2)
            {
                return new FossickElementConfig
                {
                    type = FossickElementType.Ore,
                    id = "copper",
                    amount = 10
                };
            }

            return null;
        }
    }
}
