using System;
using System.Collections.Generic;
using Robust.Shared.GameObjects;
using Robust.Shared.Log;
using Robust.Shared.Map;
using Robust.Shared.Serialization.Markdown.Mapping;
using Robust.Shared.Utility;

namespace Robust.Server.Maps;

public sealed partial class MapManagerSystem
{
    private ISawmill _logLoader = default!;

    #region Public

    public bool TryLoadGrid(MapId mapId, string path, out EntityUid? gridUid, MapLoadOptions? options = null)
    {
        options ??= new MapLoadOptions();
        throw new NotImplementedException();
    }

    public bool TryLoadMap(MapId mapId, string path, out IReadOnlyList<EntityUid> gridUids,
        MapLoadOptions? options = null)
    {
        options ??= new MapLoadOptions();
        gridUids = new List<EntityUid>();

        if (options.LoadMap && _mapManager.MapExists(mapId))
        {
            _logLoader.Error($"Tried to load map file {path} onto existing map {mapId} without overwriting the existing map?");
#if DEBUG
            DebugTools.Assert(false);
#endif
            return false;
        }

        throw new NotImplementedException();
    }

    public void SaveGrid(EntityUid uid, string ymlPath)
    {
        if (!HasComp<MapGridComponent>(uid))
        {
            _logLoader.Error($"Tried to save {ToPrettyString(uid)} as grid when it isn't a grid?");
#if DEBUG
            DebugTools.Assert(false);
#endif
            return;
        }

        _logLoader.Info($"Saving grid {ToPrettyString(uid)} to {ymlPath}");
    }

    public void SaveMap(MapId mapId, string ymlPath)
    {

    }

    #endregion

    private sealed record MapLoaderData
    {
        public readonly MapId TargetMap;
        public readonly MappingDataNode RootNode;

        public MapLoaderData(MappingDataNode rootNode)
        {
            RootNode = rootNode;
        }
    }
}
