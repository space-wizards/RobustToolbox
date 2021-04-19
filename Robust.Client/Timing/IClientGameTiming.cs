using JetBrains.Annotations;
using Robust.Shared.Timing;

namespace Robust.Client.Timing
{
    public interface IClientGameTiming : IGameTiming
    {
        void StartPastPrediction();
        void EndPastPrediction();

        [MustUseReturnValue]
        PredictionGuard StartPastPredictionArea()
        {
            StartPastPrediction();

            return new PredictionGuard(this);
        }
    }
}
