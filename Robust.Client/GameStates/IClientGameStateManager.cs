using System;
using Robust.Shared.GameObjects;
using Robust.Shared.Input;
using Robust.Shared.Timing;

namespace Robust.Client.GameStates
{
    /// <summary>
    ///     Engine service that provides processing and management of game states.
    /// </summary>
    public interface IClientGameStateManager
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
        ///     The current tick of the last server game state applied.
        /// </summary>
        /// <remarks>
        ///     Use this to synchronize server-sent simulation events with the client's game loop.
        /// </remarks>
        GameTick CurServerTick { get; }

        /// <summary>
        ///     If the buffer size is this many states larger than the target buffer size,
        ///     apply the overflow of states in a single tick.
        /// </summary>
        int StateBufferMergeThreshold { get; }

        /// <summary>
        ///     This is called after the game state has been applied for the current tick.
        /// </summary>
        event Action<GameStateAppliedArgs> GameStateApplied;

        /// <summary>
        ///     One time initialization of the service.
        /// </summary>
        void Initialize();

        /// <summary>
        ///     Resets the service back to its initial state.
        /// </summary>
        void Reset();

        /// <summary>
        ///     Applies the game state for this tick.
        /// </summary>
        void ApplyGameState();

        /// <summary>
        ///     An input command has been dispatched.
        /// </summary>
        /// <param name="message">Message being dispatched.</param>
        void InputCommandDispatched(FullInputCmdMessage message);

        uint SystemMessageDispatched<T>(T message) where T : EntityEventArgs;
    }
}
