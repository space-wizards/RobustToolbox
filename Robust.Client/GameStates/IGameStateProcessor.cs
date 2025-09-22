using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.Timing;

namespace Robust.Client.GameStates
{
    /// <summary>
    ///     Holds a collection of game states and calculates which ones to apply at a given game tick.
    ///     It also stores a copy of all the last entity states from the server,
    ///     allowing the game to be reset to a server-like condition at any point.
    /// </summary>
    internal interface IGameStateProcessor
    {
        /// <summary>
        ///     Minimum number of states needed in the buffer for everything to work.
        /// </summary>
        /// <remarks>
        ///     With interpolation enabled minimum is 2 states in buffer for the system to work (cur, next).
        ///     Without interpolation enabled minimum is 2 states in buffer for the system to work (cur).
        /// </remarks>
        int MinBufferSize { get; }

        /// <summary>
        ///     The number of states the system is trying to keep in the buffer. This will always
        ///     be greater or equal to <see cref="MinBufferSize"/>.
        /// </summary>
        int TargetBufferSize { get; }

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
        int BufferSize { get; set; }

        /// <summary>
        ///     Is debug logging enabled? This will dump debug info about every state to the log.
        /// </summary>
        bool Logging { get; set; }


        /// <summary>
        ///     Adds a new state into the processor. These are usually from networking or replays.
        /// </summary>
        /// <param name="state">Newly received state.</param>
        /// <returns>Returns true if the state was accepted and should be acknowledged</returns>
        bool AddNewState(GameState state);
        //> usually from replays
        //replays when

        /// <summary>
        ///     Calculates the current and next state to apply for a given game tick.
        /// </summary>
        /// <param name="curTick">Tick to get the states for.</param>
        /// <param name="curState">Current state for the given tick. This can be null.</param>
        /// <param name="nextState">Current state for tick + 1. This can be null.</param>
        /// <returns>Was the function able to correctly calculate the states for the given tick?</returns>
        bool TryGetServerState([NotNullWhen(true)] out GameState? curState, out GameState? nextState);

        /// <summary>
        ///     Resets the processor back to its initial state.
        /// </summary>
        void Reset();

        /// <summary>
        ///     Merges entity data into the full copy of the server states.
        /// </summary>
        /// <remarks>
        ///     This is necessary because the server does not send data
        ///     that can be inferred from entity creation on new entity states.
        ///     This data thus has to be re-constructed client-side and merged with this method.
        /// </remarks>
        /// <param name="data">
        ///     The data to merge.
        ///     It's a dictionary of entity ID -> (component net ID -> ComponentState)
        /// </param>
        void MergeImplicitData(Dictionary<NetEntity, Dictionary<ushort, IComponentState?>> data);

        /// <summary>
        ///     Get the last state data from the server for an entity.
        /// </summary>
        /// <returns>Dictionary (net ID -> ComponentState)</returns>
        Dictionary<ushort, IComponentState?> GetLastServerStates(NetEntity entity);

        /// <summary>
        ///     Calculate the number of applicable states in the game state buffer from a given tick.
        ///     This includes only applicable states. If there is a gap, future buffers are not included.
        /// </summary>
        /// <param name="fromTick">The tick to calculate from.</param>
        int GetApplicableStateCount(GameTick? fromTick);

        bool TryGetLastServerStates(NetEntity entity,
            [NotNullWhen(true)] out Dictionary<ushort, IComponentState?>? dictionary);
    }
}
