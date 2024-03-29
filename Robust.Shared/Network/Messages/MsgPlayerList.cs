﻿using System.Collections.Generic;
using Lidgren.Network;
using Robust.Shared.Enums;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

#nullable disable

namespace Robust.Shared.Network.Messages
{
    public sealed class MsgPlayerList : NetMessage
    {
        public override MsgGroups MsgGroup => MsgGroups.Core;

        public byte PlyCount { get; set; }
        public List<SessionState> Plyrs { get; set; }

        public override void ReadFromBuffer(NetIncomingMessage buffer, IRobustSerializer serializer)
        {
            Plyrs = new List<SessionState>();
            PlyCount = buffer.ReadByte();
            for (var i = 0; i < PlyCount; i++)
            {
                var plyNfo = new SessionState
                {
                    UserId = new NetUserId(buffer.ReadGuid()),
                    Name = buffer.ReadString(),
                    Status = (SessionStatus)buffer.ReadByte(),
                    Ping = buffer.ReadInt16()
                };
                Plyrs.Add(plyNfo);
            }
        }

        public override void WriteToBuffer(NetOutgoingMessage buffer, IRobustSerializer serializer)
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
