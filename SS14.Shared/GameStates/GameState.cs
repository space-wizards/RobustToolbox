using SS14.Shared.GameObjects;
using SS14.Shared.Interfaces.Serialization;
using SS14.Shared.IoC;
using SS14.Shared.Serialization;
using System;
using System.Collections.Generic;
using System.IO;

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
        public GameState(uint fromSequence, uint toSequence, List<EntityState> entities, List<PlayerState> players)
        {
            FromSequence = fromSequence;
            ToSequence = toSequence;
            EntityStates = entities;
            PlayerStates = players;
        }

        public readonly uint FromSequence;
        public readonly uint ToSequence;

        public readonly List<EntityState> EntityStates;
        public readonly List<PlayerState> PlayerStates;
    }
}
