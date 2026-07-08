using Fossick.Core.Application.Results;
using Fossick.Core.Data;

namespace Fossick.Core.Application.Adapters
{
    public interface IFossickViewAdapter
    {
        void Refresh(FossickGameplayData data, FossickActionResult result, FossickSnapshot snapshot);
    }
}
