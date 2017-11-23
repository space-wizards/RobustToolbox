using System.Collections.Generic;
using Lidgren.Network;
using SS14.Shared.Interfaces.Network;
using SS14.Shared.Players;

namespace SS14.Shared.Network.Messages
{
    public class MsgPlayerList : NetMessage
    {
        #region REQUIRED

        public static readonly string NAME = "PlayerList";
        public static readonly MsgGroups GROUP = MsgGroups.Core;
        public static readonly NetMessages ID = NetMessages.PlayerList;

        public MsgPlayerList(INetChannel channel)
            : base(NAME, GROUP, ID)
        {
        }

        #endregion

        public byte PlyCount { get; set; }
        public List<PlyInfo> Plyrs { get; set; }

        public class PlyInfo
        {
            public PlayerIndex Index { get; set; }
            public long Uuid { get; set; }
            public string Name { get; set; }
            public byte Status { get; set; }
            public short Ping { get; set; }
        }

        public override void ReadFromBuffer(NetIncomingMessage buffer)
        {
            Plyrs = new List<PlyInfo>();
            PlyCount = buffer.ReadByte();
            for (var i = 0; i < PlyCount; i++)
            {
                var plyNfo = new PlyInfo();
                plyNfo.Index = new PlayerIndex(buffer.ReadInt32());
                plyNfo.Uuid = buffer.ReadInt64();
                plyNfo.Name = buffer.ReadString();
                plyNfo.Status = buffer.ReadByte();
                plyNfo.Ping = buffer.ReadInt16();
                Plyrs.Add(plyNfo);
            }
        }

        public override void WriteToBuffer(NetOutgoingMessage buffer)
        {
            buffer.Write(PlyCount);

            foreach (var ply in Plyrs)
            {
                buffer.Write(ply.Index);
                buffer.Write(ply.Uuid);
                buffer.Write(ply.Name);
                buffer.Write(ply.Status);
                buffer.Write(ply.Ping);
            }
        }
    }
}
