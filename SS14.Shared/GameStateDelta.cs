using BsDiffLib;
using Lidgren.Network;
using NetSerializer;
using SS14.Shared.GameStates;
using SS14.Shared.Serialization;
using System;
using System.IO;

namespace SS14.Shared
{
    [Serializable]
    public class GameStateDelta : INetSerializableType
    {
        private readonly MemoryStream deltaBytes = new MemoryStream();
        public uint FromSequence;
        public uint Sequence;

        public GameStateDelta(byte[] bytes)
        {
            deltaBytes = new MemoryStream(bytes);
            deltaBytes.Seek(0, SeekOrigin.Begin);
        }

        public GameStateDelta()
        {
        }

        public long Size
        {
            get { return deltaBytes.Length; }
        }

        public void Create(GameState fromState, GameState toState)
        {
            Sequence = toState.Sequence;
            FromSequence = fromState.Sequence;
            BinaryPatchUtility.Create(fromState.GetSerializedDataBuffer(), toState.GetSerializedDataBuffer(), deltaBytes);
        }

        public GameState Apply(GameState fromState)
        {
            if (fromState.Sequence != FromSequence)
                throw new Exception("Cannot apply GameStateDelta. Sequence incorrect.");
            byte[] fromBuffer = fromState.GetSerializedDataStream().ToArray();
            var toStream = new MemoryStream();
            BinaryPatchUtility.Apply(new MemoryStream(fromBuffer), () => new MemoryStream(deltaBytes.ToArray()),
                                     toStream);
            toStream.Seek(0, SeekOrigin.Begin);
            return (GameState) Serializer.Deserialize(toStream);
        }

        public static GameStateDelta ReadDelta(NetIncomingMessage message)
        {
            uint sequence = message.ReadUInt32();
            uint fromSequence = message.ReadUInt32();
            int length = message.ReadInt32();
            byte[] bytes;
            message.ReadBytes(length, out bytes);

            var delta = new GameStateDelta(bytes);
            delta.Sequence = sequence;
            delta.FromSequence = fromSequence;
            return delta;
        }

        public void WriteDelta(NetOutgoingMessage message)
        {
            message.Write(Sequence);
            message.Write(FromSequence);
            byte[] bytes = deltaBytes.ToArray();
            message.Write(bytes.Length);
            message.Write(bytes);
        }
    }
}
