using System;
using Lidgren.Network;
using SS14.Shared.Interfaces.Network;

namespace SS14.Shared.Network.Messages
{
    public class MsgClGreet : NetMessage
    {
        #region REQUIRED
        public const NetMessages ID = NetMessages.ClientName;
        public const MsgGroups GROUP = MsgGroups.CORE;

        public static readonly string NAME = ID.ToString();
        public MsgClGreet(INetChannel channel) : base(NAME, GROUP, ID) { }
        #endregion

        public string PlyName;

        public override void ReadFromBuffer(NetIncomingMessage buffer)
        {
            PlyName = buffer.ReadString();
        }

        public override void WriteToBuffer(NetOutgoingMessage buffer)
        {
            throw new NotImplementedException();
        }
    }
}
