using System;
using Lidgren.Network;

namespace SS14.Shared.Network.Messages
{
    public class MsgStateAck : NetMessage
    {
        #region REQUIRED
        public static readonly NetMessages ID = NetMessages.StateAck;
        public static readonly MsgGroups GROUP = MsgGroups.CORE;

        public static readonly string NAME = ID.ToString();
        public static ProcessMessage _callback;
        public override ProcessMessage Callback
        {
            get => _callback;
            set => _callback = value;
        }
        public MsgStateAck(NetChannel channel)
            : base(channel, NAME, GROUP, ID)
        { }
        #endregion

        public uint Sequence;
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
