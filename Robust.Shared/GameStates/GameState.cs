using System;
using System.Collections.Generic;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;

namespace Robust.Shared.GameStates
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
        public GameState(uint fromSequence, uint toSequence, List<EntityState> entities, List<PlayerState> players, List<EntityUid> deletions, GameStateMapData mapData)
        {
            FromSequence = fromSequence;
            ToSequence = toSequence;
            EntityStates = entities;
            PlayerStates = players;
            EntityDeletions = deletions;
            MapData = mapData;
        }

        public readonly uint FromSequence;
        public readonly uint ToSequence;

        public readonly List<EntityState> EntityStates;
        public readonly List<PlayerState> PlayerStates;
        public readonly List<EntityUid> EntityDeletions;
        public readonly GameStateMapData MapData;
    }
}
