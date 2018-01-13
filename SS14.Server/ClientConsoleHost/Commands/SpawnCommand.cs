using SS14.Server.Interfaces.ClientConsoleHost;
using SS14.Server.Interfaces.GameObjects;
using SS14.Server.Interfaces.Player;
using SS14.Shared.IoC;

namespace SS14.Server.ClientConsoleHost.Commands
{
    public class SpawnCommand : IClientCommand
    {
        public string Command => "spawn";
        public string Description => "Spawns an entity with specific type at your feet.";
        public string Help => "Usage: spawn <entity type>";

        public void Execute(IClientConsoleHost host, IPlayerSession player, params string[] args)
        {
            var ent = IoCManager.Resolve<IServerEntityManager>();
            ent.ForceSpawnEntityAt(args[0], player.attachedEntity.GetComponent<IServerTransformComponent>().LocalPosition);
        }
    }
}
