using System;

namespace Robust.Shared.Interfaces.Timing
{
    public readonly struct PredictionGuard : IDisposable
    {
        private readonly IGameTiming _gameTiming;

        public PredictionGuard(IGameTiming gameTiming)
        {
            _gameTiming = gameTiming;
        }

        public void Dispose()
        {
            _gameTiming.EndPastPrediction();
        }
    }
}
