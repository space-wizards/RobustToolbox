using SS14.Shared.GameObjects;
using SS14.Shared.Serialization;
using System;
using System.Collections.Generic;

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
        /// <param name="sequence"></param>
        public GameState(uint fromSequence, uint toSequence, List<EntityState> entities, List<PlayerState> players, List<EntityUid> deletions)
        {
            FromSequence = fromSequence;
            ToSequence = toSequence;
            EntityStates = entities;
            PlayerStates = players;
            EntityDeletions = deletions;
        }

        public readonly uint FromSequence;
        public readonly uint ToSequence;

        public readonly List<EntityState> EntityStates;
        public readonly List<PlayerState> PlayerStates;
        public readonly List<EntityUid> EntityDeletions;
    }
}
