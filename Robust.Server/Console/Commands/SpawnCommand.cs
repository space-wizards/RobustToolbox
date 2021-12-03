using Robust.Server.GameObjects;
using Robust.Server.Player;
using Robust.Shared.Console;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;

namespace Robust.Server.Console.Commands
{
    public class SpawnCommand : IConsoleCommand
    {
        public string Command => "spawn";
        public string Description => "Spawns an entity with specific type.";
        public string Help => "spawn <prototype> OR spawn <prototype> <relative entity ID> OR spawn <prototype> <x> <y>";

        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            var player = shell.Player as IPlayerSession;
            var ent = IoCManager.Resolve<IServerEntityManager>();
            if (args.Length is < 1 or > 3)
            {
                shell.WriteError("Incorrect number of arguments. " + Help);
            }

            if (args.Length == 1 && player?.AttachedEntity != null)
            {
                ent.SpawnEntity(args[0], IoCManager.Resolve<IEntityManager>().GetComponent<TransformComponent>(player.AttachedEntity.Uid).Coordinates);
            }
            else if (args.Length == 2)
            {
                IEntity tempQualifier = ent.GetEntity(EntityUid.Parse(args[1]));
                ent.SpawnEntity(args[0], IoCManager.Resolve<IEntityManager>().GetComponent<TransformComponent>(tempQualifier.Uid).Coordinates);
            }
            else if (player?.AttachedEntity != null)
            {
                var coords = new MapCoordinates(float.Parse(args[1]),
                    float.Parse(args[2]), IoCManager.Resolve<IEntityManager>().GetComponent<TransformComponent>(player.AttachedEntity.Uid).MapID);
                ent.SpawnEntity(args[0], coords);
            }
        }
    }
}
