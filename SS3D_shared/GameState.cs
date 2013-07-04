using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using Lidgren.Network;
using NetSerializer;
using SS13_Shared.GO;
using SS13_Shared.Serialization;

namespace SS13_Shared
{
    [Serializable]
    public class GameState : INetSerializableType
    {
        public uint Sequence { get; private set;}
        public List<EntityState> EntityStates { get; set; }

        [NonSerialized] 
        private MemoryStream SerializedData;

        [NonSerialized] private bool serialized = false;

        public GameState(uint sequence)
        {
            Sequence = sequence;
        }
        
        public NetOutgoingMessage GameStateUpdate(NetOutgoingMessage StateUpdateMessage)
        {
            StateUpdateMessage.Write(Sequence);
            return StateUpdateMessage;
        }

        public NetOutgoingMessage WriteDelta(NetOutgoingMessage StateDeltaMessage)
        {
            return StateDeltaMessage;
        }

        public static GameStateDelta operator - (GameState toState, GameState fromState)
        {
            return Delta(fromState, toState);
        }

        public static GameState operator + (GameState fromState, GameStateDelta delta)
        {
            return Patch(fromState, delta);
        }

        public static GameState operator + (GameStateDelta delta, GameState fromState)
        {
            return Patch(fromState, delta);
        }

        public static GameStateDelta Delta(GameState fromState, GameState toState)
        {
            var delta = new GameStateDelta();
            delta.Sequence = toState.Sequence;
            delta.Create(fromState ,toState);
            return delta;
        }

        public static GameState Patch(GameState fromState, GameStateDelta delta)
        {
            return delta.Apply(fromState);
        }

        private void Serialize()
        {
            if (serialized)
                return;
            SerializedData = new MemoryStream();
            Serializer.Serialize(SerializedData, this);
            serialized = true;
        }

        public MemoryStream GetSerializedDataStream()
        {
            if(!serialized)
            {
                Serialize();
            }

            return SerializedData;
        }

        public byte[] GetSerializedDataBuffer()
        {
            if (!serialized)
            {
                Serialize();
            }

            return SerializedData.ToArray();
        }

        /// <summary>
        /// Deserializes a game state from a byte array and returns it
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public static GameState Deserialize(byte[] data)
        {
            return (GameState)Serializer.Deserialize(new MemoryStream(data));
        }

        /// <summary>
        /// Writes a full game state into a NetOutgoingMessage
        /// </summary>
        /// <param name="message">NetOutgoingMessage to write to</param>
        public int WriteStateMessage(NetOutgoingMessage message)
        {
            message.Write((byte)NetMessage.FullState);
            var stateData = Compress(GetSerializedDataBuffer());
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
            var sequence = message.ReadUInt32();
            var length = message.ReadInt32();
            var stateData = Decompress(message.ReadBytes(length));
            using (var stateStream = new MemoryStream(stateData))
                return (GameState)Serializer.Deserialize(stateStream);
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
                    }
                    while (count > 0);
                    return memory.ToArray();
                }
            }
        }
    }
}
