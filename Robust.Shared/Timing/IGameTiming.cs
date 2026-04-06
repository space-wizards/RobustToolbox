using System;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;

namespace Robust.Shared.Timing
{
    /// <summary>
    ///     This holds main loop timing information and helper functions.
    /// </summary>
    [NotContentImplementable]
    public interface IGameTiming
    {
        /// <summary>
        /// Is program execution inside the simulation, or outside of it (rendering, input handling, etc.)
        /// </summary>
        bool InSimulation { get; set; }

        /// <summary>
        ///     Is the simulation currently paused?
        /// </summary>
        /// <remarks>
        ///     When true, system update loops are not ran and relative time (like <see cref="CurTime"/>) does not
        ///     advance. This is useful for fully idling a game server and can be automatically managed by
        ///     <see cref="CVars.GameAutoPauseEmpty"/>.
        /// </remarks>
        bool Paused { get; set; }

        /// <summary>
        ///     The current synchronized uptime of the simulation. Use this for in-game timing. This can be rewound for
        ///     prediction, and is affected by Paused and TimeScale.
        /// </summary>
        TimeSpan CurTime { get; }

        /// <summary>
        ///     The current real uptime of the simulation. Use this for UI and out of game timing, it is not affected
        ///     by pausing, timescale, or lag.
        /// </summary>
        TimeSpan RealTime { get; }

        /// <summary>
        /// The time (on the <see cref="RealTime"/> scale) when the current frame started.
        /// </summary>
        TimeSpan FrameStartTime { get; }

        /// <summary>
        ///     The <see cref="RealTime"/> of the server.
        /// </summary>
        /// <remarks>
        ///     0 if we are the client and we are not connected to a server.
        ///     <see cref="RealTime"/> if we are the server.
        /// </remarks>
        TimeSpan ServerTime { get; }

        /// <summary>
        ///     The simulated time it took to render the last frame.
        /// </summary>
        TimeSpan FrameTime { get; }

        /// <summary>
        ///     The real time it took to render the last frame.
        /// </summary>
        TimeSpan RealFrameTime { get; }

        /// <summary>
        ///     Average real frame time over the last 50 frames.
        /// </summary>
        TimeSpan RealFrameTimeAvg { get; }

        /// <summary>
        ///     Standard Deviation of the frame time over the last 50 frames.
        /// </summary>
        TimeSpan RealFrameTimeStdDev { get; }

        /// <summary>
        ///     Current graphics frame since init OpenGL which is taken as frame 1. Useful to set a conditional breakpoint on specific frames, and
        ///     synchronize with OGL debugging tools that capture frames. Depending on the tools used, this frame
        ///     number will vary between 1 frame more or less due to how that tool is counting frames,
        ///     i.e. starting from 0 or 1, having a separate counter, etc. Available in timing debug panel.
        /// </summary>
        uint CurFrame { get; set; }

        /// <summary>
        ///     Average real FPS over the last 50 frames.
        /// </summary>
        double FramesPerSecondAvg { get; }

        /// <summary>
        ///     The current simulation tick being processed.
        /// </summary>
        GameTick CurTick { get; set; }

        /// <summary>
        ///     Time, relative to <see cref="RealTime"/>, the last tick started at.
        ///     If we're currently in simulation, that's THIS tick.
        /// </summary>
        TimeSpan LastTick { get; set; }

        /// <summary>
        ///     The target ticks/second of the simulation.
        /// </summary>
        /// <remarks>
        /// This is specified in simulation time, not real time.
        /// </remarks>
        ushort TickRate { get; set; }

        /// <summary>
        /// The scale of simulation time to real time.
        /// </summary>
        /// <remarks>
        /// A scale of 2 means the game should go "twice as slow"
        /// </remarks>
        float TimeScale { get; set; }

        /// <summary>
        /// The baseline time value that CurTime is calculated relatively to.
        /// </summary>
        (TimeSpan, GameTick) TimeBase { get; set; }

        /// <summary>
        ///     The length of a tick at the current TickRate. 1/TickRate.
        /// </summary>
        /// <remarks>
        /// This is in simulation time, not necessarily real time.
        /// </remarks>
        TimeSpan TickPeriod { get; }

        /// <summary>
        /// The remaining time left over after the last tick was ran.
        /// </summary>
        TimeSpan TickRemainder { get; set; }

        /// <summary>
        /// <see cref="TickRemainder"/> in real time.
        /// </summary>
        TimeSpan TickRemainderRealtime { get; }

        /// <summary>
        /// Calculate the amount of <b>real time</b> to wait between ticks.
        /// </summary>
        /// <remarks>
        /// This is adjusted for various "out of simulation"
        /// factors such as <see cref="TickTimingAdjustment"/> and <see cref="TimeScale"/>.
        /// </remarks>
        TimeSpan CalcAdjustedTickPeriod();

        /// <summary>
        ///     Fraction of how far into the tick we are. <c>0</c> is 0% and <see cref="ushort.MaxValue"/> is 100%.
        /// </summary>
        ushort TickFraction
        {
            get
            {
                if (InSimulation)
                {
                    return ushort.MaxValue;
                }

                return (ushort)(ushort.MaxValue * TickRemainder.TotalSeconds / TickPeriod.TotalSeconds);
            }
        }

        /// <summary>
        ///     If the client clock is a little behind or ahead of the server, you can
        ///     use the to adjust the timing of the clock speed. The default value is 0,
        ///     and you can run the clock from -1 (almost stopped) to 1 (almost no delay).
        ///     This has no effect on in-simulation timing, and only changes the speed at which
        ///     the simulation progresses in relation to Real time. Don't mess with this unless
        ///     you know what you are doing. DO NOT TOUCH THIS ON SERVER.
        /// </summary>
        float TickTimingAdjustment { get; set; }

        /// <summary>
        ///     Ends the 'lap' of the timer, updating frame time info.
        /// </summary>
        void StartFrame();

        /// <summary>
        ///     Is this the first time CurTick has been predicted?
        /// </summary>
        bool IsFirstTimePredicted { get; }

        /// <summary>
        ///     True if CurTick is ahead of LastRealTick, and <see cref="ApplyingState"/> is false.
        /// </summary>
        /// <remarks>
        ///     This means the client is currently running ahead of the server, to fill in the gaps for the player and
        ///     reduce latency while waiting for the next game state to arrive.
        /// </remarks>
        bool InPrediction { get; }

        /// <summary>
        ///     If true, the game is currently in the process of applying a game server-state.
        /// </summary>
        bool ApplyingState { get; }

        string TickStamp => $"{CurTick}, predFirst: {IsFirstTimePredicted}, tickRem: {TickRemainder.TotalSeconds}, sim: {InSimulation}";

        /// <summary>
        ///     Statically-accessible version of <see cref="TickStamp"/>.
        /// </summary>
        /// <remarks>
        ///     This is intended as a debugging aid, and should not be used in regular committed code.
        /// </remarks>
        static string TickStampStatic => IoCManager.Resolve<IGameTiming>().TickStamp;

        /// <summary>
        ///     Resets the simulation time completely. While functional, no mainstream RobustToolbox game currently uses
        ///     this outside of client synchronization with the server and it may have quirks on existing titles.
        /// </summary>
        /// <remarks>
        ///     To avoid potential desynchronization where some entities think they have changes from the far future,
        ///     this should be accompanied by a full ECS reset using <see cref="IEntityManager.FlushEntities"/>.
        /// </remarks>
        void ResetSimTime();
        /// <inheritdoc cref="ResetSimTime()"/>
        void ResetSimTime((TimeSpan, GameTick) timeBase);

        void SetTickRateAt(ushort tickRate, GameTick atTick);

        TimeSpan RealLocalToServer(TimeSpan local);
        TimeSpan RealServerToLocal(TimeSpan server);
    }
}
