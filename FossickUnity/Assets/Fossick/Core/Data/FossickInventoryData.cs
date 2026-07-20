using Fossick.Core.Definition.Config;

namespace Fossick.Core.Data
{
    public sealed class FossickInventoryData
    {
        public int pickaxes;
        public int dynamite;
        public int tnt;
        public int radar;

        public bool HasTool(FossickToolType toolType)
        {
            return GetToolCount(toolType) > 0;
        }

        public bool ConsumeTool(FossickToolType toolType)
        {
            if (!HasTool(toolType))
            {
                return false;
            }

            switch (toolType)
            {
                case FossickToolType.Dynamite:
                    dynamite--;
                    break;
                case FossickToolType.Tnt:
                    tnt--;
                    break;
                case FossickToolType.Radar:
                    radar--;
                    break;
                default:
                    pickaxes--;
                    break;
            }

            return true;
        }

        public void AddTool(FossickToolType toolType, int amount)
        {
            if (amount <= 0)
            {
                amount = 1;
            }

            switch (toolType)
            {
                case FossickToolType.Dynamite:
                    dynamite += amount;
                    break;
                case FossickToolType.Tnt:
                    tnt += amount;
                    break;
                case FossickToolType.Radar:
                    radar += amount;
                    break;
                default:
                    pickaxes += amount;
                    break;
            }
        }

        public int GetToolCount(FossickToolType toolType)
        {
            switch (toolType)
            {
                case FossickToolType.Dynamite:
                    return dynamite;
                case FossickToolType.Tnt:
                    return tnt;
                case FossickToolType.Radar:
                    return radar;
                default:
                    return pickaxes;
            }
        }
    }
}
