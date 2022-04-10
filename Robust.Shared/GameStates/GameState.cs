using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;
using System;
using System.Diagnostics;
using NetSerializer;
using Robust.Shared.Timing;

namespace Robust.Shared.GameStates
{
    [DebuggerDisplay("GameState from={FromSequence} to={ToSequence} ext={Extrapolated}")]
    [Serializable, NetSerializable]
    public sealed class GameState
    {
        /// <summary>
        ///     An extrapolated state that was created artificially by the client.
        ///     It does not contain any real data from the server.
        /// </summary>
        [field:NonSerialized]
        public bool Extrapolated { get; set; }

        /// <summary>
        ///     The serialized size in bytes of this game state.
        /// </summary>
        [field:NonSerialized]
        public int PayloadSize { get; set; }

        /// <summary>
        /// Constructor!
        /// </summary>
        public GameState(GameTick fromSequence, GameTick toSequence, uint lastInput, NetListAsArray<EntityState> entities, NetListAsArray<PlayerState> players, NetListAsArray<EntityUid> deletions, GameStateMapData? mapData, TimeSpan? toTime = null)
        {
            FromSequence = fromSequence;
            ToSequence = toSequence;
            ToTime = toTime;
            LastProcessedInput = lastInput;
            EntityStates = entities;
            PlayerStates = players;
            EntityDeletions = deletions;
            MapData = mapData;
        }

        public readonly GameTick FromSequence;
        public readonly GameTick ToSequence;

        /// <summary>
        ///     The current time corresponding to tick <see cref="ToSequence"/>.
        /// </summary>
        /// <remarks>
        ///     If tick-rate is constant, this could just be inferred from the current tick. But with dynamic tick
        ///     timing, and in the event of tick time cvar updates being dropped, this just ensures they stay in sync.
        /// </remarks>
        public readonly TimeSpan? ToTime;

        public readonly uint LastProcessedInput;

        public readonly NetListAsArray<EntityState> EntityStates;
        public readonly NetListAsArray<PlayerState> PlayerStates;
        public readonly NetListAsArray<EntityUid> EntityDeletions;
        public readonly GameStateMapData? MapData;
    }
}
