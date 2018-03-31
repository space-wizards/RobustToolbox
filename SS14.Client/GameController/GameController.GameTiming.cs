using System;
using SS14.Shared.Timing;
using SS14.Shared.Interfaces.Timing;

namespace SS14.Client
{
    public sealed partial class GameController
    {
        // TODO: This class is basically just a bunch of stubs.
        private class GameTiming : IGameTiming
        {
            private static readonly IStopwatch _realTimer = new Stopwatch();
            public readonly IStopwatch _tickRemainderTimer = new Stopwatch();

            public GameTiming()
            {
                _realTimer.Start();
                // Not sure if Restart() starts it implicitly so...
                _tickRemainderTimer.Start();
            }

            public bool InSimulation { get; set; }
            public bool Paused { get; set; }

            public TimeSpan CurTime => CalcCurTime();

            public TimeSpan RealTime => _realTimer.Elapsed;

            public TimeSpan FrameTime => CalcFrameTime();

            public TimeSpan RealFrameTime { get; set; }

            public TimeSpan RealFrameTimeAvg => throw new NotImplementedException();

            public TimeSpan RealFrameTimeStdDev => throw new NotImplementedException();

            public double FramesPerSecondAvg => throw new NotImplementedException();

            public uint CurTick { get; set; }
            public int TickRate
            {
                get => Godot.Engine.IterationsPerSecond;
                set => Godot.Engine.IterationsPerSecond = value;
            }

            public TimeSpan TickPeriod => TimeSpan.FromTicks((long)(1.0 / TickRate * TimeSpan.TicksPerSecond));

            public TimeSpan TickRemainder { get; set; }

            public void ResetRealTime()
            {
                throw new NotImplementedException();
            }

            public void StartFrame()
            {
                throw new NotImplementedException();
            }

            private TimeSpan CalcCurTime()
            {
                // calculate simulation CurTime
                var time = TimeSpan.FromTicks(TickPeriod.Ticks * CurTick);

                if (!InSimulation) // rendering can draw frames between ticks
                    return time + TickRemainder;
                return time;
            }

            private TimeSpan CalcFrameTime()
            {
                // calculate simulation FrameTime
                if (InSimulation)
                {
                    return TimeSpan.FromTicks(TickPeriod.Ticks);
                }
                else
                {
                    return Paused ? TimeSpan.Zero : RealFrameTime;
                }
            }
        }
    }
}
