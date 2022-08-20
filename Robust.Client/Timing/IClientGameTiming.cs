using JetBrains.Annotations;
using Robust.Shared.Timing;

namespace Robust.Client.Timing
{
    public interface IClientGameTiming : IGameTiming
    {
        void StartPastPrediction();
        void EndPastPrediction();

        void StartStateApplication();
        void EndStateApplication();

        [MustUseReturnValue]
        PredictionGuard StartPastPredictionArea()
        {
            StartPastPrediction();

            return new PredictionGuard(this);
        }

        [MustUseReturnValue]
        StateApplicationGuard StartStateApplicationArea()
        {
            StartStateApplication();

            return new StateApplicationGuard(this);
        }
    }
}
