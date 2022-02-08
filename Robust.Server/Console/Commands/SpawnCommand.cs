using System.Globalization;
using Robust.Server.GameObjects;
using Robust.Server.Player;
using Robust.Shared.Console;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;

namespace Robust.Server.Console.Commands
{
    public sealed class SpawnCommand : IConsoleCommand
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

            var pAE = player?.AttachedEntity ?? EntityUid.Invalid;

            if (args.Length == 1 && player != null && pAE != EntityUid.Invalid)
            {
                ent.SpawnEntity(args[0], ent.GetComponent<TransformComponent>(pAE).Coordinates);
            }
            else if (args.Length == 2)
            {
                var uid = EntityUid.Parse(args[1]);
                ent.SpawnEntity(args[0], ent.GetComponent<TransformComponent>(uid).Coordinates);
            }
            else if (player != null && pAE != EntityUid.Invalid)
            {
                var coords = new MapCoordinates(
                    float.Parse(args[1], CultureInfo.InvariantCulture),
                    float.Parse(args[2], CultureInfo.InvariantCulture),
                    ent.GetComponent<TransformComponent>(pAE).MapID);

                ent.SpawnEntity(args[0], coords);
            }
        }
    }
}
