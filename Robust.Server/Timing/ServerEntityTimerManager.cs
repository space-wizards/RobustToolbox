using Robust.Shared.Timing;

namespace Robust.Server.Timing;

internal sealed class ServerEntityTimerManager : EntityTimerManager
{
    protected override (bool Predicted, bool OutsidePrediction) GetQueuesToProcess(bool noPredictions)
    {
        return (true, true);
    }
}
