using SS14.Server.Interfaces.Chat;
using SS14.Server.Interfaces.ClientConsoleHost;
using SS14.Shared.Console;
using SS14.Shared.Interfaces.Network;
using SS14.Shared.IoC;

namespace SS14.Server.ClientConsoleHost.Commands
{
    public class Test : IClientCommand
    {
        public string Command => "test";
        public string Description => "It's just a test bro.";
        public string Help => "This thing tests stuff. If you got this message that means it worked. Hooray!";

        public void Execute(IClientConsoleHost host, INetChannel client, params string[] args)
        {
            IoCManager.Resolve<IChatManager>().SendChatMessage(ChatChannel.Server, "Test worked!", "retarded shitcode", null); // That retarded shitcode is chat code fyi.
        }
    }
}
