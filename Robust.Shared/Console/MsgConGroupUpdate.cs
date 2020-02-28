using System.Collections.Generic;
using Lidgren.Network;
using Robust.Shared.Interfaces.Network;
using Robust.Shared.Network;

namespace Robust.Shared.Console
{
    /// <summary>
    /// Sent from server to client. Contains the console group of the client,
    /// which includes a list of commands they can use.
    /// </summary>
    class MsgConGroupUpdate : NetMessage
    {
        public const MsgGroups Group = MsgGroups.Command;
        public const string Name = nameof(MsgConGroupUpdate);

        public MsgConGroupUpdate(INetChannel channel) : base(Name, Group)
        {

        }

        //Client console group data
        public ConGroup ClientConGroup = new ConGroup();

        public override void ReadFromBuffer(NetIncomingMessage buffer)
        {
            ClientConGroup.Index = buffer.ReadInt32();
            ClientConGroup.Name = buffer.ReadString();
            ClientConGroup.CanViewVar = buffer.ReadBoolean();
            ClientConGroup.CanAdminPlace = buffer.ReadBoolean();

            int numCommands = buffer.ReadInt32();
            ClientConGroup.Commands = new List<string>(numCommands);
            for (int i = 0; i < numCommands; i++)
            {
                ClientConGroup.Commands.Add(buffer.ReadString());
            }
        }

        public override void WriteToBuffer(NetOutgoingMessage buffer)
        {
            buffer.Write(ClientConGroup.Index);
            buffer.Write(ClientConGroup.Name);
            buffer.Write(ClientConGroup.CanViewVar);
            buffer.Write(ClientConGroup.CanAdminPlace);

            buffer.Write(ClientConGroup.Commands.Count);
            foreach (var command in ClientConGroup.Commands)
            {
                buffer.Write(command);
            }
        }
    }
}
