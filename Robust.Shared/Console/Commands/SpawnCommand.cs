using System.Globalization;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;

namespace Robust.Shared.Console.Commands;

public sealed class SpawnCommand : LocalizedCommands
{
    [Dependency] private readonly IEntityManager _entityManager = default!;

    public override string Command => "spawn";
    public override bool RequireServerOrSingleplayer => true;

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length is < 1 or > 3)
        {
            shell.WriteError("Incorrect number of arguments. " + Help);
        }

        var pAE = shell.Player?.AttachedEntity ?? EntityUid.Invalid;

        if (args.Length == 1 && pAE != EntityUid.Invalid)
        {
            _entityManager.SpawnEntity(args[0], _entityManager.GetComponent<TransformComponent>(pAE).Coordinates);
        }
        else if (args.Length == 2)
        {
            var uid = EntityUid.Parse(args[1]);
            _entityManager.SpawnEntity(args[0], _entityManager.GetComponent<TransformComponent>(uid).Coordinates);
        }
        else if (pAE != EntityUid.Invalid)
        {
            var coords = new MapCoordinates(
                float.Parse(args[1], CultureInfo.InvariantCulture),
                float.Parse(args[2], CultureInfo.InvariantCulture),
                _entityManager.GetComponent<TransformComponent>(pAE).MapID);

            _entityManager.SpawnEntity(args[0], coords);
        }
    }
}
