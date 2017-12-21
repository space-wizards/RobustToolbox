using System;
using Lidgren.Network;
using SS14.Shared.Interfaces.Network;

namespace SS14.Shared.Network.Messages
{
    public class MsgAdmin : NetMessage
    {
        #region REQUIRED
        public const NetMessages ID = NetMessages.RequestEntityDeletion;
        public const MsgGroups GROUP = MsgGroups.Command;
        public static readonly string NAME = ID.ToString();
        
        public MsgAdmin(INetChannel channel) : base(NAME, GROUP, ID) { }
        #endregion

        public int EntityId { get; set; }

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
