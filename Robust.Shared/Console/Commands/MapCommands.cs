using System.Globalization;
using System.Linq;
using System.Text;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;

namespace Robust.Shared.Console.Commands;

sealed class AddMapCommand : LocalizedCommands
{
    [Dependency] private readonly IMapManager _map = default!;

    public override string Command => "addmap";
    public override bool RequireServerOrSingleplayer => true;

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length < 1)
            return;

        var mapId = new MapId(int.Parse(args[0]));

        if (!_map.MapExists(mapId))
        {
            _map.CreateMap(mapId);
            if (args.Length >= 2 && args[1] == "false")
            {
                _map.AddUninitializedMap(mapId);
            }

            shell.WriteLine($"Map with ID {mapId} created.");
            return;
        }

        shell.WriteError($"Map with ID {mapId} already exists!");
    }
}

sealed class RemoveMapCommand : LocalizedCommands
{
    [Dependency] private readonly IMapManager _map = default!;

    public override string Command => "rmmap";
    public override bool RequireServerOrSingleplayer => true;

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length != 1)
        {
            shell.WriteError("Wrong number of args.");
            return;
        }

        var mapId = new MapId(int.Parse(args[0]));

        if (!_map.MapExists(mapId))
        {
            shell.WriteError($"Map {mapId.Value} does not exist.");
            return;
        }

        _map.DeleteMap(mapId);
        shell.WriteLine($"Map {mapId.Value} was removed.");
    }
}

sealed class RemoveGridCommand : LocalizedCommands
{
    [Dependency] private readonly IEntityManager _entManager = default!;
    [Dependency] private readonly IMapManager _map = default!;

    public override string Command => "rmgrid";
    public override bool RequireServerOrSingleplayer => true;

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length != 1)
        {
            shell.WriteError("Wrong number of args.");
            return;
        }

        var gridIdNet = NetEntity.Parse(args[0]);

        if (!_entManager.TryGetEntity(gridIdNet, out var gridId) || !_entManager.HasComponent<MapGridComponent>(gridId))
        {
            shell.WriteError($"Grid {gridId} does not exist.");
            return;
        }

        _map.DeleteGrid(gridId.Value);
        shell.WriteLine($"Grid {gridId} was removed.");
    }
}

internal sealed class RunMapInitCommand : LocalizedCommands
{
    [Dependency] private readonly IMapManager _map = default!;

    public override string Command => "mapinit";
    public override bool RequireServerOrSingleplayer => true;

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length != 1)
        {
            shell.WriteError("Wrong number of args.");
            return;
        }

        var arg = args[0];
        var mapId = new MapId(int.Parse(arg, CultureInfo.InvariantCulture));

        if (!_map.MapExists(mapId))
        {
            shell.WriteError("Map does not exist!");
            return;
        }

        if (_map.IsMapInitialized(mapId))
        {
            shell.WriteError("Map is already initialized!");
            return;
        }

        _map.DoMapInitialize(mapId);
    }
}

internal sealed class ListMapsCommand : LocalizedCommands
{
    [Dependency] private readonly IEntityManager _entManager = default!;
    [Dependency] private readonly IMapManager _map = default!;

    public override string Command => "lsmap";

    // PVS prevents the player from knowing about all maps.
    public override bool RequireServerOrSingleplayer => true;

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        var msg = new StringBuilder();

        foreach (var mapId in _map.GetAllMapIds().OrderBy(id => id.Value))
        {
            var mapUid = _map.GetMapEntityId(mapId);

            msg.AppendFormat("{0}: {1}, init: {2}, paused: {3}, nent: {4}, grids: {5}\n",
                mapId, _entManager.GetComponent<MetaDataComponent>(mapUid).EntityName,
                _map.IsMapInitialized(mapId),
                _map.IsMapPaused(mapId),
                _entManager.GetNetEntity(_map.GetMapEntityId(mapId)),
                string.Join(",", _map.GetAllGrids(mapId).Select(grid => grid.Owner)));
        }

        shell.WriteLine(msg.ToString());
    }
}

internal sealed class ListGridsCommand : LocalizedCommands
{
    [Dependency] private readonly IEntityManager _ent = default!;

    public override string Command => "lsgrid";

    // PVS prevents the player from knowing about all maps.
    public override bool RequireServerOrSingleplayer => true;

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        var msg = new StringBuilder();
        var xformSystem = _ent.System<SharedTransformSystem>();
        var xformQuery = _ent.GetEntityQuery<TransformComponent>();
        var grids = _ent.AllComponentsList<MapGridComponent>();
        grids.Sort((x, y) => x.Uid.CompareTo(y.Uid));

        foreach (var (uid, grid) in grids)
        {
            var xform = xformQuery.GetComponent(uid);
            var worldPos = xformSystem.GetWorldPosition(xform);

            msg.AppendFormat("{0}: map: {1}, ent: {2}, pos: {3:0.0},{4:0.0} \n",
                uid, xform.MapID, uid, worldPos.X, worldPos.Y);
        }

        shell.WriteLine(msg.ToString());
    }
}
