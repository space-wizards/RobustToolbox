using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Lidgren.Network;

namespace SS14.Shared.Network.Messages
{
    public class MsgMapReq : NetMessage
    {
        #region REQUIRED
        public static readonly NetMessages ID = NetMessages.RequestMap;
        public static readonly MsgGroups GROUP = MsgGroups.ENTITY;

        public static readonly string NAME = ID.ToString();
        public static ProcessMessage _callback;
        public override ProcessMessage Callback
        {
            get => _callback;
            set => _callback = value;
        }
        public MsgMapReq(NetChannel channel)
            : base(channel, NAME, GROUP, ID)
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
