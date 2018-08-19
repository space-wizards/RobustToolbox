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
                var plyNfo = new PlayerState
                {
                    SessionId = new NetSessionId(buffer.ReadString()),
                    Name = buffer.ReadString(),
                    Status = (SessionStatus)buffer.ReadByte(),
                    Ping = buffer.ReadInt16()
                };
                Plyrs.Add(plyNfo);
            }
        }

        public override void WriteToBuffer(NetOutgoingMessage buffer)
        {
            buffer.Write(PlyCount);

            foreach (var ply in Plyrs)
            {
                buffer.Write(ply.SessionId.Username);
                buffer.Write(ply.Name);
                buffer.Write((byte) ply.Status);
                buffer.Write(ply.Ping);
            }
        }
    }
}
