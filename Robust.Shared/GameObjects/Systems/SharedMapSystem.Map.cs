using Robust.Shared.GameStates;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;

namespace Robust.Shared.GameObjects;

public abstract partial class SharedMapSystem
{
    private void InitializeMap()
    {
        SubscribeLocalEvent<MapComponent, ComponentAdd>(OnMapAdd);
        SubscribeLocalEvent<MapComponent, ComponentInit>(OnMapInit);
        SubscribeLocalEvent<MapComponent, ComponentShutdown>(OnMapRemoved);
        SubscribeLocalEvent<MapComponent, ComponentHandleState>(OnMapHandleState);
        SubscribeLocalEvent<MapComponent, ComponentGetState>(OnMapGetState);
    }

    private void OnMapHandleState(EntityUid uid, MapComponent component, ref ComponentHandleState args)
    {
        if (args.Current is not MapComponentState state)
            return;

        component.MapId = state.MapId;

        if (!MapManager.MapExists(state.MapId))
        {
            var mapInternal = (IMapManagerInternal)MapManager;
            mapInternal.CreateMap(state.MapId, uid);
        }

        component.LightingEnabled = state.LightingEnabled;
        var xformQuery = GetEntityQuery<TransformComponent>();

        xformQuery.GetComponent(uid).ChangeMapId(state.MapId, xformQuery);

        MapManager.SetMapPaused(state.MapId, state.MapPaused);
    }

    private void OnMapGetState(EntityUid uid, MapComponent component, ref ComponentGetState args)
    {
        args.State = new MapComponentState(component.MapId, component.LightingEnabled, component.MapPaused);
    }

    protected abstract void OnMapAdd(EntityUid uid, MapComponent component, ComponentAdd args);

    private void OnMapInit(EntityUid uid, MapComponent component, ComponentInit args)
    {
        var msg = new MapChangedEvent(uid, component.MapId, true);
        RaiseLocalEvent(uid, msg, true);
    }

    private void OnMapRemoved(EntityUid uid, MapComponent component, ComponentShutdown args)
    {
        var iMap = (IMapManagerInternal)MapManager;

        iMap.TrueDeleteMap(component.MapId);

        var msg = new MapChangedEvent(uid, component.MapId, false);
        RaiseLocalEvent(uid, msg, true);
    }
}
