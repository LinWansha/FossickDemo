using Fossick.Core.Application.Results;

namespace Fossick.Core.Application.Adapters
{
    public interface IFossickAnimationAdapter
    {
        void Play(FossickActionResult result, FossickSnapshot snapshot);
    }
}
