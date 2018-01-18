using System.Collections.Generic;
using Lidgren.Network;
using SS14.Shared.Enums;
using SS14.Shared.GameStates;
using SS14.Shared.Interfaces.Network;
using SS14.Shared.Players;

namespace SS14.Shared.Network.Messages
{
    public class MsgPlayerList : NetMessage
    {
        #region REQUIRED
        public static readonly MsgGroups GROUP = MsgGroups.Core;
        public static readonly string NAME = nameof(MsgPlayerList);
        public MsgPlayerList(INetChannel channel) : base(NAME, GROUP) { }
        #endregion

        public byte PlyCount { get; set; }
        public List<PlayerState> Plyrs { get; set; }
        
        public override void ReadFromBuffer(NetIncomingMessage buffer)
        {
            Plyrs = new List<PlayerState>();
            PlyCount = buffer.ReadByte();
            for (var i = 0; i < PlyCount; i++)
            {
                var plyNfo = new PlayerState();
                plyNfo.Index = new PlayerIndex(buffer.ReadInt32());
                plyNfo.Uuid = buffer.ReadInt64();
                plyNfo.Name = buffer.ReadString();
                plyNfo.Status = (SessionStatus) buffer.ReadByte();
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
                buffer.Write((byte) ply.Status);
                buffer.Write(ply.Ping);
            }
        }
    }
}
