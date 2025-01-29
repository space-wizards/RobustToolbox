using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Robust.Shared.GameObjects;
using Robust.Shared.Map.Components;
using Robust.Shared.Utility;

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
    private Dictionary<MapId, EntityUid> _mapEntities => _mapSystem.Maps;

    /// <inheritdoc />
    public virtual void DeleteMap(MapId mapId)
    {
        if (!_mapEntities.TryGetValue(mapId, out var ent) || !ent.IsValid())
            throw new InvalidOperationException($"Attempted to delete nonexistent map '{mapId}'");

        EntityManager.DeleteEntity(ent);
        DebugTools.Assert(!_mapEntities.ContainsKey(mapId));
    }

    /// <inheritdoc />
    public MapId CreateMap(MapId? mapId = null)
    {
        if (mapId != null)
        {
            _mapSystem.CreateMap(mapId.Value);
            return mapId.Value;
        }

        _mapSystem.CreateMap(out var map);
        return map;
    }

    /// <inheritdoc />
    public bool MapExists([NotNullWhen(true)] MapId? mapId)
    {
        return _mapSystem.MapExists(mapId);
    }

    /// <inheritdoc />
    public EntityUid GetMapEntityId(MapId mapId)
    {
        return _mapSystem.GetMapOrInvalid(mapId);
    }

    /// <summary>
    ///     Replaces GetMapEntity()'s throw-on-failure semantics.
    /// </summary>
    public EntityUid GetMapEntityIdOrThrow(MapId mapId)
    {
        return _mapSystem.GetMap(mapId);
    }

    public bool TryGetMap([NotNullWhen(true)] MapId? mapId, [NotNullWhen(true)] out EntityUid? uid)
    {
        return _mapSystem.TryGetMap(mapId, out uid);
    }

    /// <inheritdoc />
    public IEnumerable<MapId> GetAllMapIds()
    {
        return _mapEntities.Keys;
    }

    /// <inheritdoc />
    public bool IsMap(EntityUid uid)
    {
        return EntityManager.HasComponent<MapComponent>(uid);
    }
}
