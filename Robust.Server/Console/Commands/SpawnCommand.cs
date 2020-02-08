using Robust.Server.Interfaces.Console;
using Robust.Server.Interfaces.GameObjects;
using Robust.Server.Interfaces.Player;
using Robust.Shared.Interfaces.GameObjects.Components;
using Robust.Shared.IoC;

namespace Robust.Server.Console.Commands
{
    public class SpawnCommand : IClientCommand
    {
        public string Command => "spawn";
        public string Description => "Spawns an entity with specific type at your feet.";
        public string Help => "spawn <entity type>";

        public void Execute(IConsoleShell shell, IPlayerSession player, string[] args)
        {
            var ent = IoCManager.Resolve<IServerEntityManager>();
            if (player?.AttachedEntity != null)
            {
                ent.SpawnEntity(args[0], player.AttachedEntity.Transform.GridPosition);
            }
        }
    }
}
