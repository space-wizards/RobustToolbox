using System;

namespace SS14.Shared.Interfaces.Timing
{
    /// <summary>
    ///     This holds main loop timing information and helper functions.
    /// </summary>
    public interface IGameTiming
    {
        /// <summary>
        ///     Is the simulation currently paused?
        /// </summary>
        bool Paused { get; set; }

        /// <summary>
        ///     How fast time passes in the simulation compared to RealTime. 1.0 = 100%, 0.25 = 25% (slow mo).
        ///     Minimum timescale is 0.1, max is 2.0.
        /// </summary>
        double TimeScale { get; set; }

        /// <summary>
        ///     The current synchronized uptime of the simulation. Use this for in-game timing. This can be rewound for
        ///     prediction, and is affected by Paused and TimeScale.
        /// </summary>
        TimeSpan CurTime { get; }

        /// <summary>
        ///     The current real uptime of the simulation. Use this for UI and out of game timing.
        /// </summary>
        TimeSpan RealTime { get; }

        /// <summary>
        ///     The simulated time it took to render the last frame.
        /// </summary>
        TimeSpan FrameTime { get; set; }

        /// <summary>
        ///     The real time it took to render the last frame.
        /// </summary>
        TimeSpan RealFrameTime { get; set; }

        /// <summary>
        ///     Average real frame time over the last 50 frames.
        /// </summary>
        TimeSpan RealFrameTimeAvg { get; }

        /// <summary>
        ///     Standard Deviation of the frame time over the last 50 frames.
        /// </summary>
        TimeSpan RealFrameTimeStdDev { get; }

        /// <summary>
        ///     Average real FPS over the last 50 frames.
        /// </summary>
        double FramesPerSecondAvg { get; }

        /// <summary>
        ///     The current simulation tick being processed.
        /// </summary>
        uint CurTick { get; set; }

        /// <summary>
        ///     The target ticks/second of the simulation.
        /// </summary>
        int TickRate { get; set; }

        /// <summary>
        ///     The length of a tick at the current TickRate. 1/TickRate.
        /// </summary>
        TimeSpan TickPeriod { get; }

        /// <summary>
        ///     Ends the 'lap' of the timer, updating frame time info.
        /// </summary>
        void StartFrame();

        /// <summary>
        ///     Resets the real uptime of the server.
        /// </summary>
        void ResetRealTime();
    }
}
