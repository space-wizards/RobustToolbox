using System;
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

        public bool SingleStep { get; set; } = false;
        public bool Running { get; set; }
        public int MaxQueuedTicks { get; set; } = 5;

        public GameLoop(IGameTiming timing)
        {
            _timing = timing;
        }

        public void Run()
        {
            if(_timing.TickRate <= 0)
                throw new InvalidOperationException("TickRate must be greater than 0.");

            Running = true;

            // maximum number of ticks to queue before the loop slows down.
            _timing.ResetRealTime();
            var maxTime = TimeSpan.FromTicks(_timing.TickPeriod.Ticks * MaxQueuedTicks);

            var realFrameEvent = new MutableFrameEventArgs(0);
            var simFrameEvent = new MutableFrameEventArgs(0);

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
            }
        }
    }
}
