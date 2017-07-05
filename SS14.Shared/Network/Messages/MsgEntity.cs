using System;
using Lidgren.Network;
using SS14.Shared.Interfaces.Network;

namespace SS14.Shared.Network.Messages
{
    public class MsgEntity : NetMessage
    {
        #region REQUIRED
        public static readonly NetMessages ID = NetMessages.EntityMessage;
        public static readonly MsgGroups GROUP = MsgGroups.CORE;

        public static readonly string NAME = ID.ToString();
        public MsgEntity(INetChannel channel)
            : base(NAME, GROUP, ID)
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
