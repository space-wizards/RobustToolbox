using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Lidgren.Network;
using SS14.Shared.Interfaces.Network;

namespace SS14.Shared.Network.Messages
{
    public class MsgMapReq : NetMessage
    {
        #region REQUIRED
        public static readonly NetMessages ID = NetMessages.RequestMap;
        public static readonly MsgGroups GROUP = MsgGroups.Entity;

        public static readonly string NAME = ID.ToString();
        public MsgMapReq(INetChannel channel)
            : base(NAME, GROUP, ID)
        { }
        #endregion
        
        public override void ReadFromBuffer(NetIncomingMessage buffer)
        {
        }

        public override void WriteToBuffer(NetOutgoingMessage buffer)
        {
        }
    }
}
