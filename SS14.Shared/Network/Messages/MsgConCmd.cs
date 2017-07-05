using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Lidgren.Network;
using SS14.Shared.Interfaces.Network;

namespace SS14.Shared.Network.Messages
{
    public class MsgConCmd : NetMessage
    {
        #region REQUIRED
        public static readonly NetMessages ID = NetMessages.ConsoleCommand;
        public static readonly MsgGroups GROUP = MsgGroups.CORE;

        public static readonly string NAME = ID.ToString();
        public MsgConCmd(INetChannel channel)
            : base(NAME, GROUP, ID)
        { }
        #endregion

        public string text;

        public override void ReadFromBuffer(NetIncomingMessage buffer)
        {
            text = buffer.ReadString();
        }

        public override void WriteToBuffer(NetOutgoingMessage buffer)
        {
            buffer.Write(text);
        }
    }
}
