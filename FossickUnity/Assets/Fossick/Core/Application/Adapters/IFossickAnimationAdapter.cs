using System;

namespace Fossick.Core.Application.Adapters
{
    public interface IFossickAnimationAdapter
    {
        bool Play(FossickAnimationContext context, Action onCompleted);
    }
}
