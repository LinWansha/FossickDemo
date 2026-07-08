using Fossick.Core.Data;

namespace Fossick.Core.Application.Adapters
{
    public interface IFossickStorageAdapter
    {
        void Save(FossickGameplayData data);
    }
}
