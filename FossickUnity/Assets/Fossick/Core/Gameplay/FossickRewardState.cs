using System.Collections.Generic;
using Fossick.Core.Actions;
using Fossick.Core.Config;

namespace Fossick.Core.Gameplay
{
    public sealed class FossickRewardState
    {
        private readonly Dictionary<string, int> collections = new Dictionary<string, int>();

        public int score;
        public int coins;
        public IReadOnlyDictionary<string, int> Collections => collections;

        public void Apply(FossickRewardEvent reward, FossickInventoryState inventory)
        {
            if (reward == null)
            {
                return;
            }

            var amount = reward.amount <= 0 ? 1 : reward.amount;
            switch (reward.elementType)
            {
                case FossickElementType.Coin:
                    coins += amount;
                    break;
                case FossickElementType.Ore:
                    score += amount;
                    break;
                case FossickElementType.Collection:
                    AddCollection(reward.id, amount);
                    break;
                case FossickElementType.Item:
                    ApplyItemReward(reward.id, amount, inventory);
                    break;
            }
        }

        public void AddCollection(string id, int amount)
        {
            if (string.IsNullOrEmpty(id))
            {
                id = "default";
            }

            if (amount <= 0)
            {
                amount = 1;
            }

            if (!collections.ContainsKey(id))
            {
                collections.Add(id, 0);
            }

            collections[id] += amount;
        }

        public int GetCompleteCollectionSetCount(int setSize)
        {
            if (setSize <= 0 || collections.Count < setSize)
            {
                return 0;
            }

            var min = int.MaxValue;
            var seen = 0;
            foreach (var item in collections)
            {
                if (item.Value <= 0)
                {
                    continue;
                }

                seen++;
                if (item.Value < min)
                {
                    min = item.Value;
                }
            }

            return seen < setSize || min == int.MaxValue ? 0 : min;
        }

        public bool ConsumeCollectionSet(int setSize)
        {
            if (GetCompleteCollectionSetCount(setSize) <= 0)
            {
                return false;
            }

            var consumed = 0;
            var keys = new List<string>(collections.Keys);
            keys.Sort();
            for (var i = 0; i < keys.Count && consumed < setSize; i++)
            {
                var key = keys[i];
                if (collections[key] <= 0)
                {
                    continue;
                }

                collections[key]--;
                consumed++;
            }

            return consumed == setSize;
        }

        public List<string> CreateCollectionSaveList()
        {
            var result = new List<string>();
            foreach (var item in collections)
            {
                result.Add(item.Key + ":" + item.Value);
            }

            return result;
        }

        public void LoadCollectionSaveList(List<string> values)
        {
            collections.Clear();
            if (values == null)
            {
                return;
            }

            for (var i = 0; i < values.Count; i++)
            {
                if (TryParseCollection(values[i], out var id, out var amount))
                {
                    collections[id] = amount;
                }
            }
        }

        private static void ApplyItemReward(string id, int amount, FossickInventoryState inventory)
        {
            if (inventory == null)
            {
                return;
            }

            if (id == "tnt")
            {
                inventory.AddTool(FossickToolType.Tnt, amount);
            }
            else if (id == "radar")
            {
                inventory.AddTool(FossickToolType.Radar, amount);
            }
            else if (id == "dynamite")
            {
                inventory.AddTool(FossickToolType.Dynamite, amount);
            }
            else
            {
                inventory.AddTool(FossickToolType.Pickaxe, amount);
            }
        }

        private static bool TryParseCollection(string value, out string id, out int amount)
        {
            id = null;
            amount = 0;
            if (string.IsNullOrEmpty(value))
            {
                return false;
            }

            var splitIndex = value.LastIndexOf(':');
            if (splitIndex <= 0 || splitIndex >= value.Length - 1)
            {
                return false;
            }

            id = value.Substring(0, splitIndex);
            return int.TryParse(value.Substring(splitIndex + 1), out amount);
        }
    }
}
