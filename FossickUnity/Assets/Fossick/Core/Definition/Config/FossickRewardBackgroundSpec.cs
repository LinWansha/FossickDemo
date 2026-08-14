namespace Fossick.Core.Definition.Config
{
    public static class FossickRewardBackgroundSpec
    {
        public static bool TryGetSize(string id, out int width, out int height)
        {
            height = 2;
            switch (id)
            {
                case FossickContentIds.RewardBackground.TreasureRoomSmall:
                    width = 3;
                    return true;
                case FossickContentIds.RewardBackground.TreasureRoomMedium:
                    width = 5;
                    return true;
                case FossickContentIds.RewardBackground.TreasureRoomLarge:
                    width = 7;
                    return true;
                default:
                    width = 0;
                    height = 0;
                    return false;
            }
        }
    }
}
