namespace Fossick.Core.Definition.Config
{
    public interface IFossickRewardProvider
    {
        int GetValue(FossickElementType elementType, string id);
        string PickCoinDropId();
        bool TryPickTerrainCoinDropId(out string id);
    }
}
