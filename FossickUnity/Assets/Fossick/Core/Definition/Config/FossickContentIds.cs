namespace Fossick.Core.Definition.Config
{
    public static class FossickContentIds
    {
        public const string Activity = "Fossick";

        public static class MapProject
        {
            public const string DefaultMap = "fossick_default_map";
            public const string DefaultFragmentLibrary = "fossick_default_fragments";
            public const string DefaultGenerationRules = "fossick_default_rules";
        }

        public static class Background
        {
            public const string MineDefault = "mine_default";
            public const string MineMap = "mine_map";
            public const string MineVariant = "mine_variant";
        }

        public static class RewardBackground
        {
            public const string TreasureRoomSmall = "treasure_room_3x2";
            public const string TreasureRoomMedium = "treasure_room_5x2";
            public const string TreasureRoomLarge = "treasure_room_7x2";
        }

        public static class Reward
        {
            public const string CoinDrop = "coin_drop";
            public const string CoinDropSmall = "coin_drop_small";
            public const string CoinDropMedium = "coin_drop_medium";
            public const string CoinDropLarge = "coin_drop_large";
            public const string CoinPileSmall = "coin_pile_small";
            public const string CoinPileLarge = "coin_pile_large";
            public const string OreCopper = "ore_copper";
            public const string OreSilver = "ore_silver";
            public const string OreGold = "ore_gold";
            public const string OreGem = "ore_gem";
            public const string CollectionBox = "collection_box";
            public const string TreasureChest = "treasure_chest";
            public const string MessageBottle = "message_bottle";
            public const string LockedChest = "locked_chest";
            public const string LockedChestLegacy = "lockedChest";
            public const string CollectionPiece = "collection_piece";
            public const string DefaultCollection = "default";

            public static bool IsCoinDropPlaceholder(string id)
            {
                return id == CoinDrop;
            }
        }

        public static class Tool
        {
            public const string Pickaxe = "pickaxe";
            public const string Dynamite = "dynamite";
            public const string Tnt = "tnt";
            public const string Radar = "radar";

            public static string GetId(FossickToolType toolType)
            {
                switch (toolType)
                {
                    case FossickToolType.Pickaxe:
                        return Pickaxe;
                    case FossickToolType.Dynamite:
                        return Dynamite;
                    case FossickToolType.Tnt:
                        return Tnt;
                    case FossickToolType.Radar:
                        return Radar;
                    default:
                        return string.Empty;
                }
            }

            public static bool TryGetType(string id, out FossickToolType toolType)
            {
                switch (id)
                {
                    case Pickaxe:
                        toolType = FossickToolType.Pickaxe;
                        return true;
                    case Dynamite:
                        toolType = FossickToolType.Dynamite;
                        return true;
                    case Tnt:
                        toolType = FossickToolType.Tnt;
                        return true;
                    case Radar:
                        toolType = FossickToolType.Radar;
                        return true;
                    default:
                        toolType = default;
                        return false;
                }
            }
        }

        public static class Decoration
        {
            public const string GrassLarge = "grass_large";
            public const string GrassSmall = "grass_small";
            public const string Mushroom = "mushroom";
            public const string SmallRock = "small_rock";
            public const string GoldPile = "gold_pile";
        }
    }
}
