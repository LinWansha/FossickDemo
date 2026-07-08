using Fossick.Core.Definition.Config;

namespace Fossick.Core.Mine.Objects
{
    public sealed class CollectionPayload : FossickRewardPayload
    {
        public CollectionPayload(string collectionId, int amount)
            : base(FossickElementType.Collection, collectionId, amount)
        {
            CollectionId = collectionId;
        }

        public string CollectionId { get; }
    }
}
