using SS14.Server.Interfaces.ClientConsoleHost;
using SS14.Server.Interfaces.Player;
using SS14.Shared.IoC;
using SS14.Shared;
using SS14.Shared.Interfaces.Network;

namespace SS14.Server.ClientConsoleHost.Commands
{
    public class DrugsCommand : IClientCommand
    {
        public string Command => "everyoneondrugs";
        public string Description => "Fuck no idea what this does honestly.";
        public string Help => "Nope! no clue!";

        public void Execute(IClientConsoleHost host, INetChannel client, params string[] args)
        {
            foreach (IPlayerSession playerfordrugs in IoCManager.Resolve<IPlayerManager>().GetAllPlayers())
            {
                playerfordrugs.AddPostProcessingEffect(PostProcessingEffectType.Acid, 60);
                host.SendConsoleReply("Okay then.", client);
            }
        }
    }
}
