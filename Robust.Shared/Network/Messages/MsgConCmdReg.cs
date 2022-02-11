using System;
using Lidgren.Network;

#nullable disable

namespace Robust.Shared.Network.Messages
{
    public sealed class MsgConCmdReg : NetMessage
    {
        public override MsgGroups MsgGroup => MsgGroups.String;

        public Command[] Commands { get; set; }

        public sealed class Command
        {
            public string Name { get; set; }
            public string Description { get; set; }
            public string Help { get; set; }
        }

        public override void ReadFromBuffer(NetIncomingMessage buffer)
        {
            var cmdCount = buffer.ReadUInt16();
            Commands = new Command[cmdCount];
            for (var i = 0; i < cmdCount; i++)
            {
                Commands[i] = new Command()
                {
                    Name = buffer.ReadString(),
                    Description = buffer.ReadString(),
                    Help = buffer.ReadString()
                };
            }
        }

        public override void WriteToBuffer(NetOutgoingMessage buffer)
        {
            if(Commands == null) // client leaves comands as null to request from server
                Commands = new Command[0];

            buffer.Write((UInt16)Commands.Length);
            foreach (var command in Commands)
            {
                buffer.Write(command.Name);
                buffer.Write(command.Description);
                buffer.Write(command.Help);
            }
        }
    }
}
