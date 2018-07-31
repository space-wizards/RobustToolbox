using SS14.Server.Interfaces.Console;
using SS14.Server.Interfaces.GameObjects;
using SS14.Server.Interfaces.Player;
using SS14.Shared.Interfaces.GameObjects.Components;
using SS14.Shared.IoC;

namespace SS14.Server.Console.Commands
{
    public class SpawnCommand : IClientCommand
    {
        public string Command => "spawn";
        public string Description => "Spawns an entity with specific type at your feet.";
        public string Help => "spawn <entity type>";

        public void Execute(IConsoleShell shell, IPlayerSession player, string[] args)
        {
            var ent = IoCManager.Resolve<IServerEntityManager>();
            ent.ForceSpawnEntityAt(args[0], player.AttachedEntity.GetComponent<ITransformComponent>().LocalPosition);
        }
    }
}
