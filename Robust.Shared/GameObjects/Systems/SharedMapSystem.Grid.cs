using Robust.Shared.GameStates;
using Robust.Shared.Map;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Map.Components;

namespace Robust.Shared.GameObjects;

public abstract partial class SharedMapSystem
{
    private void InitializeGrid()
    {
        SubscribeLocalEvent<MapGridComponent, ComponentGetState>(OnGridGetState);
        SubscribeLocalEvent<MapGridComponent, ComponentHandleState>(OnGridHandleState);
        SubscribeLocalEvent<MapGridComponent, ComponentAdd>(OnGridAdd);
        SubscribeLocalEvent<MapGridComponent, ComponentInit>(OnGridInit);
        SubscribeLocalEvent<MapGridComponent, ComponentStartup>(OnGridStartup);
        SubscribeLocalEvent<MapGridComponent, ComponentShutdown>(OnGridRemove);
    }

    private void OnGridHandleState(EntityUid uid, MapGridComponent component, ref ComponentHandleState args)
    {
        if (args.Current is not MapGridComponentState state)
            return;

        component.ChunkSize = state.ChunkSize;
    }

    private void OnGridGetState(EntityUid uid, MapGridComponent component, ref ComponentGetState args)
    {
        args.State = new MapGridComponentState(component.ChunkSize);
    }

    private void OnGridAdd(EntityUid uid, MapGridComponent component, ComponentAdd args)
    {
        // GridID is not set yet so we don't include it.
        var msg = new GridAddEvent(uid);
        RaiseLocalEvent(uid, msg, true);
    }

    private void OnGridInit(EntityUid uid, MapGridComponent component, ComponentInit args)
    {
        var xformQuery = GetEntityQuery<TransformComponent>();
        var xform = xformQuery.GetComponent(uid);
        var mapId = xform.MapID;

        if (MapManager.HasMapEntity(mapId))
        {
            var mapUid = MapManager.GetMapEntityIdOrThrow(mapId);

            // Mapgrid moment
            if (mapUid != uid)
                _transform.SetParent(xform, MapManager.GetMapEntityIdOrThrow(mapId), xformQuery);
        }

        // Force networkedmapmanager to send it due to non-ECS legacy code.
        var curTick = _timing.CurTick;

        foreach (var chunk in component.Chunks.Values)
        {
            chunk.TileModified += component.OnTileModified;
            chunk.LastTileModifiedTick = curTick;
        }

        component.LastTileModifiedTick = curTick;

        // Just in case.
        _transform.SetGridId(xform, uid, xformQuery);

        var msg = new GridInitializeEvent(uid);
        RaiseLocalEvent(uid, msg, true);
    }

    private void OnGridStartup(EntityUid uid, MapGridComponent component, ComponentStartup args)
    {
        var msg = new GridStartupEvent(uid);
        RaiseLocalEvent(uid, msg, true);
    }

    private void OnGridRemove(EntityUid uid, MapGridComponent component, ComponentShutdown args)
    {
        RaiseLocalEvent(uid, new GridRemovalEvent(uid), true);

        if (uid == EntityUid.Invalid)
            return;

        if (!MapManager.GridExists(uid))
            return;

        MapManager.DeleteGrid(uid);
    }
}
