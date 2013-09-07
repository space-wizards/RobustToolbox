using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using Lidgren.Network;
using NetSerializer;
using SS13_Shared.GO;
using SS13_Shared.Serialization;

namespace SS13_Shared.GameStates
{
    [Serializable]
    public class GameState : INetSerializableType
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
            Serializer.Serialize(_serializedData, this);
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
            return (GameState) Serializer.Deserialize(new MemoryStream(data));
        }

        /// <summary>
        /// Writes a full game state into a NetOutgoingMessage
        /// </summary>
        /// <param name="message">NetOutgoingMessage to write to</param>
        public int WriteStateMessage(NetOutgoingMessage message)
        {
            message.Write((byte) NetMessage.FullState);
            byte[] stateData = Compress(GetSerializedDataBuffer());
            message.Write(Sequence);
            message.Write(stateData.Length);
            message.Write(stateData);
            return stateData.Length;
        }

        /// <summary>
        /// Reads a full gamestate from a NetIncomingMessage
        /// </summary>
        /// <param name="message">NetIncomingMessage that contains the state data</param>
        /// <returns></returns>
        public static GameState ReadStateMessage(NetIncomingMessage message)
        {
            uint sequence = message.ReadUInt32();
            int length = message.ReadInt32();
            byte[] stateData = Decompress(message.ReadBytes(length));
            using (var stateStream = new MemoryStream(stateData))
                return (GameState) Serializer.Deserialize(stateStream);
        }

        /// <summary>
        /// Compresses a decompressed state data byte array into a compressed one.
        /// </summary>
        /// <param name="stateData">full state data</param>
        /// <returns></returns>
        private static byte[] Compress(byte[] stateData)
        {
            var compressedDataStream = new MemoryStream();
            using (var gzip = new GZipStream(compressedDataStream, CompressionMode.Compress, true))
            {
                gzip.Write(stateData, 0, stateData.Length);
            }
            return compressedDataStream.ToArray();
        }

        /// <summary>
        /// Decompresses a compressed state data byte array into a decompressed one.
        /// </summary>
        /// <param name="compressedStateData">compressed state data</param>
        /// <returns></returns>
        private static byte[] Decompress(byte[] compressedStateData)
        {
            // Create a GZIP stream with decompression mode.
            // ... Then create a buffer and write into while reading from the GZIP stream.
            using (var stream = new GZipStream(new MemoryStream(compressedStateData), CompressionMode.Decompress))
            {
                const int size = 4096;
                var buffer = new byte[size];
                using (var memory = new MemoryStream())
                {
                    int count = 0;
                    do
                    {
                        count = stream.Read(buffer, 0, size);
                        if (count > 0)
                        {
                            memory.Write(buffer, 0, count);
                        }
                    } while (count > 0);
                    return memory.ToArray();
                }
            }
        }
    }
}