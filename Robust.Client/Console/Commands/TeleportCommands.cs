using Robust.Shared.GameObjects;
using Robust.Shared.IoC;

namespace Robust.Shared.Console.Commands;

public sealed class TeleportToCommand : LocalizedCommands
{
    [Dependency] private readonly IEntityManager _entities = default!;

    public override string Command => "tpto";
    public override bool RequireServerOrSingleplayer => true;

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length == 0)
            return;

        var target = args[^1];
        if (!EntityUid.TryParse(target, out var uid))
            return;

        if (!_entities.TryGetComponent(uid, out TransformComponent? targetTransform))
            return;

        var targetCoords = targetTransform.Coordinates;

        if (args.Length == 1)
        {
            var playerTransform = shell.Player?.AttachedEntityTransform;
            if (playerTransform == null)
                return;

            playerTransform.Coordinates = targetCoords;
            playerTransform.AttachToGridOrMap();
        }
        else
        {
            foreach (var victim in args)
            {
                if (victim == target)
                    continue;

                if (!EntityUid.TryParse(victim, out var victimUid))
                    continue;

                if (!_entities.TryGetComponent(victimUid, out TransformComponent? victimTransform))
                    continue;

                victimTransform.Coordinates = targetCoords;
                victimTransform.AttachToGridOrMap();
            }
        }
    }
}
