using System;
using System.Threading;
using SS14.Shared.Interfaces.Timing;
using SS14.Shared.Log;

namespace SS14.Shared.Timing
{
    /// <summary>
    ///     Manages the main game loop for a GameContainer.
    /// </summary>
    public class GameLoop
    {
        private readonly IGameTiming _timing;
        private TimeSpan _lastTick; // last wall time tick
        private TimeSpan _lastKeepUp; // last wall time keep up announcement

        public event EventHandler<FrameEventArgs> Input;
        public event EventHandler<FrameEventArgs> Tick;
        public event EventHandler<FrameEventArgs> Update;
        public event EventHandler<FrameEventArgs> Render;

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
        ///     The method currently being used to limit the Update rate.
        /// </summary>
        public SleepMode SleepMode { get; set; } = SleepMode.Yield;

        public GameLoop(IGameTiming timing)
        {
            _timing = timing;
        }

        /// <summary>
        ///     Start running the loop. This function will block for as long as the loop is Running.
        ///     Set Running to false to exit the loop and return from this function.
        /// </summary>
        public void Run()
        {
            if(_timing.TickRate <= 0)
                throw new InvalidOperationException("TickRate must be greater than 0.");

            Running = true;

            // maximum number of ticks to queue before the loop slows down.
            var maxTime = TimeSpan.FromTicks(_timing.TickPeriod.Ticks * MaxQueuedTicks);

            var realFrameEvent = new MutableFrameEventArgs(0);
            var simFrameEvent = new MutableFrameEventArgs(0);

            _timing.ResetRealTime();

            while (Running)
            {
                var accumulator = _timing.RealTime - _lastTick;

                // If the game can't keep up, limit time.
                if (accumulator > maxTime)
                {
                    // limit accumulator to max time.
                    accumulator = maxTime;

                    // pull lastTick up to the current realTime
                    // This will slow down the simulation, but if we are behind from a
                    // lag spike hopefully it will be able to catch up.
                    _lastTick = _timing.RealTime - maxTime;

                    // announce we are falling behind
                    if ((_timing.RealTime - _lastKeepUp).TotalSeconds >= 15.0)
                    {
                        Logger.Warning("[ENG] MainLoop: Cannot keep up!");
                        _lastKeepUp = _timing.RealTime;
                    }
                }
                _timing.StartFrame();
                
                realFrameEvent.SetDeltaSeconds((float)_timing.RealFrameTime.TotalSeconds);

                // process Net/KB/Mouse input
                Input?.Invoke(this, realFrameEvent);

                _timing.InSimulation = true;

                // run the simulation for every accumulated tick
                while (accumulator >= _timing.TickPeriod)
                {
                    accumulator -= _timing.TickPeriod;
                    _lastTick += _timing.TickPeriod;

                    // only run the simulation if unpaused, but still use up the accumulated time
                    if (_timing.Paused)
                        continue;

                    // update the simulation
                    simFrameEvent.SetDeltaSeconds((float)_timing.FrameTime.TotalSeconds);
                    Tick?.Invoke(this, simFrameEvent);
                    _timing.CurTick++;

                    if (SingleStep)
                        _timing.Paused = true;
                }

                // if not paused, save how close to the next tick we are so interpolation works
                if (!_timing.Paused)
                    _timing.TickRemainder = accumulator;

                _timing.InSimulation = false;

                // update out of the simulation
                simFrameEvent.SetDeltaSeconds((float)_timing.FrameTime.TotalSeconds);
                Update?.Invoke(this, simFrameEvent);

                // render the simulation
                Render?.Invoke(this, realFrameEvent);

                // Set sleep to 1 if you want to be nice and give the rest of the timeslice up to the os scheduler.
                // Set sleep to 0 if you want to use 100% cpu, but still cooperate with the scheduler.
                // do not call sleep if you want to be 'that thread' and hog 100% cpu.
                if(SleepMode != SleepMode.None)
                    Thread.Sleep((int)SleepMode);
            }
        }
    }

    /// <summary>
    ///     Methods the GameLoop can use to limit the Update rate.
    /// </summary>
    public enum SleepMode
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
