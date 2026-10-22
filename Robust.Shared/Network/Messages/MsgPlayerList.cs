using System.Collections.Generic;
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

        public List<SessionState> Plyrs { get; set; }

        public override void ReadFromBuffer(NetIncomingMessage buffer, IRobustSerializer serializer)
        {
            var playerCount = buffer.ReadInt32();
            Plyrs = new List<SessionState>(playerCount);
            for (var i = 0; i < playerCount; i++)
            {
                var plyNfo = new SessionState
                {
                    UserId = new NetUserId(buffer.ReadGuid()),
                    Name = buffer.ReadString(),
                    Status = (SessionStatus)buffer.ReadByte(),
                };
                Plyrs.Add(plyNfo);
            }
        }

        public override void WriteToBuffer(NetOutgoingMessage buffer, IRobustSerializer serializer)
        {
            buffer.Write(Plyrs.Count);

            foreach (var ply in Plyrs)
            {
                buffer.Write(ply.UserId.UserId);
                buffer.Write(ply.Name);
                buffer.Write((byte) ply.Status);
            }
        }
    }
}
