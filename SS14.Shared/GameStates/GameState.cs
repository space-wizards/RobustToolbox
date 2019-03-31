using SS14.Shared.GameObjects;
using SS14.Shared.Serialization;
using System;
using System.Collections.Generic;
using SS14.Shared.Timing;

namespace SS14.Shared.GameStates
{
    [Serializable, NetSerializable]
    public class GameState
    {
        [NonSerialized] private float _gameTime;

        public float GameTime
        {
            get => _gameTime;
            set => _gameTime = value;
        }

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
