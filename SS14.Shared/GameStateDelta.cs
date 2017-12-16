using BsDiffLib;
using SS14.Shared.GameStates;
using SS14.Shared.Interfaces.Serialization;
using SS14.Shared.IoC;
using SS14.Shared.Serialization;
using System;
using System.IO;

namespace SS14.Shared
{
    [Serializable, NetSerializable]
    public class GameStateDelta
    {
        public readonly MemoryStream deltaBytes = new MemoryStream();
        public uint FromSequence { get; set; }
        public uint Sequence { get; set; }

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
            var serializer = IoCManager.Resolve<ISS14Serializer>();
            return serializer.Deserialize<GameState>(toStream);
        }
    }
}
