namespace Robust.Shared.Map;

internal partial class MapManager
{
    public void RemoveMapId(MapId mapId)
    {
        if (mapId == MapId.Nullspace)
            return;

        _mapEntities.Remove(mapId);
    }
}
