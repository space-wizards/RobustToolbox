using Robust.Shared.Console;
using Robust.Shared.IoC;
using Robust.Shared.Network;

namespace Robust.Client.Console.Commands
{
    internal sealed class SendGarbageCommand : LocalizedCommands
    {
        [Dependency] private readonly IClientNetManager _netManager = default!;

        public override string Command => "sendgarbage";

        public override void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            // MsgStringTableEntries is registered as NetMessageAccept.Client so the server will immediately deny it.
            // And kick us.
            var msg = new MsgStringTableEntries();
            msg.Entries = new MsgStringTableEntries.Entry[0];
            _netManager.ClientSendMessage(msg);
        }
    }
}
