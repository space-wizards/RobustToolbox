using SS14.Server.GameObjects;
using SS14.Server.Interfaces.ClientConsoleHost;
using SS14.Server.Interfaces.Player;
using SS14.Shared;
using SS14.Shared.Enums;
using SS14.Shared.IoC;
using SS14.Shared.Maths;

namespace SS14.Server.ClientConsoleHost.Commands
{
    public class DrugsCommand : IClientCommand
    {
        public string Command => "everyoneondrugs";
        public string Description => "Fuck no idea what this does honestly.";
        public string Help => "Nope! no clue!";

        public void Execute(IClientConsoleHost host, IPlayerSession player, params string[] args)
        {
            var random = new System.Random();
            foreach (var targetPlayer in IoCManager.Resolve<IPlayerManager>().GetAllPlayers())
            {
                if (targetPlayer.AttachedEntity == null
                || !targetPlayer.AttachedEntity.TryGetComponent<SpriteComponent>(out var comp))
                {
                    continue;
                }

                var r = (float)random.NextDouble();
                var g = (float)random.NextDouble();
                var b = (float)random.NextDouble();
                var col = new Color(r, g, b);
                comp.Color = col;
            }
        }
    }
}
