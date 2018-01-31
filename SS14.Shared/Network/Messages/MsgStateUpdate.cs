using Lidgren.Network;
using SS14.Shared.GameStates;
using SS14.Shared.Interfaces.Network;

namespace SS14.Shared.Network.Messages
{
    public class MsgStateUpdate : NetMessage
    {
        #region REQUIRED
        public static readonly MsgGroups GROUP = MsgGroups.Entity;
        public static readonly string NAME = nameof(MsgStateUpdate);
        public MsgStateUpdate(INetChannel channel) : base(NAME, GROUP) { }
        #endregion

        public GameStateDelta StateDelta { get; set; }

        public override void ReadFromBuffer(NetIncomingMessage buffer)
        {
            uint sequence = buffer.ReadUInt32();
            uint fromSequence = buffer.ReadUInt32();
            int length = buffer.ReadInt32();
            byte[] bytes;
            buffer.ReadBytes(length, out bytes);

            StateDelta = new GameStateDelta(bytes);
            StateDelta.Sequence = sequence;
            StateDelta.FromSequence = fromSequence;
        }

        public override void WriteToBuffer(NetOutgoingMessage buffer)
        {
            buffer.Write(StateDelta.Sequence);
            buffer.Write(StateDelta.FromSequence);
            buffer.Write(StateDelta.deltaBytes.Length);
            buffer.Write(StateDelta.deltaBytes);
        }
    }
}
