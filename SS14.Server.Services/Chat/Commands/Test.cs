using SS14.Server.Interfaces;
using SS14.Server.Interfaces.Chat;
using SS14.Shared;
using SS14.Shared.IoC;

namespace SS14.Server.Services.Chat.Commands
{
    public class Test : ChatCommand
    {
        public override string Command
        {
            get
            {
                return "test";
            }
        }

        public override string Description
        {
            get
            {
                return "It's just a test bro.";
            }
        }

        public override string Help
        {
            get
            {
                return "This thing tests stuff. If you got this message that means it worked. Hooray!";
            }
        }

        public override void Execute(IClient client, params string[] args)
        {
            IoCManager.Resolve<IChatManager>().SendChatMessage(ChatChannel.Server, "Test worked!", "retarded shitcode", null); // That retarded shitcode is chat code fyi.
        }
    }
}