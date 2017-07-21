using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using SS14.Shared.Interfaces.Timing;

namespace SS14.Shared.Timing
{
    /// <summary>
    /// This holds main loop timing information and helper functions.
    /// </summary>
    public class GameTiming : IGameTiming
    {
        // number of sample frames to store for profiling
        private const int NumFrames = 50;

        private readonly List<long> _realFrameTimes = new List<long>(NumFrames);

        private static Stopwatch _realTimer;
        private TimeSpan _lastRealTime;
        
        /// <summary>
        /// Default constructor.
        /// </summary>
        public GameTiming()
        {
            if (_realTimer == null)
            {
                _realTimer = new Stopwatch();
                _realTimer.Start();
            }

            Paused = false;
            TimeScale = 1.0f;
            TickRate = NumFrames;
        }

        /// <summary>
        /// Is the simulation currently paused?
        /// </summary>
        public bool Paused { get; set; }

        /// <summary>
        /// How fast time passes in the simulation compared to RealTime. 1.0 = 100%, 0.25 = 25% (slow mo).
        /// Minimum timescale is 0.1, max is 2.0.
        /// </summary>
        public double TimeScale { get; set; }

        //TODO: Figure out how to actually relate CurTime to RealTime
        /// <summary>
        /// The current synchronized uptime of the simulation. Use this for in-game timing. This can be rewound for 
        /// prediction, and is affected by Paused and TimeScale.
        /// </summary>
        public TimeSpan CurTime => TimeSpan.FromTicks(TickPeriod.Ticks * CurTick);

        /// <summary>
        /// The current real uptime of the simulation. Use this for UI and out of game timing.
        /// </summary>
        public TimeSpan RealTime => _realTimer.Elapsed;

        //TODO: Figure out how to actually relate FrameTime to RealFrameTime
        /// <summary>
        /// The simulated time it took to render the last frame.
        /// </summary>
        public TimeSpan FrameTime { get; set; }

        /// <summary>
        /// The real time it took to render the last frame.
        /// </summary>
        public TimeSpan RealFrameTime { get; set; }

        /// <summary>
        /// Average real frame time over the last 50 frames.
        /// </summary>
        public TimeSpan RealFrameTimeAvg => TimeSpan.FromTicks((long)_realFrameTimes.Average());

        /// <summary>
        /// Standard Deviation of the real frame time over the last 50 frames.
        /// </summary>
        public TimeSpan RealFrameTimeStdDev => CalcRftStdDev();

        /// <summary>
        /// Average real FPS over the last 50 frames.
        /// </summary>
        public double FramesPerSecondAvg => CalcFpsAvg();

        /// <summary>
        /// The current simulation tick being processed.
        /// </summary>
        public uint CurTick { get; set; }
        
        /// <summary>
        /// The target ticks/second of the simulation.
        /// </summary>
        public int TickRate { get; set; }

        /// <summary>
        /// The length of a tick at the current TickRate. 1/TickRate.
        /// </summary>
        public TimeSpan TickPeriod => new TimeSpan((long) (1.0 / TickRate * TimeSpan.TicksPerSecond));

        /// <summary>
        /// Ends the 'lap' of the timer, updating frame time info.
        /// </summary>
        public void StartFrame()
        {
            var curTime = _realTimer.Elapsed;
            var delta = curTime - _lastRealTime;
            _lastRealTime = curTime;
            RealFrameTime = delta;

            if (_realFrameTimes.Count >= NumFrames)
                _realFrameTimes.RemoveAt(0);
            _realFrameTimes.Add(delta.Ticks);
        }

        /// <summary>
        /// Resets the real uptime of the server.
        /// </summary>
        public void ResetRealTime()
        {
            _realTimer.Restart();
            _lastRealTime = TimeSpan.Zero;
        }

        /// <summary>
        /// Calculates the average FPS of the last 50 real frame times.
        /// </summary>
        /// <returns></returns>
        private double CalcFpsAvg()
        {
            if (_realFrameTimes.Count == 0)
                return 0;

            return 1 / (_realFrameTimes.Average() / TimeSpan.TicksPerSecond);
        }

        /// <summary>
        /// Calculates the standard deviation of the last 50 real frame times.
        /// </summary>
        /// <returns></returns>
        private TimeSpan CalcRftStdDev()
        {
            var sum = _realFrameTimes.Sum();
            var count = _realFrameTimes.Count;
            var avg = sum / (double)count;
            double devSquared = 0.0f;
            for (var i = 0; i < count; ++i)
            {
                if (_realFrameTimes[i] == 0)
                    continue;

                var ft = _realFrameTimes[i];

                var dt = ft - avg;

                devSquared += (dt * dt);
            }

            var variance = devSquared / (count - 1);
            return TimeSpan.FromTicks((long) Math.Sqrt(variance));
        }
    }
}
