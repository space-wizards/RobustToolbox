using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Maths;

namespace Robust.Shared.GameObjects;

public abstract partial class SharedMapSystem
{
    public void SetAmbientLight(MapId mapId, Color color)
    {
        var mapComp = EnsureComp<MapLightComponent>(MapManager.GetMapEntityId(mapId));

        if (mapComp.AmbientLightColor.Equals(color))
            return;

        mapComp.AmbientLightColor = color;
        Dirty(mapComp);
    }
}
