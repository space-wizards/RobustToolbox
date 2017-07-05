using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Lidgren.Network;
using SS14.Shared.Interfaces.Network;

namespace SS14.Shared.Network.Messages
{
    public class MsgConCmdReg : NetMessage
    {
        #region REQUIRED
        public const NetMessages ID = NetMessages.ConsoleCommandRegister;
        public const MsgGroups GROUP = MsgGroups.STRING;

        public static readonly string NAME = ID.ToString();
        public MsgConCmdReg(INetChannel channel) : base(NAME, GROUP, ID) { }
        #endregion

        public Command[] Commands;
        public class Command
        {
            public string Name;
            public string Description;
            public string Help;
        }

        public override void ReadFromBuffer(NetIncomingMessage buffer)
        {
            for (ushort i = buffer.ReadUInt16(); i > 0; i--)
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
