using System;
using SS14.Shared.Interfaces.Timing;

namespace SS14.Client
{
    public sealed partial class GameController
    {
        // TODO: This class is basically just a bunch of stubs.
        private class GameTiming : IGameTiming
        {
            public bool InSimulation { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
            public bool Paused { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
            public double TimeScale { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

            public TimeSpan CurTime => throw new NotImplementedException();

            public TimeSpan RealTime { get; set; }

            public TimeSpan FrameTime => throw new NotImplementedException();

            public TimeSpan RealFrameTime => throw new NotImplementedException();

            public TimeSpan RealFrameTimeAvg => throw new NotImplementedException();

            public TimeSpan RealFrameTimeStdDev => throw new NotImplementedException();

            public double FramesPerSecondAvg => throw new NotImplementedException();

            public uint CurTick { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
            public int TickRate { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

            public TimeSpan TickPeriod => throw new NotImplementedException();

            public TimeSpan TickRemainder { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

            public void ResetRealTime()
            {
                throw new NotImplementedException();
            }

            public void StartFrame()
            {
                throw new NotImplementedException();
            }
        }
    }
}
