using System.Collections.Generic;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;

namespace Robust.Shared.Toolshed.Commands.Entities.World;

[ToolshedCommand]
public sealed class GridGetCommand : ToolshedCommand
{
    private SharedMapSystem? _mapSys;
    private SharedTransformSystem? _xForm;
    [Dependency] private ITileDefinitionManager _tileDefMan = default!;

    [CommandImplementation("tilename")]
    public IEnumerable<string> GetTileName([PipedArgument] IEnumerable<ITileDefinition> input)
    {
        foreach (var tileDef in input)
        {
            yield return tileDef.Name;
        }
    }

    [CommandImplementation("tilename")]
    public IEnumerable<string> GetTileName([PipedArgument] IEnumerable<EntityUid> input)
        => GetTileName(GetTile(input));

    [CommandImplementation("grid")]
    public IEnumerable<EntityUid> GetGrid([PipedArgument] IEnumerable<EntityUid> input)
    {
        _xForm ??= GetSys<SharedTransformSystem>();

        foreach (var uid in input)
        {
            var grid = _xForm.GetGrid((uid, Transform(uid)));
            if (grid != null)
                yield return grid.Value;
        }
    }

    [CommandImplementation("tile")]
    public IEnumerable<ITileDefinition> GetTile([PipedArgument] IEnumerable<EntityUid> input)
    {
        _mapSys ??= GetSys<SharedMapSystem>();
        _xForm ??= GetSys<SharedTransformSystem>();

        foreach (var uid in input)
        {
            var grid = _xForm.GetGrid((uid, Transform(uid)));
            if (grid is null)
                continue;
            if (!TryComp(grid, out MapGridComponent? gridComp))
                continue;
            var coords = _xForm.GetMapCoordinates(uid);

            var tile = _mapSys.GetTileRef((grid.Value, gridComp), coords);
            if (_tileDefMan.TryGetDefinition(tile.Tile.TypeId, out var tileDef))
                yield return tileDef;
        }
    }
}
