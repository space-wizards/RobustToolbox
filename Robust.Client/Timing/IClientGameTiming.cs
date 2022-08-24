using JetBrains.Annotations;
using Robust.Shared.Timing;

namespace Robust.Client.Timing
{
    public interface IClientGameTiming : IGameTiming
    {
        /// <summary>
        /// This is functionally the clients "current-tick" before prediction, and represents the target value for <see
        /// cref="LastRealTick"/>. This value should increment by at least one every tick. It may increase by more than
        /// that if we apply several server states within a single tick.
        /// </summary>
        GameTick LastProcessedTick { get; set; }

        /// <summary>
        /// The last real non-extrapolated server state that was applied. Without networking issues, this tick should
        /// always correspond to <see cref="LastRealTick"/>, however if there is a missing states or the buffer has run
        /// out, this value may be smaller..
        /// </summary>
        GameTick LastRealTick { get; set; }

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
