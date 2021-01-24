using Robust.Shared.Interfaces.Network;
using Robust.Shared.IoC;
using Robust.Shared.Network;

namespace Robust.Client.Console.Commands
{
    internal sealed class SendGarbageCommand : IClientCommand
    {
        public string Command => "sendgarbage";
        public string Description => "Sends garbage to the server.";
        public string Help => "The server will reply with 'no u'";

        public bool Execute(IClientConsoleShell shell, string argStr, string[] args)
        {
            // MsgStringTableEntries is registered as NetMessageAccept.Client so the server will immediately deny it.
            // And kick us.
            var net = IoCManager.Resolve<IClientNetManager>();
            var msg = net.CreateNetMessage<MsgStringTableEntries>();
            msg.Entries = new MsgStringTableEntries.Entry[0];
            net.ClientSendMessage(msg);

            return false;
        }
    }
}
