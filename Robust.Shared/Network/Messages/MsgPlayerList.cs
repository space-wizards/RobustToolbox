using System.Collections.Generic;
using Lidgren.Network;
using Robust.Shared.Enums;
using Robust.Shared.GameStates;

#nullable disable

namespace Robust.Shared.Network.Messages
{
    [NetMessage(MsgGroups.Core)]
    public class MsgPlayerList : NetMessage
    {
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
                    UserId = new NetUserId(buffer.ReadGuid()),
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
                buffer.Write(ply.UserId.UserId);
                buffer.Write(ply.Name);
                buffer.Write((byte) ply.Status);
                buffer.Write(ply.Ping);
            }
        }
    }
}
