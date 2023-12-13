using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;
using System;
using System.Diagnostics;
using NetSerializer;
using Robust.Shared.Timing;

namespace Robust.Shared.GameStates
{
    [DebuggerDisplay("GameState from={FromSequence} to={ToSequence}")]
    [Serializable, NetSerializable]
    public sealed class GameState
    {
        /// <summary>
        ///     The serialized size in bytes of this game state.
        /// </summary>
        [field:NonSerialized]
        public int PayloadSize { get; set; }

        /// <summary>
        /// Constructor!
        /// </summary>
        public GameState(
            GameTick fromSequence,
            GameTick toSequence,
            uint lastInput,
            NetListAsArray<EntityState> entities,
            NetListAsArray<SessionState> players,
            NetListAsArray<NetEntity> deletions)
        {
            FromSequence = fromSequence;
            ToSequence = toSequence;
            LastProcessedInput = lastInput;
            EntityStates = entities;
            PlayerStates = players;
            EntityDeletions = deletions;
        }

        public readonly GameTick FromSequence;
        public readonly GameTick ToSequence;

        public readonly uint LastProcessedInput;

        public readonly NetListAsArray<EntityState> EntityStates;
        public readonly NetListAsArray<SessionState> PlayerStates;
        public readonly NetListAsArray<NetEntity> EntityDeletions;
    }
}
