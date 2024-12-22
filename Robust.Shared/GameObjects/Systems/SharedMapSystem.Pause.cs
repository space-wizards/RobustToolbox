using System;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;

namespace Robust.Shared.GameObjects;

public abstract partial class SharedMapSystem
{
    public bool IsPaused(MapId mapId)
    {
        if (mapId == MapId.Nullspace)
            return false;

        if(!Maps.TryGetValue(mapId, out var uid))
            throw new ArgumentException($"Map {mapId} does not exist.");

        return IsPaused(uid);
    }

    public bool IsPaused(Entity<MapComponent?> map)
    {
        if (!_mapQuery.Resolve(map, ref map.Comp))
            return false;

        return map.Comp.MapPaused || !map.Comp.MapInitialized;
    }

    public void SetPaused(MapId mapId, bool paused)
    {
        if(!Maps.TryGetValue(mapId, out var uid))
            throw new ArgumentException($"Map {mapId} does not exist.");

        SetPaused(uid, paused);
    }

    public void SetPaused(Entity<MapComponent?> map, bool paused)
    {
        if (!_mapQuery.Resolve(map, ref map.Comp))
            return;

        if (map.Comp.MapPaused == paused)
            return;

        map.Comp.MapPaused = paused;
        if (map.Comp.LifeStage < ComponentLifeStage.Initializing)
            return;

        Dirty(map);
        RecursiveSetPaused(map, paused);
    }

    internal void RecursiveSetPaused(EntityUid entity, bool paused)
    {
        _meta.SetEntityPaused(entity, paused);
        foreach (var child in Transform(entity)._children)
        {
            RecursiveSetPaused(child, paused);
        }
    }
}
