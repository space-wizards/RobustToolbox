using System;
using Lidgren.Network;

namespace SS14.Shared.Network.Messages
{
    public class MsgEntity : NetMessage
    {
        #region REQUIRED
        public static readonly NetMessages ID = NetMessages.EntityMessage;
        public static readonly MsgGroups GROUP = MsgGroups.CORE;

        public static readonly string NAME = ID.ToString();
        public static ProcessMessage _callback;
        public override ProcessMessage Callback
        {
            get => _callback;
            set => _callback = value;
        }
        public MsgEntity(NetChannel channel)
            : base(channel, NAME, GROUP, ID)
        { }
        #endregion

        public NetIncomingMessage Output;
        public override void ReadFromBuffer(NetIncomingMessage buffer)
        {
            Output = buffer;
        }

        public override void WriteToBuffer(NetOutgoingMessage buffer)
        {
            throw new NotImplementedException();
        }
    }
}
