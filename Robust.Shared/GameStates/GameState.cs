using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;
using System;
using System.Collections.Generic;
using Robust.Shared.Timing;

namespace Robust.Shared.GameStates
{
    [Serializable, NetSerializable]
    public class GameState
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
        public GameState(GameTick fromSequence, GameTick toSequence, List<EntityState> entities, List<PlayerState> players, List<EntityUid> deletions, GameStateMapData mapData)
        {
            FromSequence = fromSequence;
            ToSequence = toSequence;
            EntityStates = entities;
            PlayerStates = players;
            EntityDeletions = deletions;
            MapData = mapData;
        }

        public readonly GameTick FromSequence;
        public readonly GameTick ToSequence;

        public readonly List<EntityState> EntityStates;
        public readonly List<PlayerState> PlayerStates;
        public readonly List<EntityUid> EntityDeletions;
        public readonly GameStateMapData MapData;
    }
}
