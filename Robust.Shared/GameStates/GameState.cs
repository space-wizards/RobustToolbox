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
        public GameState(GameTick fromSequence, GameTick toSequence, uint lastInput, NetListAsArray<EntityState> entities, NetListAsArray<PlayerState> players, NetListAsArray<EntityUid> deletions, GameStateMapData? mapData)
        {
            FromSequence = fromSequence;
            ToSequence = toSequence;
            LastProcessedInput = lastInput;
            EntityStates = entities;
            PlayerStates = players;
            EntityDeletions = deletions;
            MapData = mapData;
        }

        public readonly GameTick FromSequence;
        public readonly GameTick ToSequence;

        public readonly uint LastProcessedInput;

        public readonly NetListAsArray<EntityState> EntityStates;
        public readonly NetListAsArray<PlayerState> PlayerStates;
        public readonly NetListAsArray<EntityUid> EntityDeletions;
        public readonly GameStateMapData? MapData;
    }
}
