using SS14.Client.Interfaces.Player;
using SS14.Shared.IoC;

namespace SS14.Client.State.States
{
    public class Lobby : State
    {
        public override void Shutdown()
        {
            // throw new System.NotImplementedException();
        }

        public override void Startup()
        {
            IoCManager.Resolve<IClientConsole>().ProcessCommand("joingame");
        }
    }
}
