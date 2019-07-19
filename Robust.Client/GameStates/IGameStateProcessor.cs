using Robust.Shared.GameStates;
using Robust.Shared.Timing;

namespace Robust.Client.GameStates
{
    /// <summary>
    ///     Holds a collection of game states and calculates which ones to apply at a given game tick.
    /// </summary>
    internal interface IGameStateProcessor
    {
        /// <summary>
        ///     Minimum number of states needed in the buffer for everything to work.
        /// </summary>
        /// <remarks>
        ///     With interpolation enabled minimum is 3 states in buffer for the system to work (last, cur, next).
        ///     Without interpolation enabled minimum is 2 states in buffer for the system to work (last, cur).
        /// </remarks>
        int MinBufferSize { get; }

        /// <summary>
        ///     The number of states the system is trying to keep in the buffer. This will always
        ///     be greater or equal to <see cref="MinBufferSize"/>.
        /// </summary>
        int TargetBufferSize { get; }

        /// <summary>
        ///     Number of game states currently in the state buffer.
        /// </summary>
        int CurrentBufferSize { get; }

        /// <summary>
        ///     Is frame interpolation turned on?
        /// </summary>
        bool Interpolation { get; set; }

        /// <summary>
        ///     The target number of states to keep in the buffer for network smoothing.
        /// </summary>
        /// <remarks>
        ///     For Lan, set this to 0. For Excellent net conditions, set this to 1. For normal network conditions,
        ///     set this to 2. For worse conditions, set it higher.
        /// </remarks>
        int InterpRatio { get; set; }

        /// <summary>
        ///     If the client clock runs ahead of the server and the buffer gets emptied, should fake extrapolated states be generated?
        /// </summary>
        bool Extrapolation { get; set; }

        /// <summary>
        ///     Is debug logging enabled? This will dump debug info about every state to the log.
        /// </summary>
        bool Logging { get; set; }

        /// <summary>
        ///     Adds a new state into the processor. These are usually from networking or replays.
        /// </summary>
        /// <param name="state">Newly received state.</param>
        void AddNewState(GameState state);

        /// <summary>
        ///     Calculates the current and next state to apply for a given game tick.
        /// </summary>
        /// <param name="curTick">Tick to get the states for.</param>
        /// <param name="curState">Current state for the given tick. This can be null.</param>
        /// <param name="nextState">Current state for tick + 1. This can be null.</param>
        /// <returns>Was the function able to correctly calculate the states for the given tick?</returns>
        bool ProcessTickStates(GameTick curTick, out GameState curState, out GameState nextState);

        /// <summary>
        ///     Resets the processor back to its initial state.
        /// </summary>
        void Reset();
    }
}
