using System.Globalization;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Maths;

namespace Robust.Shared.Console.Commands;

internal sealed class TeleportCommand : LocalizedCommands
{
    [Dependency] private readonly IMapManager _map = default!;

    public override string Command => "tp";
    public override bool RequireServerOrSingleplayer => true;

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        var transform = shell.Player?.AttachedEntityTransform;
        if (transform == null)
            return;

        if (args.Length < 2 || !float.TryParse(args[0], out var posX) || !float.TryParse(args[1], out var posY))
        {
            shell.WriteError(Help);
            return;
        }

        var position = new Vector2(posX, posY);

        transform.AttachToGridOrMap();

        MapId mapId;
        if (args.Length == 3 && int.TryParse(args[2], out var intMapId))
            mapId = new MapId(intMapId);
        else
            mapId = transform.MapID;

        if (!_map.MapExists(mapId))
        {
            shell.WriteError($"Map {mapId} doesn't exist!");
            return;
        }

        if (_map.TryFindGridAt(mapId, position, out var grid))
        {
            var gridPos = grid.WorldToLocal(position);

            transform.Coordinates = new EntityCoordinates(grid.Owner, gridPos);
        }
        else
        {
            var mapEnt = _map.GetMapEntityIdOrThrow(mapId);
            transform.WorldPosition = position;
            transform.AttachParent(mapEnt);
        }

        shell.WriteLine($"Teleported {shell.Player} to {mapId}:{posX},{posY}.");
    }
}

sealed class LocationCommand : LocalizedCommands
{
    [Dependency] private readonly IEntityManager _ent = default!;

    public override string Command => "loc";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        var pt = shell.Player?.AttachedEntityTransform;
        if (pt == null)
            return;

        var pos = pt.Coordinates;

        shell.WriteLine(
            $"MapID:{pos.GetMapId(_ent)} GridUid:{pos.GetGridUid(_ent)} X:{pos.X:N2} Y:{pos.Y:N2}");
    }
}

sealed class TpGridCommand : LocalizedCommands
{
    [Dependency] private readonly IEntityManager _ent = default!;
    [Dependency] private readonly IMapManager _map = default!;

    public override string Command => "tpgrid";
    public override bool RequireServerOrSingleplayer => true;

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length is < 3 or > 4)
        {
            shell.WriteError($"Usage: {Help}");
            return;
        }

        var gridId = EntityUid.Parse(args[0]);
        var xPos = float.Parse(args[1], CultureInfo.InvariantCulture);
        var yPos = float.Parse(args[2], CultureInfo.InvariantCulture);

        if (!_map.TryGetGrid(gridId, out var grid))
        {
            shell.WriteError($"No grid found with id {args[0]}");
            return;
        }

        var gridXform = _ent.GetComponent<TransformComponent>(grid.Owner);
        var mapId = args.Length == 4 ? new MapId(int.Parse(args[3])) : gridXform.MapID;

        gridXform.Coordinates = new EntityCoordinates(_map.GetMapEntityId(mapId), new Vector2(xPos, yPos));

        shell.WriteLine("Grid was teleported.");
    }
}
