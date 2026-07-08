using Fossick.Core.Application.Commands;
using Fossick.Core.Application.Results;

namespace Fossick.Core.Application.Adapters
{
    public interface IFossickTelemetryAdapter
    {
        void Track(FossickCommand command, FossickActionResult result, FossickSnapshot snapshot);
    }
}
