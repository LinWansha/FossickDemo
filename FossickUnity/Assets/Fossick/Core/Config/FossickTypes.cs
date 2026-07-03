namespace Fossick.Core.Config
{
    public enum FossickFragmentType
    {
        Tutorial = 0,
        Regular = 1,
        Reward = 2
    }

    public enum FossickTerrainType
    {
        Empty = 0,
        Dirt = 1,
        Stone = 2,
        Unbreakable = 3
    }

    public enum FossickElementType
    {
        None = 0,
        Ore = 1,
        Coin = 2,
        Score = 3,
        Collection = 4,
        Item = 5,
        Chest = 6
    }

    public enum FossickToolType
    {
        Pickaxe = 0,
        Dynamite = 1,
        Tnt = 2,
        Radar = 3
    }

    public enum FossickVisualLayer
    {
        Background = 0,
        RewardBackground = 1,
        Terrain = 2,
        TerrainAttachment = 3,
        Reward = 4,
        Decoration = 5,
        Fog = 6
    }

    public enum FossickBrushMode
    {
        RewardBackground = 0,
        Terrain = 1,
        Reward = 2,
        Tool = 3,
        Decoration = 4,
        Fog = 5
    }

    public enum FossickFogType
    {
        None = 0,
        Covered = 1
    }
}
