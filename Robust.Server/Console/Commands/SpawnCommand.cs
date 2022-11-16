using System.Globalization;
using Robust.Server.GameObjects;
using Robust.Server.Player;
using Robust.Shared.Console;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;

namespace Robust.Server.Console.Commands
{
    public sealed class SpawnCommand : LocalizedCommands
    {
        [Dependency] private readonly IEntityManager _entityManager = default!;

        public override string Command => "spawn";

        public override void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            var player = shell.Player as IPlayerSession;

            if (args.Length is < 1 or > 3)
            {
                shell.WriteError("Incorrect number of arguments. " + Help);
            }

            var pAE = player?.AttachedEntity ?? EntityUid.Invalid;

            if (args.Length == 1 && player != null && pAE != EntityUid.Invalid)
            {
                _entityManager.SpawnEntity(args[0], _entityManager.GetComponent<TransformComponent>(pAE).Coordinates);
            }
            else if (args.Length == 2)
            {
                var uid = EntityUid.Parse(args[1]);
                _entityManager.SpawnEntity(args[0], _entityManager.GetComponent<TransformComponent>(uid).Coordinates);
            }
            else if (player != null && pAE != EntityUid.Invalid)
            {
                var coords = new MapCoordinates(
                    float.Parse(args[1], CultureInfo.InvariantCulture),
                    float.Parse(args[2], CultureInfo.InvariantCulture),
                    _entityManager.GetComponent<TransformComponent>(pAE).MapID);

                _entityManager.SpawnEntity(args[0], coords);
            }
        }
    }
}
