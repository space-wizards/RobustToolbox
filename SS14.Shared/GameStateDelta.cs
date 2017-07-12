using Lidgren.Network;
using NetSerializer;
using SS14.Shared.Bsdiff;
using SS14.Shared.GameStates;
using SS14.Shared.Serialization;
using System;
using System.IO;

namespace SS14.Shared
{
    [Serializable]
    public class GameStateDelta : INetSerializableType
    {
        private byte[] deltaBytes;
        public uint FromSequence;
        public uint Sequence;

        public GameStateDelta(byte[] bytes)
        {
            deltaBytes = bytes;
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
            deltaBytes = Bsdiff.Bsdiff.GenerateBzip2Diff(fromState.GetSerializedDataBuffer(), toState.GetSerializedDataBuffer());
        }

        public GameState Apply(GameState fromState)
        {
            if (fromState.Sequence != FromSequence)
                throw new Exception("Cannot apply GameStateDelta. Sequence incorrect.");
            byte[] fromBuffer = fromState.GetSerializedDataStream().ToArray();
            var newBytes = Bsdiff.Bsdiff.ApplyBzip2Patch(fromBuffer, deltaBytes);
            return (GameState) Serializer.Deserialize(new MemoryStream(newBytes));
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
            message.Write(deltaBytes.Length);
            message.Write(deltaBytes);
        }
    }
}
