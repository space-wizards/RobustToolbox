using System.Collections.Generic;
using Lidgren.Network;
using SS14.Shared.Interfaces.Network;

namespace SS14.Shared.Network.Messages
{
    public class MsgPlayerList : NetMessage
    {
        #region REQUIRED

        public static readonly string NAME = "PlayerList";
        public static readonly MsgGroups GROUP = MsgGroups.CORE;
        public static readonly NetMessages ID = NetMessages.PlayerList;

        public MsgPlayerList(INetChannel channel)
            : base(NAME, GROUP, ID)
        {
        }

        #endregion

        public byte PlyCount;
        public List<PlyInfo> plyrs;

        public class PlyInfo
        {
            public string name;
            public byte status;
            public float ping;
        }

        public override void ReadFromBuffer(NetIncomingMessage buffer)
        {
            plyrs = new List<PlyInfo>();
            PlyCount = buffer.ReadByte();
            for (var i = 0; i < PlyCount; i++)
            {
                var plyNfo = new PlyInfo();
                plyNfo.name = buffer.ReadString();
                plyNfo.status = buffer.ReadByte();
                plyNfo.ping = buffer.ReadFloat();
                plyrs.Add(plyNfo);
            }
        }

        public override void WriteToBuffer(NetOutgoingMessage buffer)
        {
            buffer.Write(PlyCount);

            foreach (var ply in plyrs)
            {
                buffer.Write(ply.name);
                buffer.Write(ply.status);
                buffer.Write(ply.ping);
            }
        }
    }
}
