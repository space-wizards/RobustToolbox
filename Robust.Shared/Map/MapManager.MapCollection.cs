using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Robust.Shared.GameObjects;
using Robust.Shared.Map.Components;

namespace Robust.Shared.Map;

/// <summary>
///     Arguments for when a map is created or deleted locally ore remotely.
/// </summary>
public sealed class MapEventArgs : EventArgs
{
    /// <summary>
    ///     Creates a new instance of this class.
    /// </summary>
    public MapEventArgs(MapId map)
    {
        Map = map;
    }

    /// <summary>
    ///     Map that is being modified.
    /// </summary>
    public MapId Map { get; }
}

internal partial class MapManager
{
    /// <inheritdoc />
    public virtual void DeleteMap(MapId mapId)
    {
        MapSystem.DeleteMap(mapId);
    }

    /// <inheritdoc />
    public MapId CreateMap(MapId? mapId = null)
    {
        if (mapId != null)
        {
            MapSystem.CreateMap(mapId.Value);
            return mapId.Value;
        }

        MapSystem.CreateMap(out var map);
        return map;
    }

    /// <inheritdoc />
    public bool MapExists([NotNullWhen(true)] MapId? mapId)
    {
        return MapSystem.MapExists(mapId);
    }

    /// <inheritdoc />
    public EntityUid GetMapEntityId(MapId mapId)
    {
        return MapSystem.GetMapOrInvalid(mapId);
    }

    /// <summary>
    ///     Replaces GetMapEntity()'s throw-on-failure semantics.
    /// </summary>
    public EntityUid GetMapEntityIdOrThrow(MapId mapId)
    {
        return MapSystem.GetMap(mapId);
    }

    public bool TryGetMap([NotNullWhen(true)] MapId? mapId, [NotNullWhen(true)] out EntityUid? uid)
    {
        return MapSystem.TryGetMap(mapId, out uid);
    }

    /// <inheritdoc />
    public IEnumerable<MapId> GetAllMapIds()
    {
        return MapSystem.GetAllMapIds();
    }

    /// <inheritdoc />
    public bool IsMap(EntityUid uid)
    {
        return EntityManager.HasComponent<MapComponent>(uid);
    }
}
