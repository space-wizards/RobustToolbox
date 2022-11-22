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

        component.WorldMap = state.MapId;
        component.LightingEnabled = state.LightingEnabled;
        var xformQuery = GetEntityQuery<TransformComponent>();

        xformQuery.GetComponent(uid).ChangeMapId(state.MapId, xformQuery);
    }

    private void OnMapGetState(EntityUid uid, MapComponent component, ref ComponentGetState args)
    {
        args.State = new MapComponentState(component.WorldMap, component.LightingEnabled);
    }

    protected abstract void OnMapAdd(EntityUid uid, MapComponent component, ComponentAdd args);

    private void OnMapInit(EntityUid uid, MapComponent component, ComponentInit args)
    {
        var msg = new MapChangedEvent(component.WorldMap, true);
        RaiseLocalEvent(uid, msg, true);
    }

    private void OnMapRemoved(EntityUid uid, MapComponent component, ComponentShutdown args)
    {
        var iMap = (IMapManagerInternal)MapManager;

        iMap.TrueDeleteMap(component.WorldMap);

        var msg = new MapChangedEvent(component.WorldMap, false);
        RaiseLocalEvent(uid, msg, true);
    }
}
