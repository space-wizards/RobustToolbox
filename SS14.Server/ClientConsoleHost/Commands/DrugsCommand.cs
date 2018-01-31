using SS14.Server.Interfaces.ClientConsoleHost;
using SS14.Server.Interfaces.Player;
using SS14.Shared;
using SS14.Shared.Enums;
using SS14.Shared.IoC;

namespace SS14.Server.ClientConsoleHost.Commands
{
    public class DrugsCommand : IClientCommand
    {
        public string Command => "everyoneondrugs";
        public string Description => "Fuck no idea what this does honestly.";
        public string Help => "Nope! no clue!";

        public void Execute(IClientConsoleHost host, IPlayerSession player, params string[] args)
        {
            foreach (var targetPlayer in IoCManager.Resolve<IPlayerManager>().GetAllPlayers())
            {
                targetPlayer.AddPostProcessingEffect(PostProcessingEffectType.Acid, 60);
                host.SendConsoleReply(player.ConnectedClient, "Okay then.");
            }
        }
    }
}
