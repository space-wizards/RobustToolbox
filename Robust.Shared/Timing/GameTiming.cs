using System;
using System.Linq;
using Robust.Shared.Maths;
using Robust.Shared.Utility;

namespace Robust.Shared.Timing
{
    /// <summary>
    ///     This holds main loop timing information and helper functions.
    /// </summary>
    [Virtual]
    public class GameTiming : IGameTiming
    {
        // number of sample frames to store for profiling
        private const int NumFrames = 60;

        private readonly IStopwatch _realTimer = new Stopwatch();
        private readonly long[] _realFrameTimes = new long[NumFrames];
        private int _frameIdx;
        private TimeSpan _lastRealTime;

        /// <summary>
        ///     Default constructor.
        /// </summary>
        public GameTiming()
        {
            // does nothing if timer is already running
            _realTimer.Start();

            Paused = true;
            TickRate = NumFrames;
        }

        /// <summary>
        /// Is program execution inside of the simulation update, or rendering?
        /// </summary>
        public bool InSimulation { get; set; }

        /// <summary>
        ///     Is the simulation currently paused?
        /// </summary>
        public bool Paused { get; set; }

        // Cached values for making CurTime not jump whenever the tickrate
        // changes. This holds the last calculated value of CurTime, and the
        // tick that the value was calculated for. The next time CurTime is
        // calculated, it should only try to monotonically increase both of
        // these values
        //
        // Notice that it starts from GameTick 1  - the "first tick" has no impact
        // on timing
        public (TimeSpan, GameTick) TimeBase { get; set; } = (TimeSpan.Zero, GameTick.First);

        /// <summary>
        ///     The current synchronized uptime of the simulation. Use this for in-game timing. This can be rewound for
        ///     prediction, and is affected by Paused and TimeScale.
        /// </summary>
        public TimeSpan CurTime
        {
            get
            {
                // last tickrate change epoch
                var (time, lastTimeTick) = TimeBase;

                // add our current time to it.
                // the server never rewinds time, and the client never rewinds time outside of prediction.
                // the only way this assert should fail is if the TickRate is changed inside prediction, which should never happen.
                //DebugTools.Assert(CurTick >= lastTimeTick);
                //TODO: turns out prediction leaves CurTick at the last predicted tick, and not at the last processed server tick
                //so time gets rewound when processing events like TickRate.
                time += TickPeriod.Mul(CurTick.Value - lastTimeTick.Value);

                if (!InSimulation) // rendering can draw frames between ticks
                {
                    DebugTools.Assert(0 <= (time + TickRemainder).TotalSeconds);
                    return time + TickRemainder;
                }

                DebugTools.Assert(0 <= time.TotalSeconds);
                return time;
            }
        }

        /// <summary>
        ///     The current real uptime of the simulation. Use this for UI and out of game timing.
        /// </summary>
        public TimeSpan RealTime => _realTimer.Elapsed;

        public virtual TimeSpan ServerTime => TimeSpan.Zero;

        /// <summary>
        ///     The simulated time it took to render the last frame.
        /// </summary>
        public TimeSpan FrameTime => CalcFrameTime();

        /// <summary>
        ///     The real time it took to render the last frame.
        /// </summary>
        public TimeSpan RealFrameTime { get; private set; }

        /// <summary>
        ///     Average real frame time over the last 50 frames.
        /// </summary>
        public TimeSpan RealFrameTimeAvg => TimeSpan.FromTicks((long)_realFrameTimes.Average());

        /// <summary>
        ///     Standard Deviation of the real frame time over the last 50 frames.
        /// </summary>
        public TimeSpan RealFrameTimeStdDev => CalcRftStdDev();

        /// <summary>
        ///     Average real FPS over the last 50 frames.
        /// </summary>
        public double FramesPerSecondAvg => CalcFpsAvg();

        /// <summary>
        ///     The current simulation tick being processed.
        /// </summary>
        public GameTick CurTick { get; set; } = new(1); // Time always starts on the first tick

        /// <summary>
        ///     Timespan for the last tick.
        /// </summary>
        public TimeSpan LastTick { get; set; }

        private ushort _tickRate;
        private TimeSpan _tickRemainder;

        /// <summary>
        ///     The target ticks/second of the simulation.
        /// </summary>
        public ushort TickRate
        {
            get => _tickRate;
            set => SetTickRateAt(value, CurTick);
        }

        /// <summary>
        ///     The length of a tick at the current TickRate. 1/TickRate.
        /// </summary>
        public TimeSpan TickPeriod => TimeSpan.FromTicks((long)(1.0 / TickRate * TimeSpan.TicksPerSecond));

        /// <summary>
        /// The remaining time left over after the last tick was ran.
        /// </summary>
        public TimeSpan TickRemainder
        {
            get => _tickRemainder;
            set
            {
                // Generally the upper limit is Tickrate*2, but changing the tickrate mid-round can make this really large until timing can stabilize
                DebugTools.Assert(TimeSpan.Zero <= TickRemainder);
                _tickRemainder = value;
            }
        }

        public TimeSpan CalcAdjustedTickPeriod()
        {
            // ranges from -1 to 1, with 0 being 'default'
            var ratio = MathHelper.Clamp(TickTimingAdjustment, -0.99f, 0.99f);

            // Final period ranges from near 0 (runs very fast to catch up) or 2 * tick period (runs at half speed).
            return TickPeriod * (1-ratio);
        }

        /// <summary>
        ///     Current graphics frame since init OpenGL which is taken as frame 1, from swapbuffer to swapbuffer. Useful to set a
        ///     conditional breakpoint on specific frames, and synchronize with OGL debugging tools that capture frames.
        ///     Depending on the tools used, this frame number will vary between 1 frame more or less due to how that tool is counting frames,
        ///     i.e. starting from 0 or 1, having a separate counter, etc. Available in timing debug panel.
        /// </summary>
        public uint CurFrame { get; set; } = 1;

        /// <inheritdoc />
        public float TickTimingAdjustment { get; set; } = 0;

        /// <summary>
        ///     Ends the 'lap' of the timer, updating frame time info.
        /// </summary>
        public void StartFrame()
        {
            // calculate real timing info
            var curRealTime = RealTime;
            RealFrameTime = curRealTime - _lastRealTime;
            _lastRealTime = curRealTime;

            // update profiling
            _frameIdx = (1 + _frameIdx) % _realFrameTimes.Length;
            _realFrameTimes[_frameIdx] = RealFrameTime.Ticks;
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

        // Calculate and store the current time value, based on the current tick rate.
        // Call this whenever you change the TickRate.
        private void CacheCurTime(GameTick atTick)
        {
            var (cachedTime, lastTimeTick) = TimeBase;

            var newTime = cachedTime + TickPeriod.Mul(atTick.Value - lastTimeTick.Value);

            DebugTools.Assert(TimeSpan.Zero <= newTime);
            TimeBase = (newTime, atTick);
        }

        /// <summary>
        /// Resets the simulation time.
        /// </summary>
        public void ResetSimTime()
        {
            ResetSimTime((TimeSpan.Zero, GameTick.First));
        }

        public void ResetSimTime((TimeSpan, GameTick) timeBase)
        {
            TimeBase = timeBase;
            CurTick = GameTick.First;
            TickRemainder = TimeSpan.Zero;
            Paused = true;
        }

        public void SetTickRateAt(ushort tickRate, GameTick atTick)
        {
            // Check this, because TickRate is a divisor in the cache calculation
            // The first time TickRate is set, no time will have passed anyways
            if (_tickRate != 0)
                // Cache BEFORE updating the tick rate, because ticks up until
                // now have been on a different rate, so they count for a
                // different amount of time
                CacheCurTime(atTick);

            _tickRate = tickRate;
        }

        public virtual TimeSpan RealLocalToServer(TimeSpan local)
        {
            return TimeSpan.Zero;
        }

        public virtual TimeSpan RealServerToLocal(TimeSpan server)
        {
            return TimeSpan.Zero;
        }

        protected virtual TimeSpan? GetServerOffset()
        {
            return null;
        }

        public bool IsFirstTimePredicted { get; protected set; } = true;

        /// <inheritdoc />
        public virtual bool InPrediction => false;

        /// <inheritdoc />
        public bool ApplyingState {get; protected set; }

        /// <summary>
        ///     Calculates the average FPS of the last 50 real frame times.
        /// </summary>
        /// <returns></returns>
        private double CalcFpsAvg()
        {
            return 1 / (_realFrameTimes.Average() / TimeSpan.TicksPerSecond);
        }

        /// <summary>
        ///     Calculates the standard deviation of the last 50 real frame times.
        /// </summary>
        /// <returns></returns>
        private TimeSpan CalcRftStdDev()
        {
            var sum = _realFrameTimes.Sum();
            var count = _realFrameTimes.Length;
            var avg = sum / (double)count;
            double devSquared = 0.0f;
            for (var i = 0; i < count; ++i)
            {
                if (_realFrameTimes[i] == 0)
                    continue;

                var ft = _realFrameTimes[i];

                var dt = ft - avg;

                devSquared += dt * dt;
            }

            var variance = devSquared / (count - 1);
            return TimeSpan.FromTicks((long)Math.Sqrt(variance));
        }
    }
}
