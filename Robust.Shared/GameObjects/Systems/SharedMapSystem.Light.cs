using System;
using Robust.Shared.GameStates;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Maths;
using Robust.Shared.Serialization;

namespace Robust.Shared.GameObjects;

public abstract partial class SharedMapSystem
{
    public void SetAmbientLight(MapId mapId, Color color)
    {
        var mapUid = GetMap(mapId);
        var mapComp = EnsureComp<MapLightComponent>(mapUid);

        if (mapComp.AmbientLightColor.Equals(color))
            return;

        mapComp.AmbientLightColor = color;
        Dirty(mapUid, mapComp);
    }

    private void OnMapLightGetState(EntityUid uid, MapLightComponent component, ref ComponentGetState args)
    {
        args.State = new MapLightComponentState()
        {
            AmbientLightColor = component.AmbientLightColor,
        };
    }

    private void OnMapLightHandleState(EntityUid uid, MapLightComponent component, ref ComponentHandleState args)
    {
        if (args.Current is not MapLightComponentState state)
            return;

        component.AmbientLightColor = state.AmbientLightColor;
    }

    [Serializable, NetSerializable]
    private sealed class MapLightComponentState : ComponentState
    {
        public Color AmbientLightColor;
    }
}
