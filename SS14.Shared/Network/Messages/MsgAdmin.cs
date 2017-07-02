using System;
using Lidgren.Network;

namespace SS14.Shared.Network.Messages
{
    public class MsgAdmin : NetMessage
    {
        #region REQUIRED
        public static readonly NetMessages ID = NetMessages.RequestEntityDeletion;
        public static readonly MsgGroups GROUP = MsgGroups.ENTITY;

        public static readonly string NAME = ID.ToString();
        public static ProcessMessage _callback;
        public override ProcessMessage Callback
        {
            get => _callback;
            set => _callback = value;
        }
        public MsgAdmin(NetChannel channel)
            : base(channel, NAME, GROUP, ID)
        { }
        #endregion

        public int EntityId;
        public override void ReadFromBuffer(NetIncomingMessage buffer)
        {
            EntityId = buffer.ReadInt32();
        }

        public override void WriteToBuffer(NetOutgoingMessage buffer)
        {
            throw new NotImplementedException();
        }
    }
}
