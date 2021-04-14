using System;

namespace Robust.Client.Timing
{
    public readonly struct PredictionGuard : IDisposable
    {
        private readonly IClientGameTiming _gameTiming;

        public PredictionGuard(IClientGameTiming gameTiming)
        {
            _gameTiming = gameTiming;
        }

        public void Dispose()
        {
            _gameTiming.EndPastPrediction();
        }
    }
}
