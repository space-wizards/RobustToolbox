using System;
using System.Collections.Generic;
using Robust.Shared;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.Input;
using Robust.Shared.Network.Messages;
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
        ///     Number of applicable game states currently in the state buffer.
        /// </summary>
        int CurrentBufferSize { get; }

        /// <summary>
        ///     If the buffer size is this many states larger than the target buffer size,
        ///     apply the overflow of states in a single tick.
        /// </summary>
        int StateBufferMergeThreshold { get; }

        /// <summary>
        /// Whether prediction is currently enabled on the client entirely.
        /// This is NOT equal to <see cref="IGameTiming.InPrediction"/> or <see cref="IGameTiming.IsFirstTimePredicted"/>.
        /// </summary>
        /// <remarks>This is effectively an alias of <see cref="CVars.NetPredict"/>.</remarks>
        bool IsPredictionEnabled { get; }

        /// <summary>
        ///     This is called after the game state has been applied for the current tick.
        /// </summary>
        event Action<GameStateAppliedArgs> GameStateApplied;

        /// <summary>
        ///     This is invoked whenever a pvs-leave message is received.
        /// </summary>
        public event Action<MsgStateLeavePvs>? PvsLeave;

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
        ///     Applies a given set of game states.
        /// </summary>
        IEnumerable<EntityUid> ApplyGameState(GameState curState, GameState? nextState);

        /// <summary>
        ///     Resets any entities that have changed while predicting future ticks.
        /// </summary>
        void ResetPredictedEntities();

        /// <summary>
        ///     An input command has been dispatched.
        /// </summary>
        /// <param name="message">Message being dispatched.</param>
        void InputCommandDispatched(FullInputCmdMessage message);

        /// <summary>
        ///     Requests a full state from the server. This should override even implicit entity data.
        /// </summary>
        public void RequestFullState(EntityUid? missingEntity = null);

        uint SystemMessageDispatched<T>(T message) where T : EntityEventArgs;

        void UpdateFullRep(GameState state);
    }
}
