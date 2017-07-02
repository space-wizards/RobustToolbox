using SS14.Server.Interfaces;
using SS14.Server.Interfaces.Chat;
using SS14.Shared;
using SS14.Shared.IoC;
using SS14.Shared.Network;

namespace SS14.Server.Chat.Commands
{
    public class Test : IChatCommand
    {
        public string Command => "test";
        public string Description => "It's just a test bro.";
        public string Help => "This thing tests stuff. If you got this message that means it worked. Hooray!";

        public void Execute(IChatManager manager, NetChannel client, params string[] args)
        {
            IoCManager.Resolve<IChatManager>().SendChatMessage(ChatChannel.Server, "Test worked!", "retarded shitcode", null); // That retarded shitcode is chat code fyi.
        }
    }
}
