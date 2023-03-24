using System;
using Robust.Shared.IoC;
using Robust.Shared.Network;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Robust.Client.Timing
{
    public sealed class ClientGameTiming : GameTiming, IClientGameTiming
    {
        [Dependency] private readonly IClientNetManager _netManager = default!;

        public override bool InPrediction => !ApplyingState && CurTick > LastRealTick;

        /// <inheritdoc />
        public GameTick LastRealTick { get; set; }

        /// <inheritdoc />
        public GameTick LastProcessedTick { get; set; }

        public override TimeSpan ServerTime
        {
            get
            {
                var offset = GetServerOffset();
                if (offset == null)
                {
                    return TimeSpan.Zero;
                }

                return RealTime + offset.Value;
            }
        }

        public override TimeSpan RealLocalToServer(TimeSpan local)
        {
            var offset = GetServerOffset();
            if (offset == null)
                return TimeSpan.Zero;

            return local + offset.Value;
        }

        public override TimeSpan RealServerToLocal(TimeSpan server)
        {
            var offset = GetServerOffset();
            if (offset == null)
                return TimeSpan.Zero;

            return server - offset.Value;
        }

        protected override TimeSpan? GetServerOffset()
        {
            return _netManager.ServerChannel?.RemoteTimeOffset;
        }

        public void StartPastPrediction()
        {
            // Don't allow recursive predictions.
            // Not sure if it's necessary yet and if not, great!
            DebugTools.Assert(IsFirstTimePredicted);

            IsFirstTimePredicted = false;
        }

        public void EndPastPrediction()
        {
            DebugTools.Assert(!IsFirstTimePredicted);

            IsFirstTimePredicted = true;
        }

        public void StartStateApplication()
        {
            DebugTools.Assert(IsFirstTimePredicted, "Starting state application in the middle of a past prediction.");
            IsFirstTimePredicted = false;
            ApplyingState = true;
        }

        public void EndStateApplication()
        {
            IsFirstTimePredicted = true;
            ApplyingState = false;
        }
    }
}
