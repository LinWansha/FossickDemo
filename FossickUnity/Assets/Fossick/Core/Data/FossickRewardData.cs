using System.Collections.Generic;
using Fossick.Core.Application.Results;
using Fossick.Core.Definition.Config;
using Fossick.Core.Generation;
using System;

namespace Fossick.Core.Data
{
    public sealed class FossickRewardData
    {
        private readonly Dictionary<string, int> collections = new Dictionary<string, int>();
        private readonly HashSet<string> discoveredCollections = new HashSet<string>();
        private readonly List<string> collectionDrawIds = new List<string>();
        private int collectionDrawSeed;

        public int score;
        public int coins;
        public int collectionDrawCount;
        public List<string> collectionItems = new List<string>();
        public List<string> collectionDiscoveredItems = new List<string>();
        public IReadOnlyDictionary<string, int> Collections => collections;

        public void Apply(FossickRewardEvent reward, FossickInventoryData inventory)
        {
            if (reward == null)
            {
                return;
            }

            var amount = reward.amount;
            switch (reward.elementType)
            {
                case FossickElementType.Coin:
                    coins += amount;
                    break;
                case FossickElementType.Ore:
                    score += amount;
                    break;
                case FossickElementType.Collection:
                    reward.resolvedId = ResolveCollectionId(reward.id);
                    AddCollection(reward.resolvedId, amount);
                    break;
                case FossickElementType.Item:
                    ApplyItemReward(reward.id, amount, inventory);
                    break;
            }
        }

        public void AddCollection(string id, int amount)
        {
            if (!collections.ContainsKey(id))
            {
                collections.Add(id, 0);
            }

            collections[id] += amount;
            discoveredCollections.Add(id);
            collectionItems = CreateCollectionSaveList();
            collectionDiscoveredItems = CreateCollectionDiscoveredSaveList();
        }

        public bool HasDiscoveredCollection(string id)
        {
            return !string.IsNullOrEmpty(id) && discoveredCollections.Contains(id);
        }

        public void ConfigureCollectionDraw(IReadOnlyList<string> collectionIds, int seed)
        {
            if (collectionIds == null || collectionIds.Count != 5)
            {
                throw new InvalidOperationException("Fossick collection item config must contain exactly five items.");
            }

            collectionDrawIds.Clear();
            for (var i = 0; i < collectionIds.Count; i++)
            {
                var id = collectionIds[i];
                if (string.IsNullOrEmpty(id) || collectionDrawIds.Contains(id))
                {
                    throw new InvalidOperationException("Fossick collection item config contains an invalid or duplicate id.");
                }

                collectionDrawIds.Add(id);
            }

            collectionDrawSeed = seed;
            collectionItems = CreateCollectionSaveList();
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

            var success = consumed == setSize;
            if (success)
            {
                collectionItems = CreateCollectionSaveList();
            }

            return success;
        }

        public bool ConsumeCollectionSet(IReadOnlyList<string> collectionIds)
        {
            if (collectionIds == null || collectionIds.Count == 0)
            {
                return false;
            }

            for (var i = 0; i < collectionIds.Count; i++)
            {
                var id = collectionIds[i];
                if (string.IsNullOrEmpty(id) || !collections.TryGetValue(id, out var amount) || amount <= 0)
                {
                    return false;
                }
            }

            for (var i = 0; i < collectionIds.Count; i++)
            {
                collections[collectionIds[i]]--;
            }

            collectionItems = CreateCollectionSaveList();
            return true;
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

        public List<string> CreateCollectionDiscoveredSaveList()
        {
            var result = new List<string>(discoveredCollections);
            result.Sort();
            return result;
        }

        public void LoadCollectionSaveList(List<string> values)
        {
            collections.Clear();
            collectionItems = new List<string>(values);

            for (var i = 0; i < values.Count; i++)
            {
                if (TryParseCollection(values[i], out var id, out var amount))
                {
                    collections[id] = amount;
                }
            }
        }

        public void LoadCollectionDiscoveredSaveList(List<string> values)
        {
            discoveredCollections.Clear();
            collectionDiscoveredItems = values == null ? new List<string>() : new List<string>(values);
            for (var i = 0; i < collectionDiscoveredItems.Count; i++)
            {
                var id = collectionDiscoveredItems[i];
                if (!string.IsNullOrEmpty(id))
                {
                    discoveredCollections.Add(id);
                }
            }

            foreach (var item in collections)
            {
                if (item.Value > 0)
                {
                    discoveredCollections.Add(item.Key);
                }
            }

            collectionDiscoveredItems = CreateCollectionDiscoveredSaveList();
        }

        private static void ApplyItemReward(string id, int amount, FossickInventoryData inventory)
        {
            if (FossickContentIds.Tool.TryGetType(id, out var toolType))
            {
                inventory.AddTool(toolType, amount);
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

        private string ResolveCollectionId(string id)
        {
            if (!string.IsNullOrEmpty(id) &&
                id != FossickContentIds.Reward.CollectionBox &&
                id != FossickContentIds.Reward.CollectionPiece &&
                id != FossickContentIds.Reward.DefaultCollection)
            {
                return id;
            }

            if (collectionDrawIds.Count == 0)
            {
                throw new InvalidOperationException("Fossick collection draw is not configured.");
            }

            var min = int.MaxValue;
            var max = int.MinValue;
            for (var i = 0; i < collectionDrawIds.Count; i++)
            {
                var amount = GetCollectionAmount(collectionDrawIds[i]);
                min = Math.Min(min, amount);
                max = Math.Max(max, amount);
            }

            var candidates = new List<string>(collectionDrawIds.Count);
            var excludeMax = max - min >= 2;
            for (var i = 0; i < collectionDrawIds.Count; i++)
            {
                var candidate = collectionDrawIds[i];
                if (!excludeMax || GetCollectionAmount(candidate) < max)
                {
                    candidates.Add(candidate);
                }
            }

            var drawState = unchecked(collectionDrawSeed * 397 ^ collectionDrawCount * 7919 ^ 0x5F3759DF);
            var random = new FossickSeededRandom(collectionDrawSeed, drawState);
            var selected = candidates[random.RangeExclusive(0, candidates.Count)];
            collectionDrawCount++;
            return selected;
        }

        private int GetCollectionAmount(string id)
        {
            return collections.TryGetValue(id, out var amount) ? amount : 0;
        }
    }
}
