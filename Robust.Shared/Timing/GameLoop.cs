using System;
using System.Threading;
using Robust.Shared.Log;
using Robust.Shared.Exceptions;
using Robust.Shared.Maths;
using Prometheus;
using Robust.Shared.Profiling;

namespace Robust.Shared.Timing
{
    public interface IGameLoop
    {
        event EventHandler<FrameEventArgs> Input;
        event EventHandler<FrameEventArgs> Tick;
        event EventHandler<FrameEventArgs> Update;
        event EventHandler<FrameEventArgs> Render;

        /// <summary>
        ///     Enables single step mode. If this is enabled, after every tick the GameTime will pause.
        ///     Unpausing GameTime will run another single tick.
        /// </summary>
        bool SingleStep { get; set; }

        /// <summary>
        ///     Setting this to false will stop the loop after it has started running.
        /// </summary>
        bool Running { get; set; }

        /// <summary>
        ///     How many ticks behind the simulation can get before it starts to slow down.
        /// </summary>
        int MaxQueuedTicks { get; set; }

        /// <summary>
        ///     The method currently being used to limit the Update rate.
        /// </summary>
        SleepMode SleepMode { get; set; }

        /// <summary>
        ///     Start running the loop. This function will block for as long as the loop is Running.
        ///     Set Running to false to exit the loop and return from this function.
        /// </summary>
        void Run();
    }

    /// <summary>
    ///     Manages the main game loop for a GameContainer.
    /// </summary>
    public sealed class GameLoop : IGameLoop
    {
        public const string ProfTextStartFrame = "Start Frame";

        private static readonly Histogram _frameTimeHistogram = Metrics.CreateHistogram(
            "robust_game_loop_frametime",
            "Histogram of frametimes in ms",
            new HistogramConfiguration
            {
                Buckets = Histogram.ExponentialBuckets(.001, 1.5, 10)
            });

        private readonly IGameTiming _timing;
        private TimeSpan _lastKeepUp; // last wall time keep up announcement

        public event EventHandler<FrameEventArgs>? Input;
        public event EventHandler<FrameEventArgs>? Tick;
        public event EventHandler<FrameEventArgs>? Update;
        public event EventHandler<FrameEventArgs>? Render;

        /// <summary>
        ///     Enables single step mode. If this is enabled, after every tick the GameTime will pause.
        ///     Unpausing GameTime will run another single tick.
        /// </summary>
        public bool SingleStep { get; set; } = false;

        /// <summary>
        ///     Setting this to false will stop the loop after it has started running.
        /// </summary>
        public bool Running { get; set; }

        /// <summary>
        ///     How many ticks behind the simulation can get before it starts to slow down.
        /// </summary>
        public int MaxQueuedTicks { get; set; } = 5;

        /// <summary>
        ///     If true and the same event causes an event 10 times in a row, the game loop will shut itself down.
        /// </summary>
        public bool DetectSoftLock { get; set; }

        public bool EnableMetrics { get; set; } = false;

        /// <summary>
        ///     The method currently being used to limit the Update rate.
        /// </summary>
        public SleepMode SleepMode { get; set; } = SleepMode.Yield;

        // Only used on release mode.
        // ReSharper disable once NotAccessedField.Local
        private readonly IRuntimeLog _runtimeLog;
        private readonly ProfManager _prof;

#if EXCEPTION_TOLERANCE
        private int _tickExceptions;

        private const int MaxSoftLockExceptions = 10;
#endif

        public GameLoop(IGameTiming timing, IRuntimeLog runtimeLog, ProfManager prof)
        {
            _timing = timing;
            _runtimeLog = runtimeLog;
            _prof = prof;
        }

        /// <summary>
        ///     Start running the loop. This function will block for as long as the loop is Running.
        ///     Set Running to false to exit the loop and return from this function.
        /// </summary>
        public void Run()
        {
            if (_timing.TickRate <= 0)
                throw new InvalidOperationException("TickRate must be greater than 0.");

            Running = true;

            FrameEventArgs realFrameEvent;
            FrameEventArgs simFrameEvent;

            while (Running)
            {
                var profFrameStart = _prof.WriteValue(ProfTextStartFrame, ProfData.Int64(_timing.CurFrame));
                var profFrameGroupStart = _prof.WriteGroupStart();
                var profFrameSw = ProfSampler.StartNew();
                var profFrameGcGen0 = GC.CollectionCount(0);
                var profFrameGcGen1 = GC.CollectionCount(1);
                var profFrameGcGen2 = GC.CollectionCount(2);

                // maximum number of ticks to queue before the loop slows down.
                var maxTime = TimeSpan.FromTicks(_timing.TickPeriod.Ticks * MaxQueuedTicks);

                var accumulator = _timing.RealTime - _timing.LastTick;

                // If the game can't keep up, limit time.
                if (accumulator > maxTime)
                {
                    // limit accumulator to max time.
                    accumulator = maxTime;

                    // pull LastTick up to the current realTime
                    // This will slow down the simulation, but if we are behind from a
                    // lag spike hopefully it will be able to catch up.
                    _timing.LastTick = _timing.RealTime - maxTime;

                    // announce we are falling behind
                    if ((_timing.RealTime - _lastKeepUp).TotalSeconds >= 15.0)
                    {
                        Logger.WarningS("eng", "MainLoop: Cannot keep up!");
                        _lastKeepUp = _timing.RealTime;
                    }
                }

                _timing.StartFrame();
                realFrameEvent = new FrameEventArgs((float)_timing.RealFrameTime.TotalSeconds);
#if EXCEPTION_TOLERANCE
                try
#endif
                {
                    using var _ = _prof.Group("Input");

                    // process Net/KB/Mouse input
                    Input?.Invoke(this, realFrameEvent);
                }
#if EXCEPTION_TOLERANCE
                catch (Exception exp)
                {
                    _runtimeLog.LogException(exp, "GameLoop Input");
                }
#endif
                _timing.InSimulation = true;
                var tickPeriod = _timing.CalcAdjustedTickPeriod();

                using (_prof.Group("Ticks"))
                {
                    var countTicksRan = 0;
                    // run the simulation for every accumulated tick

                    while (accumulator >= tickPeriod)
                    {
                        accumulator -= tickPeriod;
                        _timing.LastTick += tickPeriod;

                        // only run the simulation if unpaused, but still use up the accumulated time
                        if (_timing.Paused)
                            continue;

                        _timing.TickRemainder = accumulator;
                        countTicksRan += 1;

                        // update the simulation
                        simFrameEvent = new FrameEventArgs((float)_timing.FrameTime.TotalSeconds);
#if EXCEPTION_TOLERANCE
                    var threw = false;
                    try
                    {
#endif
                        using var tickGroup = _prof.Group("Tick");
                        _prof.WriteValue("Tick", ProfData.Int64(_timing.CurTick.Value));

                        if (EnableMetrics)
                        {
                            using (_frameTimeHistogram.NewTimer())
                            {
                                Tick?.Invoke(this, simFrameEvent);
                            }
                        }
                        else
                        {
                            Tick?.Invoke(this, simFrameEvent);
                        }
#if EXCEPTION_TOLERANCE
                    }
                    catch (Exception exp)
                    {
                        threw = true;
                        _runtimeLog.LogException(exp, "GameLoop Tick");
                        _tickExceptions += 1;

                        if (_tickExceptions > MaxSoftLockExceptions && DetectSoftLock)
                        {
                            Logger.FatalS("eng",
                                "MainLoop: 10 consecutive exceptions inside GameLoop Tick, shutting down!");
                            Running = false;
                        }
                    }

                    if (!threw)
                    {
                        _tickExceptions = 0;
                    }
#endif
                        _timing.CurTick = new GameTick(_timing.CurTick.Value + 1);
                        tickPeriod = _timing.CalcAdjustedTickPeriod();

                        if (SingleStep)
                            _timing.Paused = true;
                    }

                    _prof.WriteValue("Tick count", ProfData.Int32(countTicksRan));
                }

                // if not paused, save how close to the next tick we are so interpolation works
                if (!_timing.Paused)
                    _timing.TickRemainder = accumulator;

                _timing.InSimulation = false;

                // update out of the simulation

#if EXCEPTION_TOLERANCE
                try
#endif
                {
                    using var _ = _prof.Group("Update");

                    Update?.Invoke(this, realFrameEvent);
                }
#if EXCEPTION_TOLERANCE
                catch (Exception exp)
                {
                    _runtimeLog.LogException(exp, "GameLoop Update");
                }
#endif

                // render the simulation
#if EXCEPTION_TOLERANCE
                try
#endif
                {
                    using (_prof.Group("Render"))
                    {
                        Render?.Invoke(this, realFrameEvent);
                    }
                }
#if EXCEPTION_TOLERANCE
                catch (Exception exp)
                {
                    _runtimeLog.LogException(exp, "GameLoop Render");
                }
#endif

                {
                    using var gc = _prof.Group("GC Overview");

                    _prof.WriteValue("Gen 0 Count", ProfData.Int32(GC.CollectionCount(0) - profFrameGcGen0));
                    _prof.WriteValue("Gen 1 Count", ProfData.Int32(GC.CollectionCount(1) - profFrameGcGen1));
                    _prof.WriteValue("Gen 2 Count", ProfData.Int32(GC.CollectionCount(2) - profFrameGcGen2));
                }

                _prof.WriteGroupEnd(profFrameGroupStart, "Frame", profFrameSw);
                _prof.MarkIndex(profFrameStart, ProfIndexType.Frame);

                // Set sleep to 1 if you want to be nice and give the rest of the timeslice up to the os scheduler.
                // Set sleep to 0 if you want to use 100% cpu, but still cooperate with the scheduler.
                // do not call sleep if you want to be 'that thread' and hog 100% cpu.
                if (SleepMode != SleepMode.None)
                    Thread.Sleep((int)SleepMode);
            }
        }
    }

    /// <summary>
    ///     Methods the GameLoop can use to limit the Update rate.
    /// </summary>
    public enum SleepMode : sbyte
    {
        /// <summary>
        ///     Thread will not yield to the scheduler or sleep, and consume 100% cpu. Use this if you are
        ///     limiting the rate by other means (rendering FPS with vsync), otherwise your computer will turn into a heater.
        /// </summary>
        None = -1,

        /// <summary>
        ///     Same as None, except you are yielding the rest of your timeslice to other OS threads at the end of each update.
        ///     This will run at 100% CPU if another thread does not hog all the CPU time, and your OS scheduler will be happier.
        /// </summary>
        Yield = 0,

        /// <summary>
        ///     Adds ~1ms thread sleep after every update. Use this to limit the Update rate of the loop, conserve power and
        ///     have low CPU usage. You should use this on a dedicated server.
        /// </summary>
        Delay = 1,
    }
}
