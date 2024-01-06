using System;
using System.Collections.Generic;
using Lidgren.Network;
using Robust.Shared.Serialization;

#nullable disable

namespace Robust.Shared.Network.Messages
{
    public sealed class MsgConCmdReg : NetMessage
    {
        public override MsgGroups MsgGroup => MsgGroups.String;

        public List<Command> Commands { get; set; }

        public sealed class Command
        {
            public string Name { get; set; }
            public string Description { get; set; }
            public string Help { get; set; }
        }

        public override void ReadFromBuffer(NetIncomingMessage buffer, IRobustSerializer serializer)
        {
            var cmdCount = buffer.ReadUInt16();
            Commands = new (cmdCount);
            for (var i = 0; i < cmdCount; i++)
            {
                Commands.Add(new Command()
                {
                    Name = buffer.ReadString(),
                    Description = buffer.ReadString(),
                    Help = buffer.ReadString()
                });
            }
        }

        public override void WriteToBuffer(NetOutgoingMessage buffer, IRobustSerializer serializer)
        {
            if (Commands == null) // client leaves comands as null to request from server
            {
                buffer.Write((UInt16)0);
                return;
            }

            buffer.Write((UInt16)Commands.Count);
            foreach (var command in Commands)
            {
                buffer.Write(command.Name);
                buffer.Write(command.Description);
                buffer.Write(command.Help);
            }
        }
    }
}
