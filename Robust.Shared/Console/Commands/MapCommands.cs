using System.Globalization;
using System.Linq;
using System.Text;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;

namespace Robust.Shared.Console.Commands;

sealed class AddMapCommand : LocalizedEntityCommands
{
    [Dependency] private readonly SharedMapSystem _mapSystem = default!;

    public override string Command => "addmap";
    public override bool RequireServerOrSingleplayer => true;

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length < 1)
            return;

        var mapId = new MapId(int.Parse(args[0]));

        if (!_mapSystem.MapExists(mapId))
        {
            var init = args.Length < 2 || !bool.Parse(args[1]);
            EntityManager.System<SharedMapSystem>().CreateMap(mapId, runMapInit: init);

            shell.WriteLine($"Map with ID {mapId} created.");
            return;
        }

        shell.WriteError($"Map with ID {mapId} already exists!");
    }
}

sealed class RemoveMapCommand : LocalizedCommands
{
    [Dependency] private readonly IEntitySystemManager _systems = default!;

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
        var mapSystem = _systems.GetEntitySystem<SharedMapSystem>();

        if (!mapSystem.MapExists(mapId))
        {
            shell.WriteError($"Map {mapId.Value} does not exist.");
            return;
        }

        mapSystem.DeleteMap(mapId);
        shell.WriteLine($"Map {mapId.Value} was removed.");
    }
}

sealed class RemoveGridCommand : LocalizedEntityCommands
{
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

        if (!EntityManager.TryGetEntity(gridIdNet, out var gridId) || !EntityManager.HasComponent<MapGridComponent>(gridId))
        {
            shell.WriteError($"Grid {gridId} does not exist.");
            return;
        }

        EntityManager.DeleteEntity(gridId);
        shell.WriteLine($"Grid {gridId} was removed.");
    }
}

internal sealed class RunMapInitCommand : LocalizedEntityCommands
{
    [Dependency] private readonly SharedMapSystem _mapSystem = default!;

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

        if (!_mapSystem.MapExists(mapId))
        {
            shell.WriteError("Map does not exist!");
            return;
        }

        if (_mapSystem.IsInitialized(mapId))
        {
            shell.WriteError("Map is already initialized!");
            return;
        }

        _mapSystem.InitializeMap(mapId);
    }
}

internal sealed class ListMapsCommand : LocalizedEntityCommands
{
    [Dependency] private readonly IEntityManager _entManager = default!;
    [Dependency] private readonly IMapManager _map = default!;
    [Dependency] private readonly SharedMapSystem _mapSystem = default!;

    public override string Command => "lsmap";

    // PVS prevents the player from knowing about all maps.
    public override bool RequireServerOrSingleplayer => true;

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        var msg = new StringBuilder();

        foreach (var mapId in _mapSystem.GetAllMapIds().OrderBy(id => id.Value))
        {
            if (!_mapSystem.TryGetMap(mapId, out var mapUid))
                continue;

            msg.AppendFormat("{0}: {1}, init: {2}, paused: {3}, nent: {4}, grids: {5}\n",
                mapId,
                _entManager.GetComponent<MetaDataComponent>(mapUid.Value).EntityName,
                _mapSystem.IsInitialized(mapUid),
                _mapSystem.IsPaused(mapId),
                _entManager.GetNetEntity(mapUid),
                string.Join(",", _map.GetAllGrids(mapId).Select(grid => grid.Owner)));
        }

        shell.WriteLine(msg.ToString());
    }
}

internal sealed class ListGridsCommand : LocalizedEntityCommands
{
    [Dependency]
    private readonly SharedTransformSystem _transformSystem = default!;

    public override string Command => "lsgrid";

    // PVS prevents the player from knowing about all maps.
    public override bool RequireServerOrSingleplayer => true;

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        var msg = new StringBuilder();
        var xformQuery = EntityManager.GetEntityQuery<TransformComponent>();
        var grids = EntityManager.AllComponentsList<MapGridComponent>();
        grids.Sort((x, y) => x.Uid.CompareTo(y.Uid));

        foreach (var (uid, _) in grids)
        {
            var xform = xformQuery.GetComponent(uid);
            var worldPos = _transformSystem.GetWorldPosition(xform);

            msg.AppendFormat("{0}: map: {1}, ent: {2}, pos: {3:0.0},{4:0.0} \n",
                uid, xform.MapID, uid, worldPos.X, worldPos.Y);
        }

        shell.WriteLine(msg.ToString());
    }
}
