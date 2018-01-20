using SS14.Client.Console;
using SS14.Client.Interfaces.Player;
using SS14.Shared.IoC;

namespace SS14.Client.State.States
{
    public class Lobby : State
    {
        public override void Shutdown()
        {
            // Nothing but it's abstract so we need to override it.
            // Yay.
        }

        public override void Startup()
        {
            IoCManager.Resolve<IClientConsole>().ProcessCommand("joingame");
        }
    }
}
