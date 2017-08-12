using System;
using Lidgren.Network;
using SS14.Shared.Interfaces.Network;

namespace SS14.Shared.Network.Messages
{
    public class MsgStateAck : NetMessage
    {
        #region REQUIRED
        public static readonly NetMessages ID = NetMessages.StateAck;
        public static readonly MsgGroups GROUP = MsgGroups.Entity;

        public static readonly string NAME = ID.ToString();
        public MsgStateAck(INetChannel channel)
            : base(NAME, GROUP, ID)
        { }
        #endregion

        public uint Sequence { get; set; }

        public override void ReadFromBuffer(NetIncomingMessage buffer)
        {
            Sequence = buffer.ReadUInt32();
        }

        public override void WriteToBuffer(NetOutgoingMessage buffer)
        {
            throw new NotImplementedException();
        }
    }
}
