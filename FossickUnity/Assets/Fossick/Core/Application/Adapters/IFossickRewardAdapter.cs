using Fossick.Core.Application.Results;
using Fossick.Core.Data;

namespace Fossick.Core.Application.Adapters
{
    public interface IFossickRewardAdapter
    {
        void Commit(FossickActionResult result, FossickRewardData rewards);
    }
}
