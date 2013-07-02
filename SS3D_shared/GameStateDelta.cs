using System;
using System.IO;
using NetSerializer;
using SS13_Shared.Serialization;

namespace SS13_Shared
{
    [Serializable]
    public class GameStateDelta : INetSerializableType
    {
        MemoryStream deltaBytes = new MemoryStream();
        public uint Sequence;

        public long Size
        {
            get { return deltaBytes.Length; }
        }

        public void Create(GameState fromState, GameState toState)
        {
            var fromStream = new MemoryStream();
            var toStream = new MemoryStream();
            Serializer.Serialize(fromStream, fromState);
            Serializer.Serialize(toStream, toState);
            BsDiffLib.BinaryPatchUtility.Create(fromStream.GetBuffer(), toStream.GetBuffer(), deltaBytes);
        }

        public GameState Apply(GameState fromState)
        {
            var fromStream = new MemoryStream();
            var toStream = new MemoryStream();
            Serializer.Serialize(fromStream, fromState);
            BsDiffLib.BinaryPatchUtility.Apply(fromStream, () => deltaBytes, toStream);
            return (GameState) Serializer.Deserialize(toStream);
        }

    }
}
