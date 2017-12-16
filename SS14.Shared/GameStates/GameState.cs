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
        [NonSerialized] private bool _serialized;
        [NonSerialized] private MemoryStream _serializedData;
        [NonSerialized] public float GameTime;
        /// <summary>
        /// Constructor!
        /// </summary>
        /// <param name="sequence"></param>
        public GameState(uint sequence)
        {
            Sequence = sequence;
        }

        public uint Sequence { get; private set; }
        public List<EntityState> EntityStates { get; set; }
        public List<PlayerState> PlayerStates { get; set; }

        /// <summary>
        /// Creates a delta from two game states
        /// </summary>
        /// <param name="toState"></param>
        /// <param name="fromState"></param>
        /// <returns></returns>
        public static GameStateDelta operator -(GameState toState, GameState fromState)
        {
            return Delta(fromState, toState);
        }

        /// <summary>
        /// Applies a delta to a game state
        /// </summary>
        /// <param name="fromState"></param>
        /// <param name="delta"></param>
        /// <returns></returns>
        public static GameState operator +(GameState fromState, GameStateDelta delta)
        {
            return Patch(fromState, delta);
        }

        /// <summary>
        /// Applies a delta to a game state
        /// </summary>
        /// <param name="delta"></param>
        /// <param name="fromState"></param>
        /// <returns></returns>
        public static GameState operator +(GameStateDelta delta, GameState fromState)
        {
            return Patch(fromState, delta);
        }

        public static GameStateDelta Delta(GameState fromState, GameState toState)
        {
            var delta = new GameStateDelta();
            delta.Sequence = toState.Sequence;
            delta.Create(fromState, toState);
            return delta;
        }

        public static GameState Patch(GameState fromState, GameStateDelta delta)
        {
            return delta.Apply(fromState);
        }

        /// <summary>
        /// Serializes the game state to its buffer -- allows us to serialize only once, saving some processor time
        /// </summary>
        private void Serialize()
        {
            if (_serialized)
                return;
            _serializedData = new MemoryStream();
            var serializer = IoCManager.Resolve<ISS14Serializer>();
            serializer.Serialize(_serializedData, this);
            _serialized = true;
        }

        /// <summary>
        /// Returns the serialized game state data stream
        /// </summary>
        /// <returns></returns>
        public MemoryStream GetSerializedDataStream()
        {
            if (!_serialized)
            {
                Serialize();
            }

            return _serializedData;
        }

        /// <summary>
        /// Returns the serialized game state data as a byte array
        /// </summary>
        /// <returns></returns>
        public byte[] GetSerializedDataBuffer()
        {
            if (!_serialized)
            {
                Serialize();
            }

            return _serializedData.ToArray();
        }

        /// <summary>
        /// Deserializes a game state from a byte array and returns it
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public static GameState Deserialize(byte[] data)
        {
            var serializer = IoCManager.Resolve<ISS14Serializer>();
            using (var stream = new MemoryStream(data))
            {
                return serializer.Deserialize<GameState>(new MemoryStream(data));
            }
        }
    }
}
