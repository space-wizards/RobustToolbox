using System;
using System.Collections.Generic;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Utility;

namespace Robust.Shared.GameObjects;

public abstract partial class SharedMapSystem
{
    public bool IsInitialized(MapId mapId)
    {
        if (mapId == MapId.Nullspace)
            return true; // Nullspace is always initialized

        if(!Maps.TryGetValue(mapId, out var uid))
            throw new ArgumentException($"Map {mapId} does not exist.");

        return IsInitialized(uid);
    }
    public bool IsInitialized(EntityUid? map)
    {
        if (map == null)
            return true; // Nullspace is always initialized

        return IsInitialized(map.Value);
    }

    public bool IsInitialized(Entity<MapComponent?> map)
    {
        if (!_mapQuery.Resolve(map, ref map.Comp))
            return false;

        return map.Comp.MapInitialized;
    }

    public void InitializeMap(MapId mapId, bool unpause = true)
    {
        if(!Maps.TryGetValue(mapId, out var uid))
            throw new ArgumentException($"Map {mapId} does not exist.");

        InitializeMap(uid, unpause);
    }

    public void InitializeMap(Entity<MapComponent?> map, bool unpause = true)
    {
        if (!_mapQuery.Resolve(map, ref map.Comp))
            return;

        if (map.Comp.MapInitialized)
            throw new ArgumentException($"Map {ToPrettyString(map)} is already initialized.");

        RecursiveMapInit(map.Owner);

        if (unpause)
            SetPaused(map, false);
    }

    internal void RecursiveMapInit(EntityUid entity)
    {
        var toInitialize = new List<EntityUid> {entity};
        for (var i = 0; i < toInitialize.Count; i++)
        {
            var uid = toInitialize[i];
            // toInitialize might contain deleted entities.
            if(!_metaQuery.TryComp(uid, out var meta))
                continue;

            if (meta.EntityLifeStage == EntityLifeStage.MapInitialized)
                continue;

            toInitialize.AddRange(Transform(uid)._children);
            EntityManager.RunMapInit(uid, meta);
        }
    }
}
