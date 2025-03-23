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
        ///     With interpolation enabled the minimum is 2 states (current & next tick). Without interpolation the
        ///     minimum is just 1.
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
        int GetApplicableStateCount();

        /// <summary>
        ///     Total number of game states currently in the state buffer.
        /// </summary>
        int StateCount { get; }

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
        event Action<MsgStateLeavePvs>? PvsLeave;

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
        IEnumerable<NetEntity> ApplyGameState(GameState curState, GameState? nextState);

        void MergeImplicitData();

        /// <summary>
        ///     Resets any entities that have changed while predicting future ticks.
        /// </summary>
        void ResetPredictedEntities();

        /// <summary>
        ///     An input command has been dispatched.
        /// </summary>
        /// <param name="message">Message being dispatched.</param>
        void InputCommandDispatched(ClientFullInputCmdMessage clientMsg, FullInputCmdMessage message);

        /// <summary>
        ///     Requests a full state from the server. This should override even implicit entity data.
        /// </summary>
        void RequestFullState(NetEntity? missingEntity = null, GameTick? tick = null);

        uint SystemMessageDispatched<T>(T message) where T : EntityEventArgs;

        /// <summary>
        ///     Updates the cached game sates that are used to reset predicted entities.
        /// </summary>
        /// <param name="cloneDelta">If true, this will clone old states while applying delta states, rather than
        /// modifying them directly. Useful if they are still cached elsewhere (e.g., replays).</param>
        void UpdateFullRep(GameState state, bool cloneDelta = false);

        /// <summary>
        /// Returns the full collection of cached game states that are used to reset predicted entities.
        /// </summary>
        Dictionary<NetEntity, Dictionary<ushort, IComponentState?>> GetFullRep();

        /// <summary>
        /// This will perform some setup in order to reset the game to an earlier state. To fully reset the state
        /// <see cref="ApplyGameState()"/> still needs to be called separately.
        /// </summary>
        /// <remarks>
        /// This function will delete any networked entities that are not present in the given game state. Any child
        /// entities that are in the state will simply be sent to null-space. This will also reset
        /// <see cref="MetaDataComponent.LastStateApplied"/> to zero, so that <see cref="ApplyGameState()"/> will
        /// actually apply the state.
        /// </remarks>
        /// <param name="state">
        /// The state to reset to.
        /// </param>
        /// <param name="resetAllEntities">
        /// Whether or not to reset <see cref="MetaDataComponent.LastStateApplied"/> for all entities, or only those
        /// that have been modified some after the states <see cref="GameState.ToSequence"/>. This effectively
        /// determines whether we should do a full-state reset, or only reset recently modified entities.
        /// </param>
        /// <param name="deleteClientEntities">
        /// Whether to delete all client-side entities (which are never part of the networked game state).
        /// </param>
        /// <param name="deleteClientChildren">
        /// Whether to delete client-side entities that are parented to networked that are about to be deleted during
        /// the partial reset. E.g., if this is true, then a client-side muzzle flash effect entity that is parented to
        /// a networked gun entity will get deleted if that gun is about to be deleted. If false, the entity will
        /// simply be detached to nullspace. This option has no effect if <see cref="deleteClientEntities"/> is true.
        /// </param>
        void PartialStateReset(
            GameState state,
            bool resetAllEntities,
            bool deleteClientEntities = false,
            bool deleteClientChildren = true);

        /// <summary>
        /// Queue a collection of entities that are to be detached to null-space & marked as PVS-detached.
        /// This store and modify the list given to it.
        /// </summary>
        void QueuePvsDetach(List<NetEntity> entities, GameTick tick);

        /// <summary>
        /// Immediately detach several entities.
        /// </summary>
        void DetachImmediate(List<NetEntity> entities);

        /// <summary>
        /// Clears the PVS detach queue.
        /// </summary>
        void ClearDetachQueue();
    }
}
