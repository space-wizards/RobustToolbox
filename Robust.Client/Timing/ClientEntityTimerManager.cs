using Robust.Shared.IoC;
using Robust.Shared.Timing;

namespace Robust.Client.Timing;

internal sealed partial class ClientEntityTimerManager : EntityTimerManager
{
    [Dependency] private IClientGameTiming _clientTiming = default!;

    protected override (bool Predicted, bool OutsidePrediction) GetQueuesToProcess(bool noPredictions)
    {
        if (_clientTiming.ApplyingState)
            return (false, false);

        return (!noPredictions, true);
    }
}
